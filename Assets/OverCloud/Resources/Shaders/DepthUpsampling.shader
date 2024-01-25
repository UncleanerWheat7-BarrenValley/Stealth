 Shader "Hidden/OverCloud/DepthUpsampling"
 {
 	Properties
 	{
 		_MainTex ("Base (RGB)", 2D) = "white" {}
 	}
 	SubShader
 	{
 		CGINCLUDE
 			#include "UnityCG.cginc"
 			#include "BicubicLib.cginc"
 			#include "OverCloudCore.cginc"

 			UNITY_DECLARE_TEX2D(_MainTex);
			UNITY_DECLARE_TEX2D(_CameraDepthLowRes);

 			#define DEPTH_THRESHOLD_NEAR 0.0001
 			#define DEPTH_THRESHOLD_FAR 0.05
 			// #define FIXED_THRESHOLD 0.1
 			// #define DEBUG_DEPTH_THRESHOLD
 			// #define PASSTHROUGH

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _PixelSize;
			float4 _PixelSizeDS;

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex   = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;

				return o;
			}

			sampler2D _CameraDepthTexture;

			float2 _Threshold;

			float4 _MainTex_ST;
			
 			fixed4 frag(v2f i) : COLOR
 			{
 				#if UNITY_SINGLE_PASS_STEREO
 					// Per-eye texture coordinates
 					i.texcoord.x = i.texcoord.x * 0.5 + 0.5 * unity_StereoEyeIndex;
 				#endif

 				#ifdef PASSTHROUGH
 					return UNITY_SAMPLE_TEX2D(_MainTex, i.texcoord);
 				#endif

 				float3 debugColor = 0;
 				
 				// Sample the high-resolution depth
 				fixed depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord);
				fixed linearDepth = Linear01Depth(depth);

				// Create UVs
 				fixed2 uv0 = i.texcoord - _PixelSizeDS.zw * 0.5;
				fixed2 uv1 = uv0 + fixed2(1, 0) * _PixelSizeDS.zw;
				fixed2 uv2 = uv0 + fixed2(0, 1) * _PixelSizeDS.zw;
				fixed2 uv3 = uv0 + fixed2(1, 1) * _PixelSizeDS.zw;

				// Sample the stored depth footprint
				fixed depthTaps[4] = { 0, 0, 0, 0 };
				depthTaps[0] = Linear01Depth(UNITY_SAMPLE_TEX2D(_CameraDepthLowRes, uv0).r);
				depthTaps[1] = Linear01Depth(UNITY_SAMPLE_TEX2D(_CameraDepthLowRes, uv1).r);
				depthTaps[2] = Linear01Depth(UNITY_SAMPLE_TEX2D(_CameraDepthLowRes, uv2).r);
				depthTaps[3] = Linear01Depth(UNITY_SAMPLE_TEX2D(_CameraDepthLowRes, uv3).r);

				// The color taps are sampled using the depth buffer sampler = avoids hardware bilinear filtering,
				// which would otherwise result in artifacts.
				// Effectively, this method allows us to do selective point filtering.
				fixed4 colorTaps[4] = { fixed4(0, 0, 0, 0), fixed4(0, 0, 0, 0), fixed4(0, 0, 0, 0), fixed4(0, 0, 0, 0) };
				colorTaps[0] = UNITY_SAMPLE_TEX2D_SAMPLER(_MainTex, _CameraDepthLowRes, uv0);
				colorTaps[1] = UNITY_SAMPLE_TEX2D_SAMPLER(_MainTex, _CameraDepthLowRes, uv1);
				colorTaps[2] = UNITY_SAMPLE_TEX2D_SAMPLER(_MainTex, _CameraDepthLowRes, uv2);
				colorTaps[3] = UNITY_SAMPLE_TEX2D_SAMPLER(_MainTex, _CameraDepthLowRes, uv3);

				// Picking the right depth threshold is difficult.
				// Too high a value and the filter will fail to pick up edges.
				// Too low a value and the filter will always activate.
				// Through testing I've found that a distance-based interpolated value works better than a constant one.
				#if !defined(FIXED_THRESHOLD)
					float threshold = lerp(DEPTH_THRESHOLD_NEAR, DEPTH_THRESHOLD_FAR, linearDepth);
				#else
					float threshold = FIXED_THRESHOLD;
				#endif

				// Check all 4 samples and see if we either accept the closest sample or reject all of them
				fixed smallestDelta 	= 1;
				int   nearestDepthInd 	= 0;
				bool reject = true;
				for (int u = 0; u < 4; u++)
				{
					fixed delta  = abs(depthTaps[u] - linearDepth);
					reject = reject && delta < threshold;
					UNITY_BRANCH
					if (delta < smallestDelta)
					{
						smallestDelta	= delta;
						nearestDepthInd = u;
					}
				}

				fixed4 color = 0;

				UNITY_BRANCH
				if (reject)
				{
					// Rejected (eg. non-filtered) pixels should use hardware filtering
					color = UNITY_SAMPLE_TEX2D(_MainTex, i.texcoord);
					debugColor = float3(1, 0, 0);
				}
				else
				{
					color = colorTaps[nearestDepthInd];
					debugColor = float3(0, 0, 1);
				}

				#if defined(DEBUG_DEPTH_THRESHOLD)
					return float4(debugColor, 1);
				#endif

				return color;
 			}
 		ENDCG

 		Pass
 		{
 			// Pass 0, straight transfer
 			
 			ZWrite Off

 			CGPROGRAM
	 			#pragma vertex vert
	 			#pragma fragment frag
	 		ENDCG
 		}

 		Pass
 		{
 			// Pass 1, alpha blend (pre-multiplied)
 			
 			Blend One OneMinusSrcAlpha
 			ZWrite Off

 			CGPROGRAM
	 			#pragma vertex vert
	 			#pragma fragment frag
	 		ENDCG
 		}

 		Pass
 		{
 			// Pass 2, additive
 			
 			Blend One One
 			ZWrite Off

 			CGPROGRAM
	 			#pragma vertex vert
	 			#pragma fragment frag
	 		ENDCG
 		}
 	}
 }