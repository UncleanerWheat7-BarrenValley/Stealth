///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

Shader "Hidden/OverCloud/Utilities"
{
	Properties
	{
		
	}
	SubShader
	{
		// Pass 0, render cloud shadows (project 3D noise to plane)
		Pass
		{
			name "CloudShadows"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "OverCloudCore.cginc"
			#include "OverCloudMain.cginc"

			struct v2f
			{
				float4 vertex 	: SV_POSITION;
				float2 texcoord	: TEXCOORD0;
				float3 worldPos : TEXCOORD1;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex 	= UnityObjectToClipPos(v.vertex);
				o.texcoord 	= v.texcoord;

				o.worldPos.x = lerp(_OC_CloudShadowExtentsMinMax.x, _OC_CloudShadowExtentsMinMax.z, o.texcoord.x);
				o.worldPos.z = lerp(_OC_CloudShadowExtentsMinMax.y, _OC_CloudShadowExtentsMinMax.w, o.texcoord.y);
				o.worldPos.y = _OC_CloudAltitude;

				return o;
			}

			float4 Cubic2 (float v)
			{
				float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
				float4 s = n * n * n;
				float x = s.x;
				float y = s.y - 4.0 * s.x;
				float z = s.z - 4.0 * s.y + 6.0 * s.x;
				float w = 6.0 - x - y - z;
				return float4(x, y, z, w) * (1.0/6.0);
			}

			float4 tex2DBicubic2 (sampler2D tex, float2 texCoords, float2 texSize, float2 invTexSize)
			{
				texCoords = frac(texCoords);
				texCoords = texCoords * texSize - 0.5;

				float2 fxy = frac(texCoords);
				texCoords -= fxy;

				float4 xcubic = Cubic2(fxy.x);
				float4 ycubic = Cubic2(fxy.y);

				float4 c = texCoords.xxyy + float2 (-0.5, +1.5).xyxy;

				float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
				float4 offset = c + float4 (xcubic.yw, ycubic.yw) / s;

				offset *= invTexSize.xxyy;

				float4 sample0 = tex2D(tex, offset.xz);
				float4 sample1 = tex2D(tex, offset.yz);
				float4 sample2 = tex2D(tex, offset.xw);
				float4 sample3 = tex2D(tex, offset.yw);

				float sx = s.x / (s.x + s.y);
				float sy = s.z / (s.z + s.w);

				return lerp(
					lerp(sample3, sample2, sx),
					lerp(sample1, sample0, sx),
					sy);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				/*
				float density = 0;

				// Backwards-trace a 4 sample volume shadow
				float volumeHeight = _OC_CloudHeight * 2;
				// Ray march start position
				float3 rs = i.worldPos;
				float  offsetLength = _OC_CloudHeight / abs(_OC_LightDir.y);
				float3 re = rs - _OC_LightDir * offsetLength;
				rs += _OC_LightDir * offsetLength;

				float4 samples;

				float _Opacity = 1;

				for (int u = 0; u < 4; u++)
				{
					float3 t = lerp(rs, re, (u+1) * 0.16666666);
					float3 masks;
					float d = CloudDensity3DBase(t, masks);
					// CloudErosion1(t, d, masks.x);
					// CloudErosion2(t, d, masks.x);
					// The cloud shadows texture isn't high-res enough to capturate all the detail from the noise erosion,
					// so we opt for a smooth but fake reduction in volume instead
					// d -= (1-d) * 0.25 * _OC_NoiseIntensity_A;
					// d -= (1-d) * 0.25 * _OC_NoiseIntensity_B;
					d = min(d * _OC_CellSpan.x * _OC_CloudDensity.y * _Opacity * 4 * 0.01, 1);
					density += d;
					samples[u] = d;
					samples[u] = d * (1 - u / 4);
				}
				// samples *= 0.25;

				float _PrecipitationMul = 1;
				float _OC_Precipitation = 0;
				float _Density = _OC_CloudDensity.x;
				float4 opticalDepth = samples * _Density * 4 * lerp(1, _PrecipitationMul, _OC_Precipitation);
				float4 beer = EXP(-opticalDepth.x * 0.1);

				samples = saturate((samples - 0.5) * _OC_CloudDensity.x + 0.5);
				*/

				// Fade from center of compositor
				// float fade = min(length(i.texcoord - float2(0.5, 0.5)) * 2, 1);
				// fade = 1 - fade  * fade * fade * fade;

				float2 compositorCoords = max(i.worldPos.xz - _OC_CloudWorldExtentsMinMax.xy, 0) * _OC_CloudWorldExtents.zw;

				float shadow = tex2D(_OC_CompositorTex, compositorCoords).r * 5;

				// compositor.r = CloudDensity(i.worldPos);
				// shadow = smoothstep(0, .5, shadow.r);
				// samples.xyzw = tex2D(_OC_ShapeTex, float2(compositor.r, 0.5)) * 3;
				
				return lerp(0, shadow, _OC_CloudShadowsDensity);
				// return lerp(0, samples.xyzw * 0.25, fade * _OC_CloudShadowsIntensity);
			}
			ENDCG
		}

		// Pass 1, combine blurred cloud AO result with compositor texture
		Pass
		{
			name "CloudOcclusionComposite"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex 	: SV_POSITION;
				float2 texcoord	: TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex 	= UnityObjectToClipPos(v.vertex);
				o.texcoord 	= v.texcoord;

				return o;
			}

			sampler2D _CompositorTex;
			sampler2D _CloudOcclusionTex;

			fixed4 frag (v2f i) : SV_Target
			{
				float4 tex = tex2D(_CompositorTex, i.texcoord);
				float  cloudOcclusion = tex2D(_CloudOcclusionTex, i.texcoord).r;
				tex.rgb = tex2D(_CloudOcclusionTex, i.texcoord);
				return tex;
			}
			ENDCG
		}

		// Pass 2, flipped blit. Used to transfer camera result to cubemap face.
		Pass
		{
			name "FlippedBlit"
			Cull Off
			ZWrite Off
			ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D 	_MainTex;
			int			_Flip;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex 		= UnityObjectToClipPos(v.vertex);
				o.texcoord 		= v.texcoord;
				if (_Flip == 1)
					o.texcoord.x = 1 - o.texcoord.x;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, i.texcoord);
				return color;
			}
			ENDCG
		}

		// Pass 3, rain depth map generator
		Pass
		{
			name "RainMaskGen"
			Cull Off
			ZWrite Off
			ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "OverCloudCore.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex 	 : SV_POSITION;
				float2 texcoord  : TEXCOORD0;
				float2 texcoord2 : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _CameraDepthTexture;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;

				// Offset UV
				o.texcoord2	= _OC_RainMaskPosition.xz + _OverCloudOriginOffset.xz;
				o.texcoord2.x += lerp(-_OC_RainMaskRadius, _OC_RainMaskRadius, o.texcoord.x);
				o.texcoord2.y += lerp(-_OC_RainMaskRadius, _OC_RainMaskRadius, o.texcoord.y);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// float2 offset = (tex2D(_OC_RainMaskOffsetTex, i.texcoord2).rg - float2(0.5, 0.5)) * 2;
				// i.texcoord += offset * _OC_RainMaskOffset * _OC_RainMaskTexel.y;
				// Get world-space height from depth
				float linearDepth = 1 - (tex2D(_CameraDepthTexture, i.texcoord));
				float height = _WorldSpaceCameraPos.y - lerp(_ProjectionParams.y, _ProjectionParams.z, linearDepth);
				return height;
			}
			ENDCG
		}

		// Pass 4, noise dither
		Pass
		{
			name "Dither"
			Cull Off
			ZWrite Off
			ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "OverCloudCore.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex 	 : SV_POSITION;
				float2 texcoord  : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _PreDitheredScatteringMask;
			float4 _PixelSize;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;

				return o;
			}

			static const float4 bayer[4] = {
				float4(0.0f, 0.5f, 0.125f, 0.625f),
				float4( 0.75f, 0.22f, 0.875f, 0.375f),
				float4( 0.1875f, 0.6875f, 0.0625f, 0.5625),
				float4( 0.9375f, 0.4375f, 0.8125f, 0.3125)
			};

			fixed4 frag (v2f i) : SV_Target
			{
				float2 bayerUV = floor(frac(i.texcoord * _PixelSize.xy / 4) * 4);
				float n = bayer[bayerUV.x][bayerUV.y];
				return tex2D(_PreDitheredScatteringMask, i.texcoord) + n / 255.0;
			}
			ENDCG
		}

		// Pass 5, generic add pass
		Pass
		{
			name "Add"
			Cull Off
			ZWrite Off
			ZTest Always
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex 	 : SV_POSITION;
				float2 texcoord  : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;

				return o;
			}

			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.texcoord); 
			}
			ENDCG
		}
	}
}