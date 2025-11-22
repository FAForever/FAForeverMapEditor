using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


namespace UI.Windows.Neroxis
{
    public class NeroxisProcessRunner : MonoBehaviour
    {
        public GameObject OutputPanel;
        public Text OutputText;
        public Image Spinner;

        private ConcurrentQueue<string> logQueue = new();
        private StringBuilder sb = new();
        private Process neroxisToolsuite;

        public async Task<int> invokeToolsuite(string arguments)
        {
            OutputText.text = "";
            OutputPanel.SetActive(true);
            Spinner.enabled = true;

            // Give Unity time to render ui
            await Task.Yield();
            
            neroxisToolsuite = new Process();
            neroxisToolsuite.StartInfo.FileName = MapLuaParser.StructurePath + "Neroxis/neroxis-toolsuite.exe";
            neroxisToolsuite.StartInfo.Arguments = arguments;
            logQueue.Enqueue("Starting Neroxis Toolsuite: " + neroxisToolsuite.StartInfo.FileName + " " + neroxisToolsuite.StartInfo.Arguments);
            
            neroxisToolsuite.StartInfo.CreateNoWindow = true;
            neroxisToolsuite.StartInfo.UseShellExecute = false;
            neroxisToolsuite.StartInfo.RedirectStandardOutput = true;
            neroxisToolsuite.StartInfo.RedirectStandardError = true;
            neroxisToolsuite.OutputDataReceived += (sender, args) => 
            {
                Debug.Log(args.Data);
                // Swallow messages related to java class data sharing
                if (!args.Data.Contains("[cds]"))
                    logQueue.Enqueue(args.Data);
            };
            neroxisToolsuite.ErrorDataReceived += (sender, args) =>
            {
                Debug.Log(args.Data);
                if (!args.Data.Contains("[cds]"))
                    logQueue.Enqueue(args.Data);
            };

            neroxisToolsuite.Start();

            neroxisToolsuite.BeginOutputReadLine();
            neroxisToolsuite.BeginErrorReadLine();
            
            await Task.Run(() => neroxisToolsuite.WaitForExit());

            int exitCode = neroxisToolsuite.ExitCode;
            logQueue.Enqueue("Process exited with code: " + exitCode);
            Invoke(nameof(HideWindow), 1);
            if (exitCode != 0) GenericInfoPopup.ShowInfo("Command failed! Check the log for more information.");
            return exitCode;
        }
        
        private void HideWindow()
        {
            Spinner.enabled = false;
            OutputPanel.SetActive(false);
        }

        void LateUpdate()
        {
            if (logQueue.IsEmpty)
                return;
            
            sb.Clear();
            while (logQueue.TryDequeue(out string line))
                sb.AppendLine(line);

            OutputText.text += sb.ToString();
        }
    }
}