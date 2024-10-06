


Shader "FAShaders/Terrain"
{
    Properties
    {
        // It is conventional to begin all property names with an underscore.
        // However, to be as close to the FA shader code as possible, we ignore this here.
        // We use underscores only for variables that are introduced by the editor.

        ShadowFillColor ("Shadow Fill Color", Color) = (0.0, 0.0, 0.0, 1)
        LightingMultiplier ("Lighting Multiplier", Float) = 1
        SunDirection ("Sun Direction", Vector) = (0.0, 1.0, 0.0, 1)
        SunAmbience ("Ambient Light Color", Color) = (0.0, 0.0, 0.0, 1)
        SunColor ("Sun Color", Color) = (0.0, 0.0, 0.0, 1)
        SpecularColor ("Specular Color", Color) = (0.0, 0.0, 0.0, 1)
        WaterElevation ("Water Elevation", Float) = 0

        LowerAlbedoTile ("Lower Albedo Tile", Float) = 1
        LowerNormalTile ("Lower Normal Tile", Float) = 1
        Stratum0AlbedoTile ("Stratum 0 Albedo Tile", Float) = 1
        Stratum1AlbedoTile ("Stratum 1 Albedo Tile", Float) = 1
        Stratum2AlbedoTile ("Stratum 2 Albedo Tile", Float) = 1
        Stratum3AlbedoTile ("Stratum 3 Albedo Tile", Float) = 1
        Stratum4AlbedoTile ("Stratum 4 Albedo Tile", Float) = 1
        Stratum5AlbedoTile ("Stratum 5 Albedo Tile", Float) = 1
        Stratum6AlbedoTile ("Stratum 6 Albedo Tile", Float) = 1
        Stratum7AlbedoTile ("Stratum 7 Albedo Tile", Float) = 1
        Stratum0NormalTile ("Stratum 0 Normal Tile", Float) = 1
        Stratum1NormalTile ("Stratum 1 Normal Tile", Float) = 1
        Stratum2NormalTile ("Stratum 2 Normal Tile", Float) = 1
        Stratum3NormalTile ("Stratum 3 Normal Tile", Float) = 1
        Stratum4NormalTile ("Stratum 4 Normal Tile", Float) = 1
        Stratum5NormalTile ("Stratum 5 Normal Tile", Float) = 1
        Stratum6NormalTile ("Stratum 6 Normal Tile", Float) = 1
        Stratum7NormalTile ("Stratum 7 Normal Tile", Float) = 1
        UpperAlbedoTile ("Upper Albedo Tile", Float) = 1
        UpperNormalTile ("Upper Normal Tile", Float) = 1

        // used to generate texture coordinates
        // this is 1/mapresolution
        TerrainScale ("Terrain Scale", Range (0, 1)) = 1

        UtilitySamplerA ("masks of stratum layers 0 - 3", 2D) = "black" {}
        UtilitySamplerB ("masks of stratum layers 4 - 7", 2D) = "black" {}
        UtilitySamplerC ("water properties", 2D) = "black" {}  // not set?

    	LowerAlbedoSampler ("Layer Lower (R)", 2D) = "white" {}
	    LowerNormalSampler ("Normal Lower (R)", 2D) = "bump" {}
    	UpperAlbedoSampler ("Layer Lower (R)", 2D) = "white" {}

        _SplatAlbedoArray ("Albedo array", 2DArray) = "" {}
	    _SplatNormalArray ("Normal array", 2DArray) = "" {}

        [MaterialToggle] _HideStratum0("Hide stratum 0", Integer) = 0
	    [MaterialToggle] _HideStratum1("Hide stratum 1", Integer) = 0
	    [MaterialToggle] _HideStratum2("Hide stratum 2", Integer) = 0
	    [MaterialToggle] _HideStratum3("Hide stratum 3", Integer) = 0
	    [MaterialToggle] _HideStratum4("Hide stratum 4", Integer) = 0
	    [MaterialToggle] _HideStratum5("Hide stratum 5", Integer) = 0
	    [MaterialToggle] _HideStratum6("Hide stratum 6", Integer) = 0
	    [MaterialToggle] _HideStratum7("Hide stratum 7", Integer) = 0
	    [MaterialToggle] _HideStratum8("Hide upper stratum", Integer) = 0

	    [MaterialToggle] _HideTerrainType("Hide Terrain Type", Integer) = 0
       	_TerrainTypeAlbedo ("Terrain Type Albedo", 2D) = "black" {}
	    _TerrainTypeCapacity ("Terrain Type Capacity", Range(0,1)) = 0.228

        [MaterialToggle] _Grid("Grid", Integer) = 0
        _GridType("Grid type", Integer) = 0
        // This should be refactored so we can use TerrainScale instead
        _GridScale ("Grid Scale", Range (0, 2048)) = 512
	    _GridTexture ("Grid Texture", 2D) = "white" {}

        [MaterialToggle] _Slope("Slope", Integer) = 0
	    [MaterialToggle] _UseSlopeTex("Use Slope Data", Integer) = 0
        _SlopeTex ("Slope data", 2D) = "black" {}

      	[MaterialToggle] _Brush ("Brush", Integer) = 0
	    [MaterialToggle] _BrushPainting ("Brush painting", Integer) = 0
	    _BrushTex ("Brush (RGB)", 2D) = "white" {}
	    _BrushSize ("Brush Size", Range (0, 128)) = 0
	    _BrushUvX ("Brush X", Range (0, 1)) = 0
	    _BrushUvY ("Brush Y", Range (0, 1)) = 0

        // Is this still needed?
        [MaterialToggle] _GeneratingNormal("Generating Normal", Integer) = 0
	    _TerrainNormal ("Terrain Normal", 2D) = "bump" {}

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex TerrainVS
            #pragma fragment fragmentShader
            #pragma target 3.5


			int _Slope;
            int _UseSlopeTex;
			sampler2D _SlopeTex;

			int _Grid, _GridType;
            half _GridScale;
			half _GridCamDist;
			sampler2D _GridTexture;

			sampler2D _TerrainNormal;
			sampler2D _TerrainTypeAlbedo;
	
			UNITY_DECLARE_TEX2DARRAY(_StratumAlbedoArray);
			UNITY_DECLARE_TEX2DARRAY(_StratumNormalArray);

			int _HideStratum0;
            int _HideStratum1;
            int _HideStratum2;
            int _HideStratum3;
            int _HideStratum4;
            int _HideStratum5;
            int _HideStratum6;
            int _HideStratum7;
            int _HideStratum8;
			int _HideTerrainType;
			float _TerrainTypeCapacity;

			int _Brush;
            int _BrushPainting;
			sampler2D _BrushTex;
			half _BrushSize;
			half _BrushUvX;
			half _BrushUvY;

			uniform int _Area;
			uniform half4 _AreaRect;

            int ShadowsEnabled;
            float ShadowSize;

            float3 ShadowFillColor;
            float LightingMultiplier;
            float3 SunDirection;
            float3 SunAmbience;
            float3 SunColor;
            float4 SpecularColor;
            float WaterElevation;

            float4 LowerAlbedoTile;
            float4 LowerNormalTile;
            float4 Stratum0AlbedoTile;
            float4 Stratum1AlbedoTile;
            float4 Stratum2AlbedoTile;
            float4 Stratum3AlbedoTile;
            float4 Stratum4AlbedoTile;
            float4 Stratum5AlbedoTile;
            float4 Stratum6AlbedoTile;
            float4 Stratum7AlbedoTile;
            float4 Stratum0NormalTile;
            float4 Stratum1NormalTile;
            float4 Stratum2NormalTile;
            float4 Stratum3NormalTile;
            float4 Stratum4NormalTile;
            float4 Stratum5NormalTile;
            float4 Stratum6NormalTile;
            float4 Stratum7NormalTile;
            float4 UpperAlbedoTile;
            float4 UpperNormalTile;

            float4 TerrainScale;

            sampler2D UtilitySamplerA;
            sampler2D UtilitySamplerB;
            sampler2D UtilitySamplerC;
            sampler1D WaterRampSampler;

            UNITY_DECLARE_SHADOWMAP(ShadowSampler);

            sampler2D LowerAlbedoSampler;
            sampler2D UpperAlbedoSampler;
            sampler2D LowerNormalSampler;
       

            typedef float4 position_t;


            struct VS_OUTPUT
            {
                float4 mPos                    : POSITION0;
                float4 mTexWT                : TEXCOORD1;
                float4 mTexSS                : TEXCOORD2;
                float4 mShadow              : TEXCOORD3;
                float3 mViewDirection        : TEXCOORD4;
                float4 mTexDecal            : TEXCOORD5;
                // We store some texture scales here to be able to save some memory registers
                // in the pixel shader as the stratumTiles only have one float of actual data,
                // but use four and we are pretty tight on register slots in the new shaders.
                float4 nearScales           : TEXCOORD6;
                float4 farScales            : TEXCOORD7;
            };

            float ComputeShadow( float4 vShadowCoord )
            {
                vShadowCoord.xy /= vShadowCoord.w;
                return UNITY_SAMPLE_SHADOW(ShadowSampler, vShadowCoord);
            }

            bool IsExperimentalShader() {
                // The tile value basically says how often the texture gets repeated on the map.
                // A value less than one doesn't make sense under normal conditions, so it is
                // relatively save to use it as our switch.

                // in order to trigger this you can set the albedo scale to be bigger than the map 
                // size. Use the value 10000 to be safe for any map
                return UpperAlbedoTile.x < 1.0;
            }

            float3 ApplyWaterColor(float terrainHeight, float waterDepth, float3 color)
            {
                if (waterDepth > 0) {
                    float4 waterColor = tex1D(WaterRampSampler, waterDepth);
                    color = lerp(color.xyz, waterColor.rgb, waterColor.a);
                }
                return color;
            }

            float3 ApplyWaterColorExponentially(float3 viewDirection, float terrainHeight, float waterDepth, float3 color)
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

            float4 CalculateLighting( float3 inNormal, float3 worldTerrain, float3 inAlbedo, float specAmount, float waterDepth, float4 inShadow)
            {
                float4 color = float4( 0, 0, 0, 0 );

                float shadow = ComputeShadow( inShadow );
                if (IsExperimentalShader()) {
                    float3 position = TerrainScale * worldTerrain;
                    float mapShadow = tex2D(UpperAlbedoSampler, position.xy).w;
                    shadow = shadow * mapShadow;
                }

                // calculate some specular
                float3 viewDirection = normalize(worldTerrain.xzy - _WorldSpaceCameraPos);

                float SunDotNormal = dot( SunDirection, inNormal);
                float3 R = SunDirection - 2.0f * SunDotNormal * inNormal;
                float specular = pow( saturate( dot(R, viewDirection) ), 80) * SpecularColor.x * specAmount;

                float3 light = SunColor * saturate( SunDotNormal) * shadow + SunAmbience + specular;
                light = LightingMultiplier * light + ShadowFillColor * ( 1 - light );
                color.rgb = light * inAlbedo;

                if (IsExperimentalShader()) {
                    color.rgb = ApplyWaterColorExponentially(-viewDirection, worldTerrain.z, waterDepth, color);
                } else {
                    color.rgb = ApplyWaterColor(worldTerrain.z, waterDepth, color);
                }

                color.a = 0.01f + (specular*SpecularColor.w);
                return color;
            }

            const float PI = 3.14159265359;

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
                denom = PI * denom * denom;

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

            float3 PBR(VS_OUTPUT inV, float4 position, float3 albedo, float3 n, float roughness, float waterDepth) {
                // See https://blog.selfshadow.com/publications/s2013-shading-course/

                float shadow = 1;
                float mapShadow = tex2D(UpperAlbedoSampler, position.xy).w; // 1 where sun is, 0 where shadow is
                shadow = UNITY_SAMPLE_SHADOW(ShadowSampler, float3(inV.mShadow.xy, 0));
                shadow *= mapShadow;


                float facingSpecular = 0.04;
                // using only the texture looks bad when zoomed in, using only the mesh 
                // looks bad when zoomed out, so we interpolate between both
                float underwater = step(inV.mTexWT.z, WaterElevation);
                facingSpecular *= 1 - 0.9 * underwater;

                float3 v = normalize(-inV.mViewDirection);
                float3 F0 = float3(facingSpecular, facingSpecular, facingSpecular);
                float3 l = SunDirection;
                float3 h = normalize(v + l);
                float nDotL = max(dot(n, l), 0.0);
                // Normal maps can cause an angle > 90� betweeen n and v which would 
                // cause artifacts if we don't take some countermeasures
                float nDotV = abs(dot(n, v)) + 0.001;
                float3 sunLight = SunColor * LightingMultiplier * shadow;

                // Cook-Torrance BRDF
                float3 F = FresnelSchlick(max(dot(h, v), 0.0), F0);
                float NDF = NormalDistribution(n, h, roughness);
                float G = GeometrySmith(n, nDotV, l, roughness);

                // For point lights we need to multiply with Pi
                float3 numerator = PI * NDF * G * F;
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




            VS_OUTPUT TerrainVS( position_t p : POSITION0, uniform bool shadowed)
            {
                VS_OUTPUT result;

                result.nearScales = float4(Stratum0AlbedoTile.x, Stratum1AlbedoTile.x, Stratum2AlbedoTile.x, Stratum3AlbedoTile.x);
                result.farScales =  float4(Stratum0NormalTile.x, Stratum1NormalTile.x, Stratum2NormalTile.x, Stratum3NormalTile.x);

                float4 position = float4(p);

                // calculate output position
                result.mPos = mul(position, UNITY_MATRIX_VP);

                // calculate 0..1 uv based on size of map
                result.mTexWT = position.xzyw;
                // caluclate screen space coordinate for sample a frame buffer of this size
                result.mTexSS = result.mPos;
                result.mTexDecal = float4(0,0,0,0);

                result.mViewDirection = normalize(position.xyz - _WorldSpaceCameraPos.xyz);

                // fill in the tex coordinate for the shadow projection
                result.mShadow = mul(position, unity_WorldToShadow[0]);
                result.mShadow.x = ( +result.mShadow.x + result.mShadow.w ) * 0.5;
                result.mShadow.y = ( -result.mShadow.y + result.mShadow.w ) * 0.5;
                result.mShadow.z -= 0.01f; // put epsilon in vs to save ps instruction

                return result;
            }

            float4 TerrainNormalsPS( VS_OUTPUT inV ) : COLOR
            {
                // sample all the textures we'll need
                float4 mask = saturate(tex2D( UtilitySamplerA, inV.mTexWT * TerrainScale));

                float4 lowerNormal = normalize(tex2D( LowerNormalSampler, inV.mTexWT  * TerrainScale * LowerNormalTile ) * 2 - 1);
                float4 stratum0Normal = normalize(UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum0NormalTile, 0)) * 2 - 1);
                float4 stratum1Normal = normalize(UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum1NormalTile, 1)) * 2 - 1);
                float4 stratum2Normal = normalize(UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum2NormalTile, 2)) * 2 - 1);
                float4 stratum3Normal = normalize(UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum3NormalTile, 3)) * 2 - 1);

                // blend all normals together
                float4 normal = lowerNormal;
                normal = lerp( normal, stratum0Normal, mask.x );
                normal = lerp( normal, stratum1Normal, mask.y );
                normal = lerp( normal, stratum2Normal, mask.z );
                normal = lerp( normal, stratum3Normal, mask.w );
                normal.xyz = normalize( normal.xyz );

                return float4( (normal.xyz * 0.5 + 0.5) , normal.w);
            }

            float4 TerrainPS( VS_OUTPUT inV, uniform bool inShadows ) : COLOR
            {
                // sample all the textures we'll need
                float4 mask = saturate(tex2Dproj( UtilitySamplerA, inV.mTexWT  * TerrainScale)* 2 - 1 );
                float4 upperAlbedo = tex2Dproj( UpperAlbedoSampler, inV.mTexWT  * TerrainScale* UpperAlbedoTile );
                float4 lowerAlbedo = tex2Dproj( LowerAlbedoSampler, inV.mTexWT  * TerrainScale* LowerAlbedoTile );
                float4 stratum0Albedo = UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum0AlbedoTile, 0));
                float4 stratum1Albedo = UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum1AlbedoTile, 1));
                float4 stratum2Albedo = UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum2AlbedoTile, 2));
                float4 stratum3Albedo = UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(inV.mTexWT.xy * TerrainScale * Stratum3AlbedoTile, 3));

                float3 normal = TerrainNormalsPS(inV).xyz*2-1;

                // blend all albedos together
                float4 albedo = lowerAlbedo;
                albedo = lerp( albedo, stratum0Albedo, mask.x );
                albedo = lerp( albedo, stratum1Albedo, mask.y );
                albedo = lerp( albedo, stratum2Albedo, mask.z );
                albedo = lerp( albedo, stratum3Albedo, mask.w );
                albedo.xyz = lerp( albedo.xyz, upperAlbedo.xyz, upperAlbedo.w );

                // get the water depth
                float waterDepth = tex2Dproj( UtilitySamplerC, inV.mTexWT * TerrainScale).g;

                // calculate the lit pixel
                float4 outColor = CalculateLighting( normal, inV.mTexWT.xyz, albedo.xyz, 1-albedo.w, waterDepth, inV.mShadow);

                return outColor;
            }

            float4 renderFog(float4 color){
                return color;
            }
            
            float4 renderOnlyArea(float4 worldPos, float4 color){
                if(_Area > 0){
					if(worldPos.x < _AreaRect.x){
                        color.rgb = 0;
					}
					else if(worldPos.x > _AreaRect.z){
						color.rgb = 0;
					}
					else if(worldPos.z < _AreaRect.y - _GridScale){
						color.rgb = 0;
					}
					else if(worldPos.z > _AreaRect.w - _GridScale){
						color.rgb = 0;
					}
				}
                return color;
            }

            float3 renderBrush(float2 uv){
                float3 Emit = 0;
                if (_Brush > 0) {
					float2 BrushUv = ((uv - float2(_BrushUvX, _BrushUvY)) * _GridScale) / (_BrushSize * _GridScale * 0.002);
					fixed4 BrushColor = tex2D(_BrushTex, BrushUv);

					if (BrushUv.x >= 0 && BrushUv.y >= 0 && BrushUv.x <= 1 && BrushUv.y <= 1) {

						half LerpValue = clamp(_BrushSize / 20, 0, 1);

						half From = 0.1f;
						half To = lerp(0.2f, 0.13f, LerpValue);
						half Range = lerp(0.015f, 0.008f, LerpValue);

						if (BrushColor.r >= From && BrushColor.r <= To) {
							half AA = 1;

							if (BrushColor.r < From + Range)
								AA = (BrushColor.r - From) / Range;
							else if (BrushColor.r > To - Range)
								AA = 1 - (BrushColor.r - (To - Range)) / Range;

							AA = clamp(AA, 0, 1);

							Emit += half3(0, 0.3, 1) * (AA * 0.8);
						}

						if (_BrushPainting <= 0)
							Emit += half3(0, BrushColor.r * 0.1, BrushColor.r * 0.2);
						else
							Emit += half3(0, BrushColor.r * 0.1, BrushColor.r * 0.2) * 0.2;
					}
				}
                return Emit;
            }

            float4 renderSlope(float4 color){
                return color;
            }

            float4 renderTerrainType(float4 color){
                return color;
            }

            float4 RenderGrid(sampler2D _GridTex, float2 uv_Control, float Offset, float GridScale) {
				fixed4 GridColor = tex2D(_GridTex, uv_Control * GridScale + float2(-Offset, Offset));
				fixed4 GridFinal = fixed4(0, 0, 0, GridColor.a);
				if (_GridCamDist < 1) {
					GridFinal.rgb = lerp(GridFinal.rgb, fixed3(1, 1, 1), GridColor.r * lerp(1, 0, _GridCamDist));
					GridFinal.rgb = lerp(GridFinal.rgb, fixed3(0, 1, 0), GridColor.g * lerp(1, 0, _GridCamDist));
					GridFinal.rgb = lerp(GridFinal.rgb, fixed3(0, 1, 0), GridColor.b * lerp(0, 1, _GridCamDist));
				}
				else {
					GridFinal.rgb = lerp(GridFinal.rgb, fixed3(0, 1, 0), GridColor.b);
				}

				GridFinal *= GridColor.a;

				half CenterGridSize = lerp(0.005, 0.015, _GridCamDist) / _GridScale;
				if (uv_Control.x > 0.5 - CenterGridSize && uv_Control.x < 0.5 + CenterGridSize)
					GridFinal.rgb = fixed3(0.4, 1, 0);
				else if (uv_Control.y > 0.5 - CenterGridSize && uv_Control.y < 0.5 + CenterGridSize)
					GridFinal.rgb = fixed3(0.4, 1, 0);

				return GridFinal;
			}

            float3 renderGridOverlay(float2 uv){
                float3 Emit = 0;
                if (_Grid > 0) {
					if(_GridType == 1)
						Emit += RenderGrid(_GridTexture, uv, 0, _GridScale);
					else if (_GridType == 2)
						Emit += RenderGrid(_GridTexture, uv, 0.0015, _GridScale / 5.12);
					else if (_GridType == 3)
						Emit += RenderGrid(_GridTexture, uv, 0.0015, 16);
					else
						Emit += RenderGrid(_GridTexture, uv, 0, _GridScale);
				}
                return Emit;
            }

            float4 fragmentShader(VS_OUTPUT inV) : COLOR
            {
                float shaderNumber = 0;
                float4 outColor;

                if (shaderNumber == 0)
                {
                    outColor = TerrainPS(inV, true);
                }
                // else if (shaderNumber == 1)
                // {
                //     outColor = TerrainXP(inV, true);
                // }
                // else if (shaderNumber == 2)
                // {
                //     outColor = Terrain001PS(inV, true);
                // }
                else {
                    outColor = float4(1, 0, 1, 1);
                }

                // outColor = renderFog(outColor);
                // outColor = renderOnlyArea(inV.mPos, outColor);
                // outColor.rgb += renderBrush(inV.mTexWT.xy);
                // outColor = renderSlope(outColor);
                // outColor = renderTerrainType(outColor);
                // outColor.rgb += renderGridOverlay(inV.mTexWT.xy);
                
                return outColor;
            }
            ENDCG
        }
    }
}