// Based on: Kim, Pope: Screen Space Decals in Warhammer 40,000: Space Marine. SIGGRAPH 2012.
// http://www.popekim.com/2012/10/siggraph-2012-screen-space-decals-in.html

Shader "Ozone/Deferred Decal"
{
	Properties
	{
		_Mask("Mask", 2D) = "white" {}
		//[PerRendererData] _MaskMultiplier("Mask (Multiplier)", Float) = 1.0
		//_MaskNormals("Mask Normals?", Float) = 1.0

		_MainTex("Albedo", 2D) = "white" {}
		[HDR] _Color("Albedo (Multiplier)", Color) = (1,1,1,1)
		_Glow("Glow", 2D) = "black" {}

		[Normal] _NormalTex ("Normal", 2D) = "yellow" {}
		_NormalMultiplier ("Normal (Multiplier)", Float) = 1.0

		_NormalBlendMode("Normal Blend Mode", Float) = 0
		_AngleLimit("Angle Limit", Float) = 0.5

		[PerRendererData] _CutOffLOD("CutOffLOD", Float) = 0.5
		[PerRendererData] _NearCutOffLOD("NearCutOffLOD", Float) = 0.5
	}

	// Use custom GUI for the decal shader
	//CustomEditor "ThreeEyedGames.DecalShaderGUI"

	SubShader
	{
		Cull Front
		ZTest GEqual
		ZWrite Off

		// Pass 0: Albedo
		Pass
		{
			Blend One OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile_instancing
			#include "DecalsCommon.cginc"


			sampler2D _Mask;
			sampler2D _Glow;


			void frag(v2f i, out float4 outAlbedo : SV_Target0) // 
			{
				// Common header for all fragment shaders
				DEFERRED_FRAG_HEADER

				// Get color from texture and property
				float4 color = tex2D(_MainTex, texUV);// * _Color;

				//color.a = saturate(color.a);
				//color.a = 1;
				fixed4 Mask = tex2D(_Mask, texUV);
				color.a *= blend;
				float RawAlpha = color.a;


				// Write albedo, premultiply for proper blending
				outAlbedo = float4(color.rgb * color.a, color.a);

				clip(color.a - 0.003);
			}
			ENDCG
		}

		// Pass 1: Normals
		Pass
		{
			// Manual blending
			Blend SrcAlpha OneMinusSrcAlpha
			//Blend OneMinusDstColor One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma multi_compile_instancing
			#include "UnityStandardUtils.cginc"
			#include "DecalsCommon.cginc"

			void frag(v2f i, out float4 outNormal : SV_Target1)
			{
				DEFERRED_FRAG_HEADER

				// Get normal from GBuffer
				fixed3 gbuffer_normal = tex2D(_CameraGBufferTexture2Copy, uv) * 2.0f - 1.0f;
				//clip(dot(gbuffer_normal, i.decalNormal) - _AngleLimit); // 60 degree clamp

				float3 decalBitangent;
				if (_NormalBlendMode == 0)
				{
					// Reorient decal
					i.decalNormal = gbuffer_normal;
					decalBitangent = cross(i.decalNormal, i.decalTangent);
					float3 oldDecalTangent = i.decalTangent;
					i.decalTangent = cross(i.decalNormal, decalBitangent);
					if (dot(oldDecalTangent, i.decalTangent))
						i.decalTangent *= -1;
				}
				else
				{
					decalBitangent = cross(i.decalNormal, i.decalTangent);
				}

				float3 normal;
				
				float4 NormalRaw = tex2D(_NormalTex, texUV);
				normal = UnpackNormalDXT5nm(NormalRaw);

				float AlphaNormal =  NormalRaw.r;

				normal.xy *= blend;

				normal = normalize(normal);
				
				// Clip to blend it with other normal maps
				clip(AlphaNormal - 0.004);

				normal = mul(normal, half3x3(i.decalTangent, decalBitangent, i.decalNormal));

				// Write normal
				outNormal = float4(normal * 0.5 + 0.5, 0);
			}
			ENDCG
		}
	}

	Fallback Off
}
