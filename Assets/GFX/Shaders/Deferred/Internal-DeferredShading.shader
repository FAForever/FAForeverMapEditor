Shader "Hidden/Internal-DeferredShading" {
Properties {
    _LightTexture0 ("", any) = "" {}
    _LightTextureB0 ("", 2D) = "" {}
    _ShadowMapTexture ("", any) = "" {}
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1

}
SubShader {

// Pass 1: Lighting pass
//  LDR case - Lighting encoded into a subtractive ARGB8 buffer
//  HDR case - Lighting additively blended into floating point buffer
Pass {
    ZWrite Off
    Blend [_SrcBlend] [_DstBlend]

CGPROGRAM

#define UNITY_BRDF_PBS BRDF_CUSTOM_Unity_PBS

#pragma multi_compile FANCY_STUFF_OFF FANCY_STUFF_ON
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass
#pragma multi_compile ___ UNITY_HDR_ON

#pragma exclude_renderers nomrt

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
//#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardBRDFCustom.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;
sampler1D WaterRampSampler;

uniform int _Area;
uniform half4 _AreaRect;
half _GridScale;
uniform int _ShaderID;
uniform half LightingMultiplier;
uniform fixed4 SunColor;
uniform fixed4 SunDirection;
uniform fixed4 SunAmbience;
uniform fixed4 ShadowFillColor;
uniform fixed4 SpecularColor;
uniform float WaterElevation;


float3 ApplyWaterColor(float waterDepth, float3 color)
{
    if (waterDepth > 0) {
        float4 waterColor = tex1D(WaterRampSampler, waterDepth);
        color = lerp(color.xyz, waterColor.rgb, waterColor.a);
    }
    return color;
}

float3 ApplyWaterColorExponentially(float3 viewDirection, float waterDepth, float3 color)
{
    if (waterDepth > 0) {
        float3 up = float3(0,1,0);
        // this is the length that the light travels underwater back to the camera
        float oneOverCosV = 1 / max(dot(up, normalize(viewDirection)), 0.0001);
        // Light gets absorbed exponentially,
        // to simplify, we assume that the light enters vertically into the water.
        // We need to multiply by 2 to reach 98% absorption as the waterDepth can't go over 1.
        float waterAbsorption = 1 - saturate(exp(-waterDepth * 2 * (1 + oneOverCosV)));
        // darken the color first to simulate the light absorption on the way in and out
        color *= 1 - waterAbsorption;
        // lerp in the watercolor to simulate the scattered light from the dirty water
        float4 waterColor = tex1D(WaterRampSampler, waterAbsorption);
        color = lerp(color, waterColor.rgb, waterAbsorption);
    }
    return color;
}

float4 CalculateLighting( float3 inNormal, float3 viewDirection, float3 inAlbedo, float specAmount, float shadow)
{
    float4 color = float4( 0, 0, 0, 0 );

    float SunDotNormal = dot( SunDirection, inNormal);
    float3 R = SunDirection - 2.0f * SunDotNormal * inNormal;
    float specular = pow( saturate( dot(R, viewDirection) ), 80) * SpecularColor.x * specAmount;

    float3 light = SunColor * saturate( SunDotNormal) * shadow + SunAmbience + specular;
    light = LightingMultiplier * light + ShadowFillColor * ( 1 - light );
    color.rgb = light * inAlbedo;
                
    color.a = 0.01f + (specular*SpecularColor.w);
    return color;
}

float4 CalculateXPLighting( float3 normal, float3 viewDirection, float4 albedo, float shadow)
{
    float3 r = reflect(viewDirection,normal);
    float3 specular = pow(saturate(dot(r,SunDirection)),80)*albedo.aaa*SpecularColor.a*SpecularColor.rgb;

    float dotSunNormal = dot(SunDirection,normal);

    float3 light = SunColor*saturate(dotSunNormal)*shadow + SunAmbience;
    light = LightingMultiplier*light + ShadowFillColor*(1-light);
    albedo.rgb = light * ( albedo.rgb + specular.rgb );

    return albedo;
}

float3 FresnelSchlick(float hDotN, float3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - hDotN, 5.0);
}

float NormalDistribution(float3 n, float3 h, float roughness)
{
    float a2 = roughness*roughness;
    float nDotH = max(dot(n, h), 0.0);
    float nDotH2 = nDotH*nDotH;

    float num = a2;
    float denom = nDotH2 * (a2 - 1.0) + 1.0;
    denom = 3.14159265359 * denom * denom;

    return num / denom;
}

float GeometrySchlick(float nDotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num = nDotV;
    float denom = nDotV * (1.0 - k) + k;

    return num / denom;
}

float GeometrySmith(float3 n, float nDotV, float3 l, float roughness)
{
    float nDotL = max(dot(n, l), 0.0);
    float gs2 = GeometrySchlick(nDotV, roughness);
    float gs1 = GeometrySchlick(nDotL, roughness);

    return gs1 * gs2;
}

float3 PBR(float3 wpos, float3 viewDirection, float3 albedo, float3 n, float roughness, float shadow) {
    // See https://blog.selfshadow.com/publications/s2013-shading-course/

    float facingSpecular = 0.04;
    float underwater = step(wpos, WaterElevation);
    facingSpecular *= 1 - 0.9 * underwater;

    float3 v = -viewDirection;
    float3 F0 = float3(facingSpecular, facingSpecular, facingSpecular);
    float3 l = SunDirection;
    float3 h = normalize(v + l);
    float nDotL = max(dot(n, l), 0.0);
    // Normal maps can cause an angle > 90° betweeen n and v which would 
    // cause artifacts if we don't take some countermeasures
    float nDotV = abs(dot(n, v)) + 0.001;
    float3 sunLight = SunColor * LightingMultiplier * shadow;

    // Cook-Torrance BRDF
    float3 F = FresnelSchlick(max(dot(h, v), 0.0), F0);
    float NDF = NormalDistribution(n, h, roughness);
    float G = GeometrySmith(n, nDotV, l, roughness);

    // For point lights we need to multiply with Pi
    float3 numerator = 3.14159265359 * NDF * G * F;
    // add 0.0001 to avoid division by zero
    float denominator = 4.0 * nDotV * nDotL + 0.0001;
    float3 reflected = numerator / denominator;
    
    float3 kD = float3(1.0, 1.0, 1.0) - F;	
    float3 refracted = kD * albedo;
    float3 irradiance = sunLight * nDotL;
    float3 color = (refracted + reflected) * irradiance;

    float3 shadowColor = (1 - (SunColor * shadow * nDotL + SunAmbience)) * ShadowFillColor;
    float3 ambient = SunAmbience * LightingMultiplier + shadowColor;

    // we simplify here for the ambient lighting
    color += albedo * ambient;

    return color;
}

half4 CalculateLight (unity_v2f_deferred i)
{
    float3 wpos;
    float2 uv;
    float atten, fadeDist;
    UnityLight light;
    UNITY_INITIALIZE_OUTPUT(UnityLight, light);
    UnityDeferredCalculateLightParams (i, wpos, uv, light.dir, atten, fadeDist);

    light.color = _LightColor.rgb * atten;

    // unpack Gbuffer
    half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
    half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
    half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);

    float3 eyeVec = normalize(wpos-_WorldSpaceCameraPos);

    float3 albedo = gbuffer0.rgb;
    float alpha = gbuffer0.a;
    float mapShadow = gbuffer1.r;
    float waterDepth = gbuffer1.g;
    float roughness = gbuffer1.b;
    float3 worldNormal = gbuffer2.rgb;

    worldNormal = worldNormal * 2 - 1;
    // The game is using a different coordinate system, so we need to correct for that
    worldNormal.z = worldNormal.z * -1;

	float4 color;

    if (_ShaderID == 0) {
        color.rgb = CalculateLighting(worldNormal, eyeVec, albedo, 1-alpha, atten);
    } else if (_ShaderID == 1) {
        color.rgb = CalculateXPLighting(worldNormal, eyeVec, float4(albedo, alpha), atten);
    } else if (_ShaderID == 2) {
        color.rgb = PBR(wpos, eyeVec, albedo, worldNormal, roughness, mapShadow * atten);
    }

    // Trigger for exponential water absorption
    if (LightingMultiplier > 2.1) {
        color.rgb = ApplyWaterColorExponentially(-eyeVec, waterDepth, color.rgb);
    } else {
        color.rgb = ApplyWaterColor(waterDepth, color.rgb);
    }

    color.a = 1;
    if(_Area > 0){
		if(wpos.x < _AreaRect.x){
            color.rgb = 0;
		}
		else if(wpos.x > _AreaRect.z){
			color.rgb = 0;
		}
		else if(wpos.z < _AreaRect.y - _GridScale){
			color.rgb = 0;
		}
		else if(wpos.z > _AreaRect.w - _GridScale){
			color.rgb = 0;
		}
	}

    return color;
}

#ifdef UNITY_HDR_ON
half4
#else
fixed4
#endif
frag (unity_v2f_deferred i) : SV_Target
{
    half4 c = CalculateLight(i);
    #ifdef UNITY_HDR_ON
    return c;
    #else
    return exp2(-c);
    #endif
}

ENDCG
}


// Pass 2: Final decode pass.
// Used only with HDR off, to decode the logarithmic buffer into the main RT
Pass {
    ZTest Always Cull Off ZWrite Off
    Stencil {
        ref [_StencilNonBackground]
        readmask [_StencilNonBackground]
        // Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
        compback equal
        compfront equal
    }

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers nomrt

#include "UnityCG.cginc"

sampler2D _LightBuffer;
struct v2f {
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(vertex);
    o.texcoord = texcoord.xy;
#ifdef UNITY_SINGLE_PASS_STEREO
    o.texcoord = TransformStereoScreenSpaceTex(o.texcoord, 1.0f);
#endif
    return o;
}

fixed4 frag (v2f i) : SV_Target
{
    return -log2(tex2D(_LightBuffer, i.texcoord));
}
ENDCG 
}

}
Fallback Off
}