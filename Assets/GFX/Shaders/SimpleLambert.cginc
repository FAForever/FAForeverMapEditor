float3 ShadowFillColor;
float LightingMultiplier;
float3 SunDirection;
float3 SunAmbience;
float3 SunColor;

struct CustomSurfaceOutput
{
    fixed3 Albedo;
    float3 wNormal; // world space normal, we use this one to prevent the automatic tangent to world space conversion
    half3 Emission; // for overlays
    half WaterDepth;
    half MapShadow; // manual terrain shadow that is defined by input texture
    half Roughness;
    half Alpha;
    
    // this only exists to make the surface shader compile and is unused
    float3 Normal;
};
			
inline float4 LightingSimpleLambertLight(CustomSurfaceOutput s, UnityLight l)
{
    float4 c;
    s.Normal.z *= -1;
    float dotLightNormal = dot(SunDirection, s.Normal);
    float3 light = SunColor * saturate(dotLightNormal) * 1 + SunAmbience;
    light = LightingMultiplier * light + (1 - light) * ShadowFillColor;
	c.rgb = s.Albedo * light;
    c.a = s.Alpha;
	return c;
}

inline fixed4 LightingSimpleLambert_PrePass(CustomSurfaceOutput s, half4 light)
{
	fixed4 c;
    c.rgb = s.Albedo;
    c.a = s.Alpha;
	return c;
}

inline fixed4 LightingSimpleLambert(CustomSurfaceOutput s, UnityGI gi)
{
	fixed4 c;
	c = LightingSimpleLambertLight (s, gi.light);

	return c;
}

inline half4 LightingSimpleLambert_Deferred(CustomSurfaceOutput s, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    outGBuffer0 = half4(s.Albedo, s.Alpha);

    outGBuffer1 = half4(s.MapShadow, s.WaterDepth, s.Roughness, 0);

    outGBuffer2 = half4(s.wNormal * 0.5f + 0.5f, 0);

    half4 emission = half4(s.Emission, 1);
	return emission;
}

inline void LightingSimpleLambert_GI (
		CustomSurfaceOutput s,
		UnityGIInput data,
		inout UnityGI gi)
	{
		gi = UnityGlobalIllumination (data, 1.0, s.wNormal);
	}