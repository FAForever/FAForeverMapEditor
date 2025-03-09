Shader "FAShaders/Terrain"
{
    Properties
    {
        // It is conventional to begin all property names with an underscore.
        // However, to be as close to the FA shader code as possible, we ignore this here.
        // We use underscores only for variables that are introduced by the editor.

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

    	LowerAlbedoSampler ("Lower Albedo", 2D) = "white" {}
	    LowerNormalSampler ("Lower Normal", 2D) = "bump" {}
        Stratum7AlbedoSampler ("Stratum 7 Albedo", 2D) = "white" {}
	    Stratum7NormalSampler ("Stratum 7 Normal", 2D) = "bump" {}
    	UpperAlbedoSampler ("Upper Albedo", 2D) = "white" {}

        _StratumAlbedoArray ("Albedo array", 2DArray) = "" {}
	    _StratumNormalArray ("Normal array", 2DArray) = "" {}

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

            CGPROGRAM
			#pragma surface surf SimpleLambert vertex:TerrainVS exclude_path:forward nometa
            #pragma multi_compile_fog
			#pragma target 3.5
			#include "Assets/GFX/Shaders/SimpleLambert.cginc"

            // include file that contains UnityObjectToWorldNormal helper function
            #include "UnityCG.cginc"


			int _Slope;
            int _UseSlopeTex;
			sampler2D _SlopeTex;

			int _Grid, _GridType;
            half _GridScale;
			half _GridCamDist;
			sampler2D _GridTexture;

			sampler2D _TerrainNormal;
			sampler2D _TerrainTypeAlbedo;

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

            uniform int _ShaderID;

            // Most light values are already defined in SimpleLambert
            float4 SpecularColor;
            float WaterElevation;

            float LowerAlbedoTile;
            float LowerNormalTile;
            float Stratum0AlbedoTile;
            float Stratum1AlbedoTile;
            float Stratum2AlbedoTile;
            float Stratum3AlbedoTile;
            float Stratum4AlbedoTile;
            float Stratum5AlbedoTile;
            float Stratum6AlbedoTile;
            float Stratum7AlbedoTile;
            float Stratum0NormalTile;
            float Stratum1NormalTile;
            float Stratum2NormalTile;
            float Stratum3NormalTile;
            float Stratum4NormalTile;
            float Stratum5NormalTile;
            float Stratum6NormalTile;
            float Stratum7NormalTile;
            float UpperAlbedoTile;
            float UpperNormalTile;

            float TerrainScale;

            sampler2D UtilitySamplerA;
            sampler2D UtilitySamplerB;
            sampler2D UtilitySamplerC;
            sampler1D WaterRampSampler;

            sampler2D LowerAlbedoSampler;
            sampler2D UpperAlbedoSampler;
            sampler2D LowerNormalSampler;
			sampler2D Stratum7AlbedoSampler;
			sampler2D Stratum7NormalSampler;
       
			UNITY_DECLARE_TEX2DARRAY(_StratumAlbedoArray);
			UNITY_DECLARE_TEX2DARRAY(_StratumNormalArray);

            uniform half4 unity_FogStart;
			uniform half4 unity_FogEnd;

            // This struct has to be named 'Input'. Changing it to VS_OUTPUT does not compile.
            struct Input
            {
                float4 mPos                    : POSITION0;
                // These are absolute world coordinates
                float3 mTexWT                : TEXCOORD1;
                // these three vectors will hold a 3x3 rotation matrix
                // that transforms from tangent to world space
                half3 tspace0 : TEXCOORD2; // tangent.x, bitangent.x, normal.x
                half3 tspace1 : TEXCOORD3; // tangent.y, bitangent.y, normal.y
                half3 tspace2 : TEXCOORD4; // tangent.z, bitangent.z, normal.z
                float3 mViewDirection        : TEXCOORD5;
                float4 nearScales           : TEXCOORD6;
                float4 farScales            : TEXCOORD7;

                float SlopeLerp;
                half fog;
            };

            float4 StratumAlbedoSampler(int layer, float2 uv) {
                return UNITY_SAMPLE_TEX2DARRAY(_StratumAlbedoArray, float3(uv, layer));
            }

            float4 StratumNormalSampler(int layer, float2 uv) {
                return UNITY_SAMPLE_TEX2DARRAY(_StratumNormalArray, float3(uv, layer));
            }

            float3 TangentToWorldSpace(Input v, float3 tnormal) {
                float3 worldNormal;
                if (Stratum7NormalTile < 1.0 && _HideStratum7 == 0) {
                    float3 normal;
                    normal.xz = tex2D(Stratum7NormalSampler, TerrainScale * v.mTexWT.xy).ag * 2 - 1;
                    normal.z = normal.z * -1;
                    // reconstruct y component
                    normal.y = sqrt(1 - dot(normal.xz, normal.xz));

                    // when we read the terrain normals from a texture, we need to build our own TBN matrix
                    float3 tangent = cross(normal, float3(0, 0, 1));
                    float3 bitangent = cross(normal, tangent);
                    float3 TBN0 = float3(tangent.x, bitangent.x, normal.x);
                    float3 TBN1 = float3(tangent.y, bitangent.y, normal.y);
                    float3 TBN2 = float3(tangent.z, bitangent.z, normal.z);

                    worldNormal.x = dot(TBN0, tnormal);
                    worldNormal.y = dot(TBN1, tnormal);
                    worldNormal.z = dot(TBN2, tnormal);
                } else {
                    // transform normal from tangent to world space using our vertex shader data
                    worldNormal.x = dot(v.tspace0, tnormal);
                    worldNormal.y = dot(v.tspace1, tnormal);
                    worldNormal.z = dot(v.tspace2, tnormal);
                }
                return worldNormal;
            }

            // Because the underlying engine is different, the vertex shader has to differ considerably from fa.
            // Still, we try to set up things in a way that we only have to minimally modify the fa pixel shaders
            void TerrainVS(inout appdata_full v, out Input result)
            {
                result.nearScales = float4(Stratum0AlbedoTile.x, Stratum1AlbedoTile.x, Stratum2AlbedoTile.x, Stratum3AlbedoTile.x);
                result.farScales =  float4(Stratum0NormalTile.x, Stratum1NormalTile.x, Stratum2NormalTile.x, Stratum3NormalTile.x);

                // calculate output position
                result.mPos = UnityObjectToClipPos(v.vertex);

                // Unity uses lower left origin, fa uses upper left, so we need to invert the y axis
                // and for some ungodly reason we have a scale factor of 10. I don't know where this comes from.
                result.mTexWT = v.vertex.xzy * float3(10, -10, 10);
                // We also need to move the origin from the bottom corner to the top corner
                result.mTexWT.y += 1.0 / TerrainScale;
                
                result.mViewDirection = normalize(v.vertex.xyz - _WorldSpaceCameraPos.xyz);

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                // compute bitangent from cross product of normal and tangent
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 worldBitangent = cross(worldNormal, worldTangent) * tangentSign;
                // output the tangent space matrix
                result.tspace0 = half3(worldTangent.x, worldBitangent.x, worldNormal.x);
                result.tspace1 = half3(worldTangent.y, worldBitangent.y, worldNormal.y);
                result.tspace2 = half3(worldTangent.z, worldBitangent.z, worldNormal.z);

                result.SlopeLerp = dot(v.normal, half3(0,1,0));
                float pos = length(UnityObjectToViewPos(v.vertex).xyz);
				float diff = unity_FogEnd.x - unity_FogStart.x;
			    float invDiff = 1.0f / diff;
		    	result.fog = saturate((unity_FogEnd.x - pos) * invDiff);
            }

            float4 TerrainNormalsPS( Input inV )
            {
                // sample all the textures we'll need
                float4 mask = saturate(tex2D( UtilitySamplerA, inV.mTexWT * TerrainScale));

                float4 lowerNormal = normalize(tex2D( LowerNormalSampler, inV.mTexWT * TerrainScale * LowerNormalTile ) * 2 - 1);
                float4 stratum0Normal = normalize(StratumNormalSampler(0, inV.mTexWT * TerrainScale * Stratum0NormalTile) * 2 - 1);
                float4 stratum1Normal = normalize(StratumNormalSampler(1, inV.mTexWT * TerrainScale * Stratum1NormalTile) * 2 - 1);
                float4 stratum2Normal = normalize(StratumNormalSampler(2, inV.mTexWT * TerrainScale * Stratum2NormalTile) * 2 - 1);
                float4 stratum3Normal = normalize(StratumNormalSampler(3, inV.mTexWT * TerrainScale * Stratum3NormalTile) * 2 - 1);

                // blend all normals together
                float4 normal = lowerNormal;
                if(_HideStratum0 == 0)
                normal = lerp( normal, stratum0Normal, mask.x );
                if(_HideStratum1 == 0)
                normal = lerp( normal, stratum1Normal, mask.y );
                if(_HideStratum2 == 0)
                normal = lerp( normal, stratum2Normal, mask.z );
                if(_HideStratum3 == 0)
                normal = lerp( normal, stratum3Normal, mask.w );
                normal.xyz = normalize( normal.xyz );

                return normal;
            }

            float4 TerrainPS( Input inV )
            {
                // sample all the textures we'll need
                float4 mask = saturate(tex2D( UtilitySamplerA, inV.mTexWT * TerrainScale) * 2 - 1);
                float4 upperAlbedo = tex2D( UpperAlbedoSampler, inV.mTexWT * TerrainScale * UpperAlbedoTile);
                float4 lowerAlbedo = tex2D( LowerAlbedoSampler, inV.mTexWT * TerrainScale * LowerAlbedoTile);
                float4 stratum0Albedo = StratumAlbedoSampler(0, inV.mTexWT * TerrainScale * Stratum0AlbedoTile);
                float4 stratum1Albedo = StratumAlbedoSampler(1, inV.mTexWT * TerrainScale * Stratum1AlbedoTile);
                float4 stratum2Albedo = StratumAlbedoSampler(2, inV.mTexWT * TerrainScale * Stratum2AlbedoTile);
                float4 stratum3Albedo = StratumAlbedoSampler(3, inV.mTexWT * TerrainScale * Stratum3AlbedoTile);

                // blend all albedos together
                float4 albedo = lowerAlbedo;
                if(_HideStratum0 == 0)
                albedo = lerp( albedo, stratum0Albedo, mask.x );
                if(_HideStratum1 == 0)
                albedo = lerp( albedo, stratum1Albedo, mask.y );
                if(_HideStratum2 == 0)
                albedo = lerp( albedo, stratum2Albedo, mask.z );
                if(_HideStratum3 == 0)
                albedo = lerp( albedo, stratum3Albedo, mask.w );
                albedo.xyz = lerp( albedo.xyz, upperAlbedo.xyz, upperAlbedo.w );

                return albedo;
            }

            float4 TerrainNormalsXP( Input pixel )
            {
                float4 mask0 = saturate(tex2D(UtilitySamplerA,pixel.mTexWT*TerrainScale));
                float4 mask1 = saturate(tex2D(UtilitySamplerB,pixel.mTexWT*TerrainScale));

                float4 lowerNormal = normalize(tex2D(LowerNormalSampler,pixel.mTexWT*TerrainScale*LowerNormalTile)*2-1);
                float4 stratum0Normal = normalize(StratumNormalSampler(0,pixel.mTexWT*TerrainScale*Stratum0NormalTile)*2-1);
                float4 stratum1Normal = normalize(StratumNormalSampler(1,pixel.mTexWT*TerrainScale*Stratum1NormalTile)*2-1);
                float4 stratum2Normal = normalize(StratumNormalSampler(2,pixel.mTexWT*TerrainScale*Stratum2NormalTile)*2-1);
                float4 stratum3Normal = normalize(StratumNormalSampler(3,pixel.mTexWT*TerrainScale*Stratum3NormalTile)*2-1);
                float4 stratum4Normal = normalize(StratumNormalSampler(4,pixel.mTexWT*TerrainScale*Stratum4NormalTile)*2-1);
                float4 stratum5Normal = normalize(StratumNormalSampler(5,pixel.mTexWT*TerrainScale*Stratum5NormalTile)*2-1);
                float4 stratum6Normal = normalize(StratumNormalSampler(6,pixel.mTexWT*TerrainScale*Stratum6NormalTile)*2-1);
                float4 stratum7Normal = normalize(tex2D(Stratum7NormalSampler,pixel.mTexWT*TerrainScale*Stratum7NormalTile)*2-1);

                float4 normal = lowerNormal;
                if(_HideStratum0 == 0)
                normal = lerp(normal,stratum0Normal,mask0.x);
                if(_HideStratum1 == 0)
                normal = lerp(normal,stratum1Normal,mask0.y);
                if(_HideStratum2 == 0)
                normal = lerp(normal,stratum2Normal,mask0.z);
                if(_HideStratum3 == 0)
                normal = lerp(normal,stratum3Normal,mask0.w);
                if(_HideStratum4 == 0)
                normal = lerp(normal,stratum4Normal,mask1.x);
                if(_HideStratum5 == 0)
                normal = lerp(normal,stratum5Normal,mask1.y);
                if(_HideStratum6 == 0)
                normal = lerp(normal,stratum6Normal,mask1.z);
                if(_HideStratum7 == 0)
                normal = lerp(normal,stratum7Normal,mask1.w);
                normal.xyz = normalize( normal.xyz );

                return normal;
            }
            
            float4 TerrainAlbedoXP( Input pixel)
            {
                float3 position = TerrainScale*pixel.mTexWT;

                float4 mask0 = saturate(tex2D(UtilitySamplerA,position)*2-1);
                float4 mask1 = saturate(tex2D(UtilitySamplerB,position)*2-1);

                float4 lowerAlbedo = tex2D(LowerAlbedoSampler,position*LowerAlbedoTile);
                float4 stratum0Albedo = StratumAlbedoSampler(0,position*Stratum0AlbedoTile);
                float4 stratum1Albedo = StratumAlbedoSampler(1,position*Stratum1AlbedoTile);
                float4 stratum2Albedo = StratumAlbedoSampler(2,position*Stratum2AlbedoTile);
                float4 stratum3Albedo = StratumAlbedoSampler(3,position*Stratum3AlbedoTile);
                float4 stratum4Albedo = StratumAlbedoSampler(4,position*Stratum4AlbedoTile);
                float4 stratum5Albedo = StratumAlbedoSampler(5,position*Stratum5AlbedoTile);
                float4 stratum6Albedo = StratumAlbedoSampler(6,position*Stratum6AlbedoTile);
                float4 stratum7Albedo = tex2D(Stratum7AlbedoSampler,position*Stratum7AlbedoTile);
                float4 upperAlbedo = tex2D(UpperAlbedoSampler,position*UpperAlbedoTile);

                float4 albedo = lowerAlbedo;
                if(_HideStratum0 == 0)
                albedo = lerp(albedo,stratum0Albedo,mask0.x);
                if(_HideStratum1 == 0)
                albedo = lerp(albedo,stratum1Albedo,mask0.y);
                if(_HideStratum2 == 0)
                albedo = lerp(albedo,stratum2Albedo,mask0.z);
                if(_HideStratum3 == 0)
                albedo = lerp(albedo,stratum3Albedo,mask0.w);
                if(_HideStratum4 == 0)
                albedo = lerp(albedo,stratum4Albedo,mask1.x);
                if(_HideStratum5 == 0)
                albedo = lerp(albedo,stratum5Albedo,mask1.y);
                if(_HideStratum6 == 0)
                albedo = lerp(albedo,stratum6Albedo,mask1.z);
                if(_HideStratum7 == 0)
                albedo = lerp(albedo,stratum7Albedo,mask1.w);
                if(_HideStratum8 == 0)
                albedo.rgb = lerp(albedo.xyz,upperAlbedo.xyz,upperAlbedo.w);

                return albedo;
            }

			float4 TerrainNormals000( Input pixel, uniform bool halfRange )
            {
                float2 coordinates = pixel.mTexWT * TerrainScale;
                
                float4 mask0 = tex2D(UtilitySamplerA, coordinates);
                float4 mask1 = tex2D(UtilitySamplerB, coordinates);

                if (halfRange) {
                    mask0 = saturate(mask0 * 2 - 1);
                    mask1 = saturate(mask1 * 2 - 1);
                }

                float4 lowerNormal    = normalize(tex2D(LowerNormalSampler, coordinates * LowerNormalTile) * 2 - 1);
                float4 stratum0Normal = normalize(StratumNormalSampler(0, coordinates * Stratum0NormalTile) * 2 - 1);
                float4 stratum1Normal = normalize(StratumNormalSampler(1, coordinates * Stratum1NormalTile) * 2 - 1);
                float4 stratum2Normal = normalize(StratumNormalSampler(2, coordinates * Stratum2NormalTile) * 2 - 1);
                float4 stratum3Normal = normalize(StratumNormalSampler(3, coordinates * Stratum3NormalTile) * 2 - 1);
                float4 stratum4Normal = normalize(StratumNormalSampler(4, coordinates * Stratum4NormalTile) * 2 - 1);
                float4 stratum5Normal = normalize(StratumNormalSampler(5, coordinates * Stratum5NormalTile) * 2 - 1);
                float4 stratum6Normal = normalize(StratumNormalSampler(6, coordinates * Stratum6NormalTile) * 2 - 1);

                float4 normal = lowerNormal;
                if(_HideStratum0 == 0)
                normal = normalize(lerp(normal,stratum0Normal,mask0.x));
                if(_HideStratum1 == 0)
                normal = normalize(lerp(normal,stratum1Normal,mask0.y));
                if(_HideStratum2 == 0)
                normal = normalize(lerp(normal,stratum2Normal,mask0.z));
                if(_HideStratum3 == 0)
                normal = normalize(lerp(normal,stratum3Normal,mask0.w));
                if(_HideStratum4 == 0)
                normal = normalize(lerp(normal,stratum4Normal,mask1.x));
                if(_HideStratum5 == 0)
                normal = normalize(lerp(normal,stratum5Normal,mask1.y));
                if(_HideStratum6 == 0)
                normal = normalize(lerp(normal,stratum6Normal,mask1.z));

                return normal;
            }

			float4 Terrain000AlbedoPS( Input inV, uniform bool halfRange)
            {
                float3 coordinates = TerrainScale * inV.mTexWT;

                float4 mask0 = tex2D(UtilitySamplerA, coordinates.xy);
                float4 mask1 = tex2D(UtilitySamplerB, coordinates.xy);

                if (halfRange) {
                    mask0 = saturate(mask0 * 2 - 1);
                    mask1 = saturate(mask1 * 2 - 1);
                }

                float4 lowerAlbedo =    tex2D(LowerAlbedoSampler, coordinates * LowerAlbedoTile);
                float4 stratum0Albedo = StratumAlbedoSampler(0, coordinates * Stratum0AlbedoTile);
                float4 stratum1Albedo = StratumAlbedoSampler(1, coordinates * Stratum1AlbedoTile);
                float4 stratum2Albedo = StratumAlbedoSampler(2, coordinates * Stratum2AlbedoTile);
                float4 stratum3Albedo = StratumAlbedoSampler(3, coordinates * Stratum3AlbedoTile);
                float4 stratum4Albedo = StratumAlbedoSampler(4, coordinates * Stratum4AlbedoTile);
                float4 stratum5Albedo = StratumAlbedoSampler(5, coordinates * Stratum5AlbedoTile);
                float4 stratum6Albedo = StratumAlbedoSampler(6, coordinates * Stratum6AlbedoTile);
                float4 upperAlbedo =    tex2D(UpperAlbedoSampler, coordinates * UpperAlbedoTile);

                float4 albedo = lowerAlbedo;
                if(_HideStratum0 == 0)
                albedo = lerp(albedo,stratum0Albedo,mask0.x);
                if(_HideStratum1 == 0)
                albedo = lerp(albedo,stratum1Albedo,mask0.y);
                if(_HideStratum2 == 0)
                albedo = lerp(albedo,stratum2Albedo,mask0.z);
                if(_HideStratum3 == 0)
                albedo = lerp(albedo,stratum3Albedo,mask0.w);
                if(_HideStratum4 == 0)
                albedo = lerp(albedo,stratum4Albedo,mask1.x);
                if(_HideStratum5 == 0)
                albedo = lerp(albedo,stratum5Albedo,mask1.y);
                if(_HideStratum6 == 0)
                albedo = lerp(albedo,stratum6Albedo,mask1.z);
                if(_HideStratum8 == 0)
                albedo.rgb = lerp(albedo.xyz,upperAlbedo.xyz,upperAlbedo.w);

                return albedo;
            }

            float4 splatLerp(float4 t1, float4 t2, float t2height, float opacity, uniform float blurriness = 0.06) {
                // We need to increase the contrast of the height
                float height2 = (1.6 * (t2height * (1 - 2 * blurriness) + blurriness) - 0.3) + opacity;
                float threshold = max(1, height2) - blurriness;
                float factor = 0;
                if (opacity > 0) {
                    factor = (opacity >= 1) ? 1 : max(height2 - threshold, 0) / blurriness;
                }
                return lerp(t1, t2, factor);
            }

            float3 splatBlendNormal(float3 n1, float3 n2, float t2height, float opacity, uniform float blurriness = 0.06) {
                float height2 = (1.6 * (t2height * (1 - 2 * blurriness) + blurriness) - 0.3) + opacity;
                float threshold = max(1, height2) - blurriness;
                float factor = 0;
                if (opacity > 0) {
                    factor = (opacity >= 1) ? 1 : max(height2 - threshold, 0) / blurriness;
                }
                // This modification is to make low opacity normal maps more visible,
                // as we notice small changes to the albedo maps more easily.
                // The value of 0.6 is just eyeballed.
                float factormodified = pow(factor, 0.6);
                // UDN blending
                return normalize(float3((n1.xy * (1 - factormodified) + n2.xy * factormodified), n1.z));
            }

            /* # Sample the 2D 2x2 PBR texture atlas # */
            /* To prevent bleeding from the neighboring tiles, we need to work with padding */
            float4 atlas2D(float2 uv, uniform float2 offset) {
                // We need to manually provide the derivatives to prevent seams.
                // See https://forum.unity.com/threads/tiling-textures-within-an-atlas-by-wrapping-uvs-within-frag-shader-getting-artifacts.535793/
                float2 uv_ddx = ddx(uv) / 8;
                float2 uv_ddy = ddy(uv) / 8;
                uv.x = frac(uv.x) / 4 + offset.x + 0.125;
                uv.y = frac(uv.y) / 4 + offset.y + 0.125;
                return tex2Dgrad(UpperAlbedoSampler, uv, uv_ddx, uv_ddy);
            }

            float4 sampleAlbedoStratum(int layer, float2 position, uniform float2 scale, uniform float2 offset, uniform bool firstBatch) {
                float4 albedo = StratumAlbedoSampler(layer, position * scale);
                // store roughness in albedo alpha so we get the roughness splatting for free
                if (firstBatch) {
                    albedo.a = atlas2D(position * scale, offset).x;
                } else {
                    albedo.a = atlas2D(position * scale, offset).z;
                }
                return albedo;
            }
            
            float4 sampleAlbedo(sampler2D s, float2 position, uniform float2 scale, uniform float2 offset, uniform bool firstBatch) {
                float4 albedo = tex2D(s, position * scale);
                // store roughness in albedo alpha so we get the roughness splatting for free
                if (firstBatch) {
                    albedo.a = atlas2D(position * scale, offset).x;
                } else {
                    albedo.a = atlas2D(position * scale, offset).z;
                }
                return albedo;
            }

            float sampleHeight(float2 position, uniform float2 nearScale, uniform float2 farScale, uniform float2 offset, uniform bool firstBatch) {
                float heightNear;
                float heightFar;
                if (firstBatch) {
                    heightNear = atlas2D(position * nearScale, offset).y;
                    heightFar = atlas2D(position * farScale, offset).y;
                } else {
                    heightNear = atlas2D(position * nearScale, offset).w;
                    heightFar = atlas2D(position * farScale, offset).w;
                }
                return (heightNear + heightFar) / 2;
            }
			
            float blendHeight(float3 position, float2 blendWeights, uniform float2 nearscale, uniform float2 farscale, uniform float2 offset, uniform bool firstBatch) {
                float heightNearXZ;
                float heightFarXZ;
                float heightNearYZ;
                float heightFarYZ;
                if (firstBatch) {
                    heightNearXZ = atlas2D(position.xz * nearscale, offset).y;
                    heightFarXZ = atlas2D(position.xz * farscale, offset).y;
                    heightNearYZ = atlas2D(position.yz * nearscale, offset).y;
                    heightFarYZ = atlas2D(position.yz * farscale, offset).y;
                } else {
                    heightNearXZ = atlas2D(position.xz * nearscale, offset).w;
                    heightFarXZ = atlas2D(position.xz * farscale, offset).w;
                    heightNearYZ = atlas2D(position.yz * nearscale, offset).w;
                    heightFarYZ = atlas2D(position.yz * farscale, offset).w;
                }
                return (heightNearYZ + heightFarYZ) / 2 * blendWeights.x + (heightNearXZ + heightFarXZ) / 2 * blendWeights.y;
            }

			float2 calculateBlendWeights(float2 position) {
                float2 terrainNormal = tex2D(Stratum7NormalSampler, position).ag * 2 - 1;
                float2 blendWeights = pow(abs(terrainNormal), 3);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y);
                return blendWeights;
            }

            float3 blendMacrotexture (float2 position, uniform float macrotextureblend, float3 albedo) {
                if (macrotextureblend == 1) {
                    float4 macrotexture = tex2D(LowerAlbedoSampler, position.xy * LowerAlbedoTile.xx);
                    albedo = lerp(albedo, macrotexture.rgb, macrotexture.a);
                } else if (macrotextureblend == 2) {
                    float4 macrotexture = tex2D(LowerAlbedoSampler, position.xy * LowerAlbedoTile.xx);
                    albedo = lerp(albedo, 2 * macrotexture.rgb * albedo, macrotexture.a);
                }
                return albedo;
            }

            float3 Terrain200NormalsPS ( Input inV, uniform bool halfRange )
            {
                float2 position;
                position.xy = TerrainScale * inV.mTexWT;
                // 30° rotation
                float2x2 rotationMatrix = float2x2(float2(0.866, -0.5), float2(0.5, 0.866));
                float2 rotated_pos = mul(position.xy, rotationMatrix);

                float4 mask0 = tex2D(UtilitySamplerA, position.xy);
                float4 mask1 = tex2D(UtilitySamplerB, position.xy);

                if (halfRange) {
                    mask0 = saturate(mask0 * 2 - 1);
                    mask1 = saturate(mask1 * 2 - 1);
                }

                float3 lowerNormal    = normalize(tex2D(LowerNormalSampler,  position.xy * LowerAlbedoTile.xx).rgb * 2 - 1);
                float3 stratum0Normal = normalize(StratumNormalSampler(0, rotated_pos * Stratum0AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum1Normal = normalize(StratumNormalSampler(1, position.xy * Stratum1AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum2Normal = normalize(StratumNormalSampler(2, rotated_pos * Stratum2AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum3Normal = normalize(StratumNormalSampler(3, position.xy * Stratum3AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum4Normal = normalize(StratumNormalSampler(4, rotated_pos * Stratum4AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum5Normal = normalize(StratumNormalSampler(5, position.xy * Stratum5AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum6Normal = normalize(StratumNormalSampler(6, rotated_pos * Stratum6AlbedoTile.xx).rgb * 2 - 1);

                // We need to rotate the normal vectors back.
                // I thought we would have to flip the multiplication order,
                // compared to the uv rotation, but empirically this is correct.
                stratum0Normal.xy = mul(stratum0Normal.xy, rotationMatrix);
                stratum2Normal.xy = mul(stratum2Normal.xy, rotationMatrix);
                stratum4Normal.xy = mul(stratum4Normal.xy, rotationMatrix);
                stratum6Normal.xy = mul(stratum6Normal.xy, rotationMatrix);
                
                float stratum0Height = sampleHeight(rotated_pos, Stratum0AlbedoTile.xx, Stratum0NormalTile.xx, float2(0.5, 0.0), true);
                float stratum1Height = sampleHeight(position.xy, Stratum1AlbedoTile.xx, Stratum1NormalTile.xx, float2(0.0, 0.5), true);
                float stratum2Height = sampleHeight(rotated_pos, Stratum2AlbedoTile.xx, Stratum2NormalTile.xx, float2(0.5, 0.5), true);
                float stratum3Height = sampleHeight(position.xy, Stratum3AlbedoTile.xx, Stratum3NormalTile.xx, float2(0.0, 0.0), false);
                float stratum4Height = sampleHeight(rotated_pos, Stratum4AlbedoTile.xx, Stratum4NormalTile.xx, float2(0.5, 0.0), false);
                float stratum5Height = sampleHeight(position.xy, Stratum5AlbedoTile.xx, Stratum5NormalTile.xx, float2(0.0, 0.5), false);
                float stratum6Height = sampleHeight(rotated_pos, Stratum6AlbedoTile.xx, Stratum6NormalTile.xx, float2(0.5, 0.5), false);

                float3 normal = lowerNormal;
                if(_HideStratum0 == 0)
                normal = splatBlendNormal(normal, stratum0Normal, stratum0Height, mask0.x, SpecularColor.r);
                if(_HideStratum1 == 0)
                normal = splatBlendNormal(normal, stratum1Normal, stratum1Height, mask0.y, SpecularColor.r);
                if(_HideStratum2 == 0)
                normal = splatBlendNormal(normal, stratum2Normal, stratum2Height, mask0.z, SpecularColor.r);
                if(_HideStratum3 == 0)
                normal = splatBlendNormal(normal, stratum3Normal, stratum3Height, mask0.w, SpecularColor.r);
                if(_HideStratum4 == 0)
                normal = splatBlendNormal(normal, stratum4Normal, stratum4Height, mask1.x, SpecularColor.r);
                if(_HideStratum5 == 0)
                normal = splatBlendNormal(normal, stratum5Normal, stratum5Height, mask1.y, SpecularColor.r);
                if(_HideStratum6 == 0)
                normal = splatBlendNormal(normal, stratum6Normal, stratum6Height, mask1.z, SpecularColor.r);

                return normal;
            }

            float4 Terrain200AlbedoPS ( Input inV, uniform bool halfRange, uniform float macrotextureblend )
            {
                float2 position;
                position.xy = TerrainScale * inV.mTexWT;
                // 30° rotation
                float2x2 rotationMatrix = float2x2(float2(0.866, -0.5), float2(0.5, 0.866));
                float2 rotated_pos = mul(position.xy, rotationMatrix);

                float4 mask0 = tex2D(UtilitySamplerA, position.xy);
                float4 mask1 = tex2D(UtilitySamplerB, position.xy);

                if (halfRange) {
                    mask0 = saturate(mask0 * 2 - 1);
                    // Don't touch the roughness mask
                    mask1.xyz = saturate(mask1.xyz * 2 - 1);
                }

                // This shader wouldn't compile because it would have to store too many variables if we didn't use this trick in the vertex shader
                float4 lowerAlbedo =    sampleAlbedo(LowerAlbedoSampler, position.xy, LowerAlbedoTile.xx,    float2(0.0, 0.0), true);
                float4 stratum0Albedo = sampleAlbedoStratum(0, rotated_pos, inV.nearScales.xx,     float2(0.5, 0.0), true);
                float4 stratum1Albedo = sampleAlbedoStratum(1, position.xy, inV.nearScales.yy,     float2(0.0, 0.5), true);
                float4 stratum2Albedo = sampleAlbedoStratum(2, rotated_pos, inV.nearScales.zz,     float2(0.5, 0.5), true);
                float4 stratum3Albedo = sampleAlbedoStratum(3, position.xy, inV.nearScales.ww,     float2(0.0, 0.0), false);
                float4 stratum4Albedo = sampleAlbedoStratum(4, rotated_pos, Stratum4AlbedoTile.xx, float2(0.5, 0.0), false);
                float4 stratum5Albedo = sampleAlbedoStratum(5, position.xy, Stratum5AlbedoTile.xx, float2(0.0, 0.5), false);
                float4 stratum6Albedo = sampleAlbedoStratum(6, rotated_pos, Stratum6AlbedoTile.xx, float2(0.5, 0.5), false);

                float stratum0Height = sampleHeight(rotated_pos, inV.nearScales.xx,     inV.farScales.xx,      float2(0.5, 0.0), true);
                float stratum1Height = sampleHeight(position.xy, inV.nearScales.yy,     inV.farScales.yy,      float2(0.0, 0.5), true);
                float stratum2Height = sampleHeight(rotated_pos, inV.nearScales.zz,     inV.farScales.zz,      float2(0.5, 0.5), true);
                float stratum3Height = sampleHeight(position.xy, inV.nearScales.ww,     inV.farScales.ww,      float2(0.0, 0.0), false);
                float stratum4Height = sampleHeight(rotated_pos, Stratum4AlbedoTile.xx, Stratum4NormalTile.xx, float2(0.5, 0.0), false);
                float stratum5Height = sampleHeight(position.xy, Stratum5AlbedoTile.xx, Stratum5NormalTile.xx, float2(0.0, 0.5), false);
                float stratum6Height = sampleHeight(rotated_pos, Stratum6AlbedoTile.xx, Stratum6NormalTile.xx, float2(0.5, 0.5), false);

                float4 albedo = lowerAlbedo;
                if(_HideStratum0 == 0)
                albedo = splatLerp(albedo, stratum0Albedo, stratum0Height, mask0.x, SpecularColor.r);
                if(_HideStratum1 == 0)
                albedo = splatLerp(albedo, stratum1Albedo, stratum1Height, mask0.y, SpecularColor.r);
                if(_HideStratum2 == 0)
                albedo = splatLerp(albedo, stratum2Albedo, stratum2Height, mask0.z, SpecularColor.r);
                if(_HideStratum3 == 0)
                albedo = splatLerp(albedo, stratum3Albedo, stratum3Height, mask0.w, SpecularColor.r);
                if(_HideStratum4 == 0)
                albedo = splatLerp(albedo, stratum4Albedo, stratum4Height, mask1.x, SpecularColor.r);
                if(_HideStratum5 == 0)
                albedo = splatLerp(albedo, stratum5Albedo, stratum5Height, mask1.y, SpecularColor.r);
                if(_HideStratum6 == 0)
                albedo = splatLerp(albedo, stratum6Albedo, stratum6Height, mask1.z, SpecularColor.r);
                albedo.rgb = blendMacrotexture(position.xy, macrotextureblend, albedo.rgb);

                // We need to add 0.01 as the reflection disappears at 0
                float roughness = saturate(albedo.a * mask1.w * 2 + 0.01);

                return float4(albedo.rgb, roughness);
            }

            // Stratum2 and Stratum3 use biplanar mapping to improve cliff texturing
            float3 Terrain200BNormalsPS ( Input inV, uniform bool halfRange )
            {
                // height is now in the z coordinate
                float3 position = TerrainScale.xxx * inV.mTexWT;
                // 30° rotation
                float2x2 rotationMatrix = float2x2(float2(0.866, -0.5), float2(0.5, 0.866));
                float2 rotated_pos = mul(position.xy, rotationMatrix);

                float4 mask0 = tex2D(UtilitySamplerA, position.xy);
                float4 mask1 = tex2D(UtilitySamplerB, position.xy);

                if (halfRange) {
                    mask0 = saturate(mask0 * 2 - 1);
                    mask1 = saturate(mask1 * 2 - 1);
                }

                float3 lowerNormal    = normalize(tex2D(LowerNormalSampler,  position.xy * LowerAlbedoTile.xx).rgb * 2 - 1);
                float3 stratum0Normal = normalize(StratumNormalSampler(0, rotated_pos * Stratum0AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum1Normal = normalize(StratumNormalSampler(1, position.xy * Stratum1AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum4Normal = normalize(StratumNormalSampler(4, rotated_pos * Stratum4AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum5Normal = normalize(StratumNormalSampler(5, position.xy * Stratum5AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum6Normal = normalize(StratumNormalSampler(6, rotated_pos * Stratum6AlbedoTile.xx).rgb * 2 - 1);

                // we need to rotate the normal vectors back
                stratum0Normal.xy = mul(stratum0Normal.xy, rotationMatrix);
                stratum4Normal.xy = mul(stratum4Normal.xy, rotationMatrix);
                stratum6Normal.xy = mul(stratum6Normal.xy, rotationMatrix);
                
                float stratum0Height = sampleHeight(rotated_pos, Stratum0AlbedoTile.xx, Stratum0NormalTile.xx, float2(0.5, 0.0), true);
                float stratum1Height = sampleHeight(position.xy, Stratum1AlbedoTile.xx, Stratum1NormalTile.xx, float2(0.0, 0.5), true);
                float stratum4Height = sampleHeight(rotated_pos, Stratum4AlbedoTile.xx, Stratum4NormalTile.xx, float2(0.5, 0.0), false);
                float stratum5Height = sampleHeight(position.xy, Stratum5AlbedoTile.xx, Stratum5NormalTile.xx, float2(0.0, 0.5), false);
                float stratum6Height = sampleHeight(rotated_pos, Stratum6AlbedoTile.xx, Stratum6NormalTile.xx, float2(0.5, 0.5), false);

                float2 blendWeights = calculateBlendWeights(position.xy);
                float3 stratum2NormalXZ = normalize(StratumNormalSampler(2, position.xz * Stratum2AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum2NormalYZ = normalize(StratumNormalSampler(2, position.yz * Stratum2AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum2Normal = stratum2NormalYZ * blendWeights.x + stratum2NormalXZ * blendWeights.y;
                float3 stratum3NormalXZ = normalize(StratumNormalSampler(3, position.xz * Stratum3AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum3NormalYZ = normalize(StratumNormalSampler(3, position.yz * Stratum3AlbedoTile.xx).rgb * 2 - 1);
                float3 stratum3Normal = stratum3NormalYZ * blendWeights.x + stratum3NormalXZ * blendWeights.y;

                float stratum2Height =  blendHeight(position, blendWeights, Stratum2AlbedoTile.xx, Stratum2NormalTile.xx, float2(0.5, 0.5), true);
                float stratum3Height =  blendHeight(position, blendWeights, Stratum3AlbedoTile.xx, Stratum3NormalTile.xx, float2(0.0, 0.0), false);

                float3 normal = lowerNormal;
                if(_HideStratum0 == 0)
                normal = splatBlendNormal(normal, stratum0Normal, stratum0Height, mask0.x, SpecularColor.r);
                if(_HideStratum1 == 0)
                normal = splatBlendNormal(normal, stratum1Normal, stratum1Height, mask0.y, SpecularColor.r);
                if(_HideStratum2 == 0)
                normal = splatBlendNormal(normal, stratum2Normal, stratum2Height, mask0.z, SpecularColor.r);
                if(_HideStratum3 == 0)
                normal = splatBlendNormal(normal, stratum3Normal, stratum3Height, mask0.w, SpecularColor.r);
                if(_HideStratum4 == 0)
                normal = splatBlendNormal(normal, stratum4Normal, stratum4Height, mask1.x, SpecularColor.r);
                if(_HideStratum5 == 0)
                normal = splatBlendNormal(normal, stratum5Normal, stratum5Height, mask1.y, SpecularColor.r);
                if(_HideStratum6 == 0)
                normal = splatBlendNormal(normal, stratum6Normal, stratum6Height, mask1.z, SpecularColor.r);

                return normal;
            }

            float4 Terrain200BAlbedoPS ( Input inV, uniform bool halfRange, uniform float macrotextureblend )
            {
                float3 position = TerrainScale.xxx * inV.mTexWT;
                // 30° rotation
                float2x2 rotationMatrix = float2x2(float2(0.866, -0.5), float2(0.5, 0.866));
                float2 rotated_pos = mul(position.xy, rotationMatrix);

                float4 mask0 = tex2D(UtilitySamplerA, position.xy);
                float4 mask1 = tex2D(UtilitySamplerB, position.xy);

                if (halfRange) {
                    mask0 = saturate(mask0 * 2 - 1);
                    // Don't touch the roughness mask
                    mask1.xyz = saturate(mask1.xyz * 2 - 1);
                }

                // This shader wouldn't compile because it would have to store too many variables if we didn't use this trick in the vertex shader
                float4 lowerAlbedo =    sampleAlbedo(LowerAlbedoSampler,    position.xy, LowerAlbedoTile.xx,    float2(0.0, 0.0), true);
                float4 stratum0Albedo = sampleAlbedoStratum(0, rotated_pos, inV.nearScales.xx,     float2(0.5, 0.0), true);
                float4 stratum1Albedo = sampleAlbedoStratum(1, position.xy, inV.nearScales.yy,     float2(0.0, 0.5), true);
                float4 stratum4Albedo = sampleAlbedoStratum(4, rotated_pos, Stratum4AlbedoTile.xx, float2(0.5, 0.0), false);
                float4 stratum5Albedo = sampleAlbedoStratum(5, position.xy, Stratum5AlbedoTile.xx, float2(0.0, 0.5), false);
                float4 stratum6Albedo = sampleAlbedoStratum(6, rotated_pos, Stratum6AlbedoTile.xx, float2(0.5, 0.5), false);

                float stratum0Height = sampleHeight(rotated_pos, inV.nearScales.xx,     inV.farScales.xx,      float2(0.5, 0.0), true);
                float stratum1Height = sampleHeight(position.xy, inV.nearScales.yy,     inV.farScales.yy,      float2(0.0, 0.5), true);
                float stratum4Height = sampleHeight(rotated_pos, Stratum4AlbedoTile.xx, Stratum4NormalTile.xx, float2(0.5, 0.0), false);
                float stratum5Height = sampleHeight(position.xy, Stratum5AlbedoTile.xx, Stratum5NormalTile.xx, float2(0.0, 0.5), false);
                float stratum6Height = sampleHeight(rotated_pos, Stratum6AlbedoTile.xx, Stratum6NormalTile.xx, float2(0.5, 0.5), false);

                float2 blendWeights = calculateBlendWeights(position.xy);
                float4 stratum2AlbedoXZ = sampleAlbedoStratum(2, position.xz, inV.nearScales.zz, float2(0.5, 0.5), true);
                float4 stratum2AlbedoYZ = sampleAlbedoStratum(2, position.yz, inV.nearScales.zz, float2(0.5, 0.5), true);
                float4 stratum2Albedo = stratum2AlbedoYZ * blendWeights.x + stratum2AlbedoXZ * blendWeights.y;
                float4 stratum3AlbedoXZ = sampleAlbedoStratum(3, position.xz, inV.nearScales.ww, float2(0.0, 0.0), false);
                float4 stratum3AlbedoYZ = sampleAlbedoStratum(3, position.yz, inV.nearScales.ww, float2(0.0, 0.0), false);
                float4 stratum3Albedo = stratum3AlbedoYZ * blendWeights.x + stratum3AlbedoXZ * blendWeights.y;

                float stratum2Height =  blendHeight(position, blendWeights, inV.nearScales.zz, inV.farScales.zz, float2(0.5, 0.5), true);
                float stratum3Height =  blendHeight(position, blendWeights, inV.nearScales.ww, inV.farScales.ww, float2(0.0, 0.0), false);

                float4 albedo = lowerAlbedo;
                if(_HideStratum0 == 0)
                albedo = splatLerp(albedo, stratum0Albedo, stratum0Height, mask0.x, SpecularColor.r);
                if(_HideStratum1 == 0)
                albedo = splatLerp(albedo, stratum1Albedo, stratum1Height, mask0.y, SpecularColor.r);
                if(_HideStratum2 == 0)
                albedo = splatLerp(albedo, stratum2Albedo, stratum2Height, mask0.z, SpecularColor.r);
                if(_HideStratum3 == 0)
                albedo = splatLerp(albedo, stratum3Albedo, stratum3Height, mask0.w, SpecularColor.r);
                if(_HideStratum4 == 0)
                albedo = splatLerp(albedo, stratum4Albedo, stratum4Height, mask1.x, SpecularColor.r);
                if(_HideStratum5 == 0)
                albedo = splatLerp(albedo, stratum5Albedo, stratum5Height, mask1.y, SpecularColor.r);
                if(_HideStratum6 == 0)
                albedo = splatLerp(albedo, stratum6Albedo, stratum6Height, mask1.z, SpecularColor.r);
                albedo.rgb = blendMacrotexture(position.xy, macrotextureblend, albedo.rgb);

                // We need to add 0.01 as the reflection disappears at 0
                float roughness = saturate(albedo.a * mask1.w * 2 + 0.01);

                return float4(albedo.rgb, roughness);
            }

            float3 renderBrush(float2 uv){
                float3 Emit = 0;
                if (_Brush > 0) {
                    uv.y = 1-uv.y;
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

            float3 renderSlope(Input IN){
                float3 Emit = 0;
                if (_Slope > 0) {
					float2 uv = TerrainScale * IN.mTexWT;
					uv.y = 1 - uv.y;
					half3 SlopeColor = tex2D(_SlopeTex, uv).rgb * 0.8;
                    if (IN.mTexWT.z < WaterElevation * 10) {
                        SlopeColor += half3(0, 0, 0.1);
                    }
                    Emit = SlopeColor;
				}
                return Emit;
            }

            float3 renderTerrainType(float3 albedo, float2 uv){
                if(_HideTerrainType == 0) {
					float4 TerrainTypeAlbedo = tex2D (_TerrainTypeAlbedo, uv);
					albedo = lerp(albedo, TerrainTypeAlbedo, TerrainTypeAlbedo.a*_TerrainTypeCapacity);
				}
                return albedo;
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

                // central axes
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
					if(_GridType == 1) // build
						Emit += RenderGrid(_GridTexture, uv, 0, _GridScale);
					else if (_GridType == 2) // general
						Emit += RenderGrid(_GridTexture, uv, 0.0015, _GridScale / 5.12);
					else if (_GridType == 3) // AI
						Emit += RenderGrid(_GridTexture, uv, 0.0015, 16);
					else //standard
						Emit += RenderGrid(_GridTexture, uv, 0, _GridScale);
				}
                return Emit;
            }

            // The decals get written directly in the gBuffer, so here we only prepare all necessary terrain inputs.
            // The actual lighting calculations happen in Assets\GFX\Shaders\Deferred\Internal-DeferredShading.shader
            // This way the decals and the terrain have consistent lighting
            void surf(Input inV, inout CustomSurfaceOutput o)
            {
                float3 position = TerrainScale * inV.mTexWT.xyz;
                if (_ShaderID == -10)
                {
                    float4 albedo = TerrainPS(inV);
                    o.Albedo = albedo.rgb;
                    o.Alpha = albedo.a; // for specularity

                    float3 normal = TangentToWorldSpace(inV, TerrainNormalsPS(inV).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                }
                else if (_ShaderID == -20)
                {
                    float4 albedo = TerrainAlbedoXP(inV);
                    o.Albedo = albedo.rgb;
                    o.Alpha = albedo.a; // for specularity

                    float3 normal = TangentToWorldSpace(inV, TerrainNormalsXP(inV).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    o.MapShadow = 1;
                    o.AmbientOcclusion = 1;
                }
                else if (_ShaderID == 20)
                {
                    float4 albedo = Terrain000AlbedoPS(inV, true);
                    o.Albedo = albedo.rgb;
                    o.Alpha = albedo.a; // for specularity

                    float3 normal = TangentToWorldSpace(inV, TerrainNormals000(inV, false).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 0)
                {
                    float4 albedo = Terrain000AlbedoPS(inV, false);
                    o.Albedo = albedo.rgb;
                    o.Alpha = albedo.a; // for specularity

                    float3 normal = TangentToWorldSpace(inV, TerrainNormals000(inV, false).xyz);
                    o.wNormal = normalize(normal);
                    
                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 50)
                {
                    float4 albedo = Terrain000AlbedoPS(inV, true);
                    o.Albedo = albedo.rgb;
                    o.Alpha = albedo.a; // for specularity

                    float3 normal = TangentToWorldSpace(inV, TerrainNormals000(inV, true).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 200)
                {
                    float4 albedo = Terrain200AlbedoPS(inV, false, 0);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200NormalsPS(inV, false).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 250)
                {
                    float4 albedo = Terrain200AlbedoPS(inV, true, 0);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200NormalsPS(inV, true).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 201)
                {
                    float4 albedo = Terrain200AlbedoPS(inV, false, 1);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200NormalsPS(inV, false).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 251)
                {
                    float4 albedo = Terrain200AlbedoPS(inV, true, 1);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200NormalsPS(inV, true).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 202)
                {
                    float4 albedo = Terrain200AlbedoPS(inV, false, 2);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200NormalsPS(inV, false).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 252)
                {
                    float4 albedo = Terrain200AlbedoPS(inV, true, 2);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200NormalsPS(inV, true).xyz);
                    o.wNormal = normalize(normal);

                    o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 210)
                {
                    float4 albedo = Terrain200BAlbedoPS(inV, false, 0);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200BNormalsPS(inV, false).xyz);
                    o.wNormal = normalize(normal);
                    
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.WaterDepth = terrainInfo.r;
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 260)
                {
                    float4 albedo = Terrain200BAlbedoPS(inV, true, 0);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200BNormalsPS(inV, true).xyz);
                    o.wNormal = normalize(normal);

                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.WaterDepth = terrainInfo.r;
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 211)
                {
                    float4 albedo = Terrain200BAlbedoPS(inV, false, 1);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200BNormalsPS(inV, false).xyz);
                    o.wNormal = normalize(normal);
                    
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.WaterDepth = terrainInfo.r;
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 261)
                {
                    float4 albedo = Terrain200BAlbedoPS(inV, true, 1);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200BNormalsPS(inV, true).xyz);
                    o.wNormal = normalize(normal);

                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.WaterDepth = terrainInfo.r;
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 212)
                {
                    float4 albedo = Terrain200BAlbedoPS(inV, false, 2);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200BNormalsPS(inV, false).xyz);
                    o.wNormal = normalize(normal);
                    
                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.WaterDepth = terrainInfo.r;
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else if (_ShaderID == 262)
                {
                    float4 albedo = Terrain200BAlbedoPS(inV, true, 2);
                    o.Albedo = albedo.rgb;
                    o.Roughness = albedo.a;

                    float3 normal = TangentToWorldSpace(inV, Terrain200BNormalsPS(inV, true).xyz);
                    o.wNormal = normalize(normal);

                    if (Stratum7AlbedoTile < 1 && _HideStratum7 == 0) {
                        float4 terrainInfo = tex2D(Stratum7AlbedoSampler, position.xy);
                        o.WaterDepth = terrainInfo.r;
                        o.MapShadow = terrainInfo.a;
                        o.AmbientOcclusion = terrainInfo.g;
                    } else {
                        o.WaterDepth = tex2D(UtilitySamplerC, position.xy).g;
                        o.MapShadow = 1;
                        o.AmbientOcclusion = 1;
                    }
                }
                else {
                    o.Albedo = float3(1, 0, 1);
                }

                o.Emission = renderBrush(position.xy);
                o.Emission += renderSlope(inV);
                o.Albedo = renderTerrainType(o.Albedo, position.xy);
                o.Emission += renderGridOverlay(position.xy);

                // fog
                o.Albedo = lerp(0, o.Albedo, inV.fog);
				o.Emission = lerp(unity_FogColor, o.Emission, inV.fog);
            }
            ENDCG
    }
}
