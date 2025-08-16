using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Rendering/Global Fog")]
    class GlobalFog : PostEffectsBase
	{
		[Tooltip("Apply distance-based fog?")]
        public bool  distanceFog = true;
		[Tooltip("Exclude far plane pixels from distance-based fog? (Skybox or clear color)")]
		public bool  excludeFarPixels = true;
        public Shader fogShader = null;
        private Material fogMaterial = null;


        public override bool CheckResources ()
		{
            CheckSupport (true);

            fogMaterial = CheckShaderAndCreateMaterial (fogShader, fogMaterial);

            if (!isSupported)
                ReportAutoDisable ();
            return isSupported;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CheckResources() == false || !distanceFog)
            {
                Graphics.Blit(source, destination);
                return;
            }

            Camera cam = GetComponent<Camera>();
            Transform camtr = cam.transform;

            Vector3[] frustumCorners = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, cam.stereoActiveEye, frustumCorners);
            var bottomLeft = camtr.TransformVector(frustumCorners[0]);
            var topLeft = camtr.TransformVector(frustumCorners[1]);
            var topRight = camtr.TransformVector(frustumCorners[2]);
            var bottomRight = camtr.TransformVector(frustumCorners[3]);

            Matrix4x4 frustumCornersArray = Matrix4x4.identity;
            frustumCornersArray.SetRow(0, bottomLeft);
            frustumCornersArray.SetRow(1, bottomRight);
            frustumCornersArray.SetRow(2, topLeft);
            frustumCornersArray.SetRow(3, topRight);

            var camPos = camtr.position;
            
            float excludeDepth = (excludeFarPixels ? 1.0f : 2.0f);
            fogMaterial.SetMatrix("_FrustumCornersWS", frustumCornersArray);
            fogMaterial.SetVector("_CameraWS", camPos);
            fogMaterial.SetFloat("excludeDepth", excludeDepth);
            
            float fogStart = RenderSettings.fogStartDistance;
            float fogEnd = RenderSettings.fogEndDistance;
            float invDiff = 1 / Mathf.Max(fogEnd - fogStart, 0.0001f);

            fogMaterial.SetFloat("fogStart", fogStart);
            fogMaterial.SetFloat("invDiff", invDiff);
            fogMaterial.SetVector("viewForward", cam.transform.forward);
            
            Graphics.Blit(source, destination, fogMaterial, 0);
        }
    }
}
