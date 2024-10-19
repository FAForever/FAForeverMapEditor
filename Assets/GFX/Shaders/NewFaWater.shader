Shader "FAShaders/Water" {
	Properties {
		waterColor  ("waterColor", Color) = (0.0, 0.7, 1.5, 1)
		sunColor  ("Sun Color", Color) = (1.1, 0.7, 0.5, 1)
		_WaterData ("Water Data", 2D) = "white" {}
		SkySampler("SkySampler", CUBE) = "" {}
		ReflectionSampler ("ReflectionSampler", 2D) = "white" {}
		
		NormalSampler0 ("NormalSampler0", 2D) = "white" {}
		NormalSampler1 ("NormalSampler1", 2D) = "white" {}
		NormalSampler2 ("NormalSampler2", 2D) = "white" {}
		NormalSampler3 ("NormalSampler3", 2D) = "white" {}

		_GridScale ("Grid Scale", Range (0, 2048)) = 512
	}
    SubShader {
    	Tags { "Queue"="Transparent+6" "RenderType"="Transparent" }

	    GrabPass 
	     {
	     "_WaterGrabTexture"
	     } 

		//Blend SrcAlpha OneMinusSrcAlpha
		//Offset 0, -20000


		CGPROGRAM
		#pragma target 3.5

		#pragma surface surf Lambert vertex:vert alpha noambient 
			#pragma exclude_renderers gles
			#pragma multi_compile ___ UNITY_HDR_ON


		//************ Water Params
		
		uniform float _WaterScaleX, _WaterScaleZ;
		float waveCrestThreshold = 1;
		float3 waveCrestColor = float3(1,1,1);
		float refractionScale = 0.015;

		// 3 repeat rate for 3 texture layers
		float4  normalRepeatRate = float4(0.0009, 0.009, 0.05, 0.5);

		// 3 vectors of normal movements
		//float2 normal1Movement = float2(0.5, -0.95);
		float2 normal1Movement = float2(5.5, -9.95);
		float2 normal2Movement = float2(0.05, -0.095);
		float2 normal3Movement = float2(0.01, 0.03);
		float2 normal4Movement = float2(0.0005, 0.0009);

		float fresnelBias = 0.1;
		float fresnelPower = 1.5;

		float SunShininess;
		float sunReflectionAmount;
		float unitreflectionAmount;
		float skyreflectionAmount;
		float2 waterLerp;
	    float3 SunDirection;

		float SunGlow;

	    fixed4 waterColor, sunColor;
		half _GridScale;
		
		int _Area;
		half4 _AreaRect;
		
		//*********** End Water Params
		sampler2D _WaterGrabTexture;


		half4 LightingEmpty (SurfaceOutput s, half3 lightDir, half atten) {
					half4 c;
			            c.rgb = s.Albedo;
			            c.a = s.Alpha;
			            return c;
			        }

		struct Input {
	        //float4 position 	: 	SV_POSITION;
			float2 uvUtilitySamplerC : TEXCOORD0;
			float4 mLayer01      : 	TEXCOORD1;
			float4 mLayer23      : 	TEXCOORD2;
			//float2 mLayer2      : 	TEXCOORD3;
		    //float2 mLayer3      : 	TEXCOORD4;	
			float3 mViewVec     : 	TEXCOORD3;
			float4 mScreenPos	: 	TEXCOORD4;
			float4 AddVar		: 	TEXCOORD5;
			float4 grabUV;
			float3 worldPos;
			//float3 viewDir;
		};

		void vert (inout appdata_full v, out Input o){
			UNITY_INITIALIZE_OUTPUT(Input,o);
	        //o.position = UnityObjectToClipPos (v.vertex);
	        o.mScreenPos = ComputeNonStereoScreenPos(UnityObjectToClipPos (v.vertex));
	        //o.mScreenPos.xy /=  o.mScreenPos.w;
	        //o.mScreenPos.xy /= _ScreenParams.xy * 0.1;
	        
	        //o.mTexUV = v.texcoord0;
			float2 WaterLayerUv = float2(v.vertex.x * _WaterScaleX, -v.vertex.z * _WaterScaleZ);
	        //o.mLayer0 = (WaterLayerUv * _WaterScale + (float2(5.5, -9.95) * _Time.y)) * 0.0009;
	        //o.mLayer1 = (WaterLayerUv * _WaterScale + (float2(0.05, -0.095) * _Time.y)) * 0.09;
	        //o.mLayer2 = (WaterLayerUv * _WaterScale + (float2(0.01, 0.03) * _Time.y)) * 0.05;
	        //o.mLayer3 = (WaterLayerUv * _WaterScale + (float2(0.0005, 0.0009) * _Time.y)) * 0.5;

			float timer = _Time.y * 10;
			o.mLayer01.xy = (WaterLayerUv + (normal1Movement * timer)) * normalRepeatRate.x;
	        o.mLayer01.zw = (WaterLayerUv + (normal2Movement * timer)) * normalRepeatRate.y;
	        o.mLayer23.xy = (WaterLayerUv + (normal3Movement * timer)) * normalRepeatRate.z;
	        o.mLayer23.zw = (WaterLayerUv + (normal4Movement * timer)) * normalRepeatRate.w;

	        //o.mScreenPos = mul (UNITY_MATRIX_MVP, float4(0,0,0,1));
	        //o.mScreenPos.xy /= o.mScreenPos.w;
	        
	        o.mViewVec = mul (unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos;
	        o.mViewVec = normalize(o.mViewVec);
	        o.AddVar = float4(length(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz), 0, 0, 0);
			 float4 hpos = UnityObjectToClipPos (v.vertex);
	         o.grabUV = ComputeGrabScreenPos(hpos);
			//v.color = _Abyss;
		}


	    uniform sampler2D UtilitySamplerC;
	    uniform sampler2D RefractionSampler;
	    sampler2D  ReflectionSampler;
		sampler2D _ReflectionTexture;
	    uniform sampler2D NormalSampler0, NormalSampler1, NormalSampler2, NormalSampler3;
		//samplerCUBE _Reflection;
		samplerCUBE SkySampler;

	    
	   void surf (Input IN, inout SurfaceOutput o) {
	    
	    	float4 ViewportScaleOffset = float4((_ScreenParams.x / _ScreenParams.y) * 1, 1.0, (_ScreenParams.x / _ScreenParams.y) * -0.25, 0);
	    	//float3 SunDirection = normalize(float3( -0.2 , -0.967, -0.453));
	    	// calculate the depth of water at this pixel, we use this to decide
			// how to shade and it should become lesser as we get shallower
			// so that we don't have a sharp line
	    	float4 waterTexture = tex2D( UtilitySamplerC, IN.uvUtilitySamplerC * float2(-1, 1) + float2(1 / (_WaterScaleX * 1) + 1, 1 / (_WaterScaleZ * 1)) );
			
			
			float waterDepth = clamp( waterTexture.g * 10, 0, 1);
			
	        // calculate the correct viewvector
			float3 viewVector = normalize(IN.mViewVec);
			//viewVector = WorldSpaceViewDir(float4(0, 0, 1, 1));
			//viewVector = IN.viewDir;
			float OneOverW = 1 / IN.mScreenPos.w;


			// calculate the background pixel
			float4 backGroundPixels = tex2Dproj( _WaterGrabTexture, UNITY_PROJ_COORD(IN.grabUV) );

			#ifdef UNITY_HDR_ON
			//backGroundPixels.rgb = exp2(-backGroundPixels.rgb);
			#endif
			//float4 col = tex2Dproj( _MyGrabTexture3, UNITY_PROJ_COORD(IN.grabUV));

			float mask = saturate(backGroundPixels.a * 255);
	    
	        // calculate the normal we will be using for the water surface
		    float4 W0 = tex2D( NormalSampler0, IN.mLayer01.xy );
			float4 W1 = tex2D( NormalSampler1, IN.mLayer01.zw );
			float4 W2 = tex2D( NormalSampler2, IN.mLayer23.xy );
			float4 W3 = tex2D( NormalSampler3, IN.mLayer23.zw );

		    float4 sum = W0 + W1 + W2 + W3;
			waveCrestThreshold = 1.2;
		    float waveCrest = saturate( sum.a - waveCrestThreshold );
		    
		    // average, scale, bias and normalize
			float3 N = 2.0 * sum.xyz - 4.0;
			
			// flatness
		   	N = normalize(N.xzy); 
		    float3 up = float3(0, 1, 0);
		  	N = lerp(up, N, waterTexture.r);
		    
		    // calculate the reflection vector
			float3 R = reflect( viewVector, N );
	    
	        // calculate the sky reflection color
			float4 skyReflection = texCUBE( SkySampler, R );
	    		    	
	    	// get the correct coordinate for sampling refraction and reflection

			float2 screenPos = UNITY_PROJ_COORD(IN.mScreenPos.xy / IN.mScreenPos.w);

			float4 refractionPos = IN.mScreenPos;
			refractionPos.xy -= refractionScale * N.xz * OneOverW * 0.1;

			float4 GrabUvPos = IN.grabUV;
			GrabUvPos.xy -= N.xz * OneOverW * 0.1 * refractionScale;
			//GrabUvPos.xy = clamp(GrabUvPos.xy, 0, 1);
			// calculate the refract pixel, corrected for fetching a non-refractable pixel
			float4 refractedPixels = tex2Dproj(_WaterGrabTexture, UNITY_PROJ_COORD(GrabUvPos)); // UNITY_PROJ_COORD(IN.grabUV)
		    // because the range of the alpha value that we use for the water is very small
		    // we multiply by a large number and then saturate
		    // this will also help in the case where we filter to an intermediate value
		    refractedPixels.xyz = lerp(refractedPixels, backGroundPixels, saturate((IN.AddVar.x - 40) / 30 ) ).xyz; //255

			// 
			// calculate the reflected value at this pixel
			//
			float4 reflectedPixels = tex2D( _ReflectionTexture, refractionPos);


			float  NDotL = saturate(dot(-viewVector, N));
			float fresnel = saturate(pow(saturate((1 - NDotL)), fresnelPower) + fresnelBias);

			// figure out the sun reflection
			float SunDotR = saturate(dot(-R, SunDirection));
    		float3 sunReflection = pow( SunDotR, SunShininess) * sunColor.rgb * 2;

    		// lerp the reflections together
   			reflectedPixels = lerp( skyReflection, reflectedPixels, saturate(unitreflectionAmount * reflectedPixels.w));
			//reflectedPixels = skyReflection;
   			
   			// we want to lerp in some of the water color based on depth, but
			// not totally on depth as it gets clamped
			float waterLerp2 = clamp(waterDepth, waterLerp.x, waterLerp.y);
			
			// lerp in the color
			refractedPixels.xyz = lerp( refractedPixels.xyz, waterColor.rgb * 2, waterLerp2);
			
			// implement the water depth into the reflection
		    float depthReflectionAmount = 10;
		    skyreflectionAmount *= saturate(waterDepth * depthReflectionAmount);
		    
		   	// lerp the reflection into the refraction   
			refractedPixels = lerp( refractedPixels, reflectedPixels, saturate(skyreflectionAmount * fresnel));
			//refractedPixels = skyReflection;
			//refractedPixels = 
			
			// add in the sky reflection
			sunReflection = sunReflection * fresnel;
		    refractedPixels.xyz += sunReflection;

			// Lerp in a wave crest
			waveCrestColor = float3(1,1,1);
			refractedPixels.xyz = lerp( refractedPixels.xyz, waveCrestColor, ( 1 - waterTexture.a ) * waveCrest);
		    

    		float4 returnPixels = refractedPixels;

			returnPixels.a = waterDepth;
			//clip(waterDepth - 0.01);


			if(_Area > 0){
				fixed3 BlackEmit = -1;
				fixed3 Albedo = 0;
				if(IN.worldPos.x < _AreaRect.x){
					returnPixels.rgb = 0;
				}
				else if(IN.worldPos.x > _AreaRect.z){
					returnPixels.rgb = 0;
				}
				else if(IN.worldPos.z < _AreaRect.y - _GridScale){
					returnPixels.rgb = 0;
				}
				else if(IN.worldPos.z > _AreaRect.w - _GridScale){
					returnPixels.rgb = 0;
				}
			}


			o.Albedo = 0;
			// By using the emission we bypass all shading operations by Unity
			o.Emission = returnPixels.rgb;
			o.Alpha = returnPixels.a;
	    }
    ENDCG  
    }
}