 Shader "Hidden/OverCloud/DownsampleDepth"
 {
 	Properties
 	{

 	}
 	SubShader
 	{
 		Pass
 		{
 			CGPROGRAM
	 			#pragma vertex vert
	 			#pragma fragment frag

	 			#include "UnityCG.cginc"

	 			// #define CHECKERBOARD

				struct appdata
				{
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float4 screenPos : TEXCOORD1;

					UNITY_VERTEX_OUTPUT_STEREO
				};

				v2f vert (appdata v)
				{
					v2f o;
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord;
					// o.screenPos = ComputeNonStereoScreenPos(o.vertex);
					o.screenPos = ComputeScreenPos(o.vertex);
					return o;
				}

				sampler2D _CameraDepthTexture;
				sampler2D _CameraDepthNormalsTexture;
				float4 _PixelSize;
				float4 _PixelSizeDS;
				float2 _HalfTexel;

				uniform fixed4 _CameraDepthTexture_TexelSize;

	 			half4 frag(v2f i) : COLOR
	 			{
	 				#if UNITY_SINGLE_PASS_STEREO
	 					float2 uv = i.screenPos.xy / i.screenPos.w;
	 				#else
	 					float2 uv = i.texcoord;
	 				#endif

	 				#ifdef CHECKERBOARD
						float2 uv0 = uv + float2(-1, -1) * _HalfTexel;
						float2 uv1 = uv + float2(-1,  1) * _HalfTexel;
						float2 uv2 = uv + float2( 1, -1) * _HalfTexel;
						float2 uv3 = uv + float2( 1,  1) * _HalfTexel;

						float d0 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv0);
						float d1 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv1);
						float d2 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv2);
						float d3 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv3);

						float d;
						// Store minimum/maximum depth values in a checkerboard pattern
						if (fmod(floor(uv.x*_PixelSizeDS.x) + floor(uv.y*_PixelSizeDS.y), 2) < 1)
							d = min(d0, min(d1, min(d2, d3)));
						else
							d = max(d0, max(d1, max(d2, d3)));
					#else
						float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
					#endif

					return float4(d, 0, 0, 0);
	 			}
	 			ENDCG
 		}
 	}
 }