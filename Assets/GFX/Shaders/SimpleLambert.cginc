
			

inline fixed4 LightingSimpleLambert (SurfaceOutput s, UnityGI gi)
{
	fixed4 c;
    c.rgb = s.Albedo;
    c.a = s.Alpha;

	return c;
}

inline half4 LightingSimpleLambert_Deferred (SurfaceOutput s, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
	UnityStandardData data;
	data.diffuseColor   = s.Albedo;
	data.occlusion      = 1;
	data.specularColor  = 0;
	data.smoothness     = s.Gloss;
	data.normalWorld    = s.Normal;

	UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

	half4 emission = half4(s.Emission, 1);

	//#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
	//	emission.rgb += s.Albedo * gi.indirect.diffuse;
	//#endif

	return emission;
}

inline void LightingSimpleLambert_GI (
		SurfaceOutput s,
		UnityGIInput data,
		inout UnityGI gi)
	{
		gi = UnityGlobalIllumination (data, 1.0, s.Normal);
	}