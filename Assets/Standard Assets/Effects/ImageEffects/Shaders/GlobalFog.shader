Shader "FA/GlobalFog" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "black" {}
}

CGINCLUDE

	#include "UnityCG.cginc"

	uniform sampler2D _MainTex;
	uniform sampler2D_float _CameraDepthTexture;
	
	#ifndef UNITY_APPLY_FOG
	half4 unity_FogColor;
	half4 unity_FogDensity;
	#endif	

	uniform float4 _MainTex_TexelSize;
	
	// for fast world space reconstruction
	uniform float4x4 _FrustumCornersWS;
	uniform float4 _CameraWS;
	
	uniform float excludeDepth;
	uniform float fogStart;
	uniform float invDiff;
	uniform float3 viewForward;

	struct appdata_fog
	{
		float4 vertex : POSITION;
		half2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv_depth : TEXCOORD1;
		float4 interpolatedRay : TEXCOORD2;
		float4 centerRay : TEXCOORD3;
	};
	
	v2f vert (appdata_fog v)
	{
		v2f o;
		v.vertex.z = 0.1;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;
		
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif				
		
		int frustumIndex = v.texcoord.x + (2 * o.uv.y);
		o.interpolatedRay = _FrustumCornersWS[frustumIndex];
		o.interpolatedRay.w = frustumIndex;
		o.centerRay = lerp(
        lerp(_FrustumCornersWS[0], _FrustumCornersWS[2], 0.5),
        lerp(_FrustumCornersWS[1], _FrustumCornersWS[3], 0.5),
        0.5);
		
		return o;
	}

	half4 ComputeFog (v2f i) : SV_Target
	{
		half4 sceneColor = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.uv));
		
		// Reconstruct world space position & direction
		// towards this screen pixel.
		float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth));
		float dpth = Linear01Depth(rawDepth);
		float4 wsDir = dpth * i.interpolatedRay;
		float4 wsPos = _CameraWS + wsDir;

		half4 fogColor = unity_FogColor;

		float2 forwardHorizontal = normalize(viewForward.xz);
		float centerDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(float2(0.5, 0.5))));
		float3 centerPos = _CameraWS + centerDepth * i.centerRay;
		float distance = dot(forwardHorizontal, wsPos.xz - centerPos.xz);

	    float fogFactor = exp2((distance - fogStart) * invDiff) - 1;
		// Do not fog skybox
    	if (dpth == excludeDepth)
    		fogFactor = 0.0;

		float pitchFactor = 1 - dot(viewForward, float3(0,-1,0));
		
		return lerp(sceneColor, fogColor, saturate(fogFactor * pitchFactor));
	}

ENDCG

SubShader
{
	ZTest Always Cull Off ZWrite Off Fog { Mode Off }
	
	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		half4 frag (v2f i) : SV_Target { return ComputeFog (i); }
		ENDCG
	}
}

Fallback off

}
