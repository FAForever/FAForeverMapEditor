using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using UnityEngine;

// Attach to its own GameObject in the scene.
// ScmapEditor.Current is null-guarded in Update() — no explicit
// Script Execution Order configuration required.
public class ForgeBridgeServer : MonoBehaviour
{
    public static ForgeBridgeServer Current { get; private set; }

    private const string PipeName = "ForgeMapToolkit.EditorBridge";

    private NamedPipeServerStream _pipe;
    private Thread _acceptThread;
    private CancellationTokenSource _cts;

    // Parsed, ready-to-apply patches are queued here by the read thread
    // and drained on the Unity main thread in Update().
    private readonly ConcurrentQueue<SkyboxPatch> _patchQueue =
        new ConcurrentQueue<SkyboxPatch>();

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Debug.LogWarning("[ForgeBridge] Duplicate ForgeBridgeServer detected — destroying this instance.");
            Destroy(gameObject);
            return;
        }
        Current = this;

        _cts = new CancellationTokenSource();
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "ForgeBridge-Accept" };
        _acceptThread.Start();

        Debug.Log("[ForgeBridge] Server started, listening on pipe: " + PipeName);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _pipe?.Dispose();
        if (Current == this) Current = null;
        Debug.Log("[ForgeBridge] Server stopped.");
    }

    // -------------------------------------------------------------------------
    // Main-thread Update — drain queue, apply patches
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (ScmapEditor.Current == null) return;

        while (_patchQueue.TryDequeue(out SkyboxPatch patch))
        {
            ApplyPatch(patch);
        }
    }

    private void ApplyPatch(SkyboxPatch patch)
    {
        try
        {
            var data = ScmapEditor.Current.map.AdditionalSkyboxData.Data;

            data.SubtractHeight    = patch.subtractHeight;
            data.SubdivisionsAxis  = patch.subdivisionsAxis;
            data.SubdivisionsHeight = patch.subdivisionsHeight;
            data.HorizonHeight     = patch.horizonHeight;
            data.ZenithHeight      = patch.zenithHeight;

            data.HorizonColor = new UnityEngine.Color(
                patch.horizonColor[0], patch.horizonColor[1], patch.horizonColor[2]);
            data.ZenithColor = new UnityEngine.Color(
                patch.zenithColor[0], patch.zenithColor[1], patch.zenithColor[2]);

            ScmapEditor.Current.Skybox.LoadSkybox();
        }
        catch (Exception ex)
        {
            Debug.LogError("[ForgeBridge] ApplyPatch failed: " + ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // Background thread — accept loop
    // Reopens the pipe after each connection so the server is always ready
    // for the next client connect without restarting the Editor.
    // -------------------------------------------------------------------------

    private void AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                _pipe = CreatePipeWithAcl();
                _pipe.WaitForConnection(); // blocks until Toolkit connects

                Debug.Log("[ForgeBridge] Client connected.");
                HandleConnection(_pipe);
                Debug.Log("[ForgeBridge] Client disconnected.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (!_cts.IsCancellationRequested)
            {
                Debug.LogWarning("[ForgeBridge] Accept error: " + ex.Message + " — retrying in 1s.");
                Thread.Sleep(1000);
            }
            finally
            {
                _pipe?.Dispose();
                _pipe = null;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Per-connection read loop — framing (§5.2) + message dispatch (§5.3)
    // -------------------------------------------------------------------------

    private void HandleConnection(NamedPipeServerStream pipe)
    {
        try
        {
            while (!_cts.IsCancellationRequested && pipe.IsConnected)
            {
                // Read 4-byte length prefix (UInt32, little-endian)
                byte[] lenBuf = ReadExact(pipe, 4);
                if (lenBuf == null) break; // client disconnected cleanly

                uint msgLen = BitConverter.ToUInt32(lenBuf, 0);
                if (msgLen == 0 || msgLen > 1_048_576) // sanity cap: 1 MB
                {
                    Debug.LogWarning("[ForgeBridge] Implausible message length " + msgLen + " — dropping connection.");
                    break;
                }

                byte[] body = ReadExact(pipe, (int)msgLen);
                if (body == null) break;

                string json = Encoding.UTF8.GetString(body);
                DispatchMessage(pipe, json);
            }
        }
        catch (Exception ex) when (!_cts.IsCancellationRequested)
        {
            Debug.LogWarning("[ForgeBridge] Read error: " + ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // Message dispatch
    // -------------------------------------------------------------------------

    private void DispatchMessage(NamedPipeServerStream pipe, string json)
    {
        try
        {
            var envelope = JsonUtility.FromJson<MessageEnvelope>(json);
            if (envelope == null || string.IsNullOrEmpty(envelope.type))
            {
                Debug.LogWarning("[ForgeBridge] Received message with missing type — ignored.");
                return;
            }

            switch (envelope.type)
            {
                case "handshake.request":
                    HandleHandshake(pipe, json);
                    break;

                case "skybox.snapshot":
                    HandleSnapshot(json);
                    break;

                default:
                    Debug.LogWarning("[ForgeBridge] Unknown message type: " + envelope.type);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[ForgeBridge] DispatchMessage failed: " + ex.Message);
        }
    }

    private void HandleHandshake(NamedPipeServerStream pipe, string json)
    {
        var req = JsonUtility.FromJson<HandshakeRequest>(json);
        string loadedMap = GetLoadedMapName();
        bool ok = !string.IsNullOrEmpty(loadedMap) &&
                  string.Equals(req.mapName, loadedMap, StringComparison.OrdinalIgnoreCase);

        var response = new HandshakeResponse
        {
            type      = "handshake.response",
            ok        = ok,
            loadedMap = loadedMap ?? ""
        };

        if (!ok)
        {
            // Also send map.mismatch so Toolkit can show the warning state
            var mismatch = new MapMismatch
            {
                type      = "map.mismatch",
                loadedMap = loadedMap ?? ""
            };
            SendMessage(pipe, JsonUtility.ToJson(mismatch));
        }

        SendMessage(pipe, JsonUtility.ToJson(response));
        Debug.Log("[ForgeBridge] Handshake — mapName: " + req.mapName + ", ok: " + ok + ", loadedMap: " + loadedMap);
    }

    private void HandleSnapshot(string json)
    {
        var msg = JsonUtility.FromJson<SnapshotMessage>(json);
        if (msg?.payload == null)
        {
            Debug.LogWarning("[ForgeBridge] skybox.snapshot with null payload — ignored.");
            return;
        }
        _patchQueue.Enqueue(msg.payload);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetLoadedMapName()
    {
        // ScmapEditor.map has no "Filename" field — the loaded map's folder name
        // lives on MapLuaParser instead, set during scenario load
        // (FolderName = Names[Names.Length - 2] in MapLuaParser.cs).
        // MapLuaParser.IsMapLoaded is the existing static guard for "is a map ready".
        // FolderName is already in the exact format the Toolkit sends as mapName
        // (e.g. "Tartaron.v0006") — no path parsing needed.
        try
        {
            if (!MapLuaParser.IsMapLoaded) return null;
            return MapLuaParser.Current.FolderName;
        }
        catch { return null; }
    }

    /// <summary>
    /// Write a length-prefixed UTF-8 JSON message to the pipe.
    /// Called from the background read thread only.
    /// </summary>
    private static void SendMessage(PipeStream pipe, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        byte[] lenBuf = BitConverter.GetBytes((uint)body.Length);
        pipe.Write(lenBuf, 0, 4);
        pipe.Write(body, 0, body.Length);
        pipe.Flush();
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes. Returns null on clean EOF.
    /// </summary>
    private static byte[] ReadExact(Stream stream, int count)
    {
        byte[] buf = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buf, offset, count - offset);
            if (read == 0) return null; // EOF / client disconnected
            offset += read;
        }
        return buf;
    }

    /// <summary>
    /// Creates the NamedPipeServerStream.
    /// NOTE: §3 originally called for an explicit ACL restricting access to the
    /// current Windows user via a PipeSecurity overload. Unity's Mono runtime
    /// does not implement that constructor overload — it compiles, but throws
    /// NotImplementedException at runtime on every accept attempt. Falling back
    /// to the basic constructor without an explicit ACL; both ends of the pipe
    /// run locally under the same user session anyway.
    /// </summary>
    private static NamedPipeServerStream CreatePipeWithAcl()
    {
        return new NamedPipeServerStream(
            PipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.None,
            inBufferSize:  4096,
            outBufferSize: 4096);
    }

    // -------------------------------------------------------------------------
    // Serialization types (JsonUtility-compatible: public fields, no properties)
    // -------------------------------------------------------------------------

    [Serializable] private class MessageEnvelope   { public string type; }
    [Serializable] private class HandshakeRequest  { public string type; public string mapName; }
    [Serializable] private class HandshakeResponse { public string type; public bool ok; public string loadedMap; }
    [Serializable] private class MapMismatch       { public string type; public string loadedMap; }

    [Serializable]
    private class SnapshotMessage
    {
        public string type;
        public string mapName;
        public SkyboxPatch payload;
    }
}

// Standalone struct — can be referenced from tests without the MonoBehaviour
[Serializable]
public class SkyboxPatch
{
    public float   subtractHeight;
    public int     subdivisionsAxis;
    public int     subdivisionsHeight;
    public float   horizonHeight;
    public float   zenithHeight;
    public float[] horizonColor;   // [r, g, b]
    public float[] zenithColor;    // [r, g, b]
}
