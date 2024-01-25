///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

Shader "Hidden/OverCloud/ScatteringMask"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "Queue"="Background" "RenderType"="Background" }
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ SAMPLE_COUNT_LOW SAMPLE_COUNT_MEDIUM

			#include "UnityCG.cginc"
			#include "Atmosphere.cginc"
			#include "Skybox.cginc"
			#include "OverCloudMain.cginc"
			#define VARIABLE_STEP_SIZE

			#if SAMPLE_COUNT_LOW
				#define SAMPLE_COUNT 32
				#define SAMPLE_COUNT_INV 0.0625
			#elif SAMPLE_COUNT_MEDIUM
				#define SAMPLE_COUNT 64
				#define SAMPLE_COUNT_INV 0.03125
			#else // SAMPLE_COUNT_HIGH
				#define SAMPLE_COUNT 128
				#define SAMPLE_COUNT_INV 0.015625
			#endif

			struct v2f
			{
				float4 vertex 		: SV_POSITION;
				float2 texcoord		: TEXCOORD0;
				float4 screenPos 	: TEXCOORD1;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D 	_MainTex;
			fixed4		_Color;

			v2f vert (appdata_full v, uint vid : SV_VertexID)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex 	= UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.texcoord 	= v.texcoord;

				return o;
			}

			float2 _OC_ScatteringMaskRadius;
			float2 _ShadowDistance;
			float  _Intensity;
			float  _Floor;
			float  _CascadedShadowsEnabled;
			float  _CascadeShadowMapPresent;

			float4 _PixelSize;
			// float4 _PixelSizeDS;

			sampler2D _CameraDepthLowRes;

			static const float4 bayer[4] = {
				float4(0.0f, 0.5f, 0.125f, 0.625f),
				float4( 0.75f, 0.22f, 0.875f, 0.375f),
				float4( 0.1875f, 0.6875f, 0.0625f, 0.5625),
				float4( 0.9375f, 0.4375f, 0.8125f, 0.3125)
			};

			fixed4 frag (v2f i) : SV_Target
			{
				float2 screenUV = UNITY_PROJ_COORD(i.screenPos);

				// Sample scene/backbuffer
				float3 color = tex2D(_MainTex, screenUV).rgb;

				// Calculate world position of fragment
				float depth = tex2D(_CameraDepthLowRes, screenUV);
				bool isSky = Linear01Depth(depth) > 0.99999;

				float sceneDist;
				float3 worldPos, viewDir;
				InverseProjectDepth(depth, i.texcoord, worldPos, sceneDist, viewDir);

				// Limiting the distance of the sky ensures some rays appear against it
				if (isSky)
				{
					// Attenuate by horizon/sky factor so distant clouds aren't fully masked out
					// (since they will count as part of the sky due to not having depth)
					float skyFactor = max(viewDir.y, 0);
					skyFactor = 1-oc_pow8(1-skyFactor);
					sceneDist = lerp(_ProjectionParams.z, _OC_ScatteringMaskRadius.x * 0.25, skyFactor);
				}

				float2 bayerUV = floor(frac(screenUV * _PixelSizeDS.xy / 4) * 4);
				float n = bayer[bayerUV.x][bayerUV.y];

				// n = 0;

				// God rays ray march test
				float3 ro 	= _WorldSpaceCameraPos;
				float3 rd 	= viewDir;

				// Samples are valid within this height volume
				float ceiling = _OC_CloudAltitude - _OverCloudOriginOffset.y - _OC_CloudHeight * 0.9;// + _OC_CloudHeight * 0.5;
				float _floor = _OC_ScatteringMaskFloor - _OverCloudOriginOffset.y;

				float3 blockCoords = CloudShadowCoordinates(_WorldSpaceCameraPos, -viewDir);
				float  block = tex2D(_OC_CompositorTex, blockCoords.xy).r;
				// block = smoothstep(0.25, 0.75, block);
				block = tex2D(_OC_CompositorTex, blockCoords.xy).g;
				float blockFade = saturate(abs(_WorldSpaceCameraPos.y - ceiling) / _OC_CloudHeight);
				block *= blockFade;

				// Sample radius
				float radius = _OC_ScatteringMaskRadius.x;

				#if defined(VARIABLE_STEP_SIZE)
					// Variable step size will always use all samples for every pixel.
					// This is less noisy, but in most cases also more expensive
					radius = min(radius, sceneDist);
				#endif

				// Ray step size
				float stepSize = radius * SAMPLE_COUNT_INV;
				// Sample position
				float3 t 		= ro;
				// Distance traveled
				float rayDist 	= 0;
				// Offset starting position
				t += rd * stepSize * n;
				rayDist += stepSize * n;

				float cloudAlpha = 0;
				float projectedAlpha = tex2D(_OC_CloudShadowsTex, CloudShadowCoordinates(_WorldSpaceCameraPos).xy);

				// Fade out scattering mask when light is close to horizon
				float cloudShadowFade = saturate((-_OC_LightDir.y) * 10);

				// Result
				float2 density = 0;
				float2 u = 0;
				UNITY_LOOP
				for (; u.x < SAMPLE_COUNT;)
				{
					// // Depth termination
					UNITY_BRANCH
					if (rayDist > sceneDist)
						break;

					float cloudShadow   = 1;
					float cascadeShadow = 1;
					float fogShadow		= 1;

					if (t.y > (_CloudVolumeFloor + _CloudVolumeCeiling) * 0.5)
					{
						cloudAlpha = max(cloudAlpha, projectedAlpha);
					}

					UNITY_BRANCH
					if (t.y < _floor || t.y > _CloudVolumeCeiling)
					{
						// Do nothing
					}
					else
					{
						// Calculate sample weight
						float x = rayDist * _OC_ScatteringMaskRadius.y;
						float weight = 1-smoothstep(0.9, 1, x);//exp2(-x * 0.5);//(1-x*x*x);//*(1-exp(-x*8));

						// Projected shadow
						float3 cs = CloudShadowCoordinates(t);
						float cloudVolume = tex2Dlod(_OC_CloudShadowsTex, float4(cs.xy, 0, 0));

						// Get cloud shadow
						float heightDelta = saturate((t.y - _CloudVolumeFloor) * _OC_CloudHeightInv * 0.5);
						cloudVolume = lerp(cloudVolume, 0, heightDelta);
						cloudShadow = lerp(cloudVolume < 0.75, 1 - cloudVolume, _OC_ScatteringMaskSoftness);
						cloudShadow = lerp(1, cloudShadow, min(_OC_CloudShadowsOpacity * weight * cs.z * cloudShadowFade, 1));
						
						// Add to cloud alpha if sample is within cloud volume
						if (t.y > lerp(_CloudVolumeFloor, _OC_CloudAltitude, 0.25) && t.y < _CloudVolumeCeiling)
							cloudAlpha = min(cloudAlpha + cloudVolume, 1);
					}

					UNITY_BRANCH
					if (t.y < (_OC_FogHeight + _OverCloudOriginOffset.y))
					{
						fogShadow = FogShadow(t);
					}

					UNITY_BRANCH
					if (_CascadedShadowsEnabled == 1.0 && _CascadeShadowMapPresent == 1.0)
					{
						UNITY_BRANCH
						if (rayDist < _ShadowDistance.x)
						{
							// Sample cascaded shadow map
							float4 cascadeWeights = GetCascadeWeights_SplitSpheres(t);
							bool   inside = dot(cascadeWeights, float4(1, 1, 1, 1)) < 4;
							float4 samplePos = GetCascadeShadowCoord(float4(t, 1), cascadeWeights);
							cascadeShadow = inside ? UNITY_SAMPLE_SHADOW(_CascadeShadowMapTexture, samplePos.xyz) : 1.0f;
							// Apply shadow distance fade
							float shadowWeight = rayDist * _ShadowDistance.y;
							// cascadeShadow = lerp(cascadeShadow, 1, shadowWeight*shadowWeight);
						}
					}

					// Sum shadow
					float shadow = cascadeShadow * cloudShadow * fogShadow;

					// Used for the world
					density.x += shadow;
					// Used specifically for the volumetric clouds
					density.y += shadow * (1-cloudAlpha);

					// Step forwards
					t += rd * stepSize;
					rayDist += stepSize;

					// World mask normalization factor
					u.x++;
					// Cloud mask normalization factor
					if (cloudAlpha < 0.25)
						u.y++;
				}

				if (isSky)
					density.x = max(density.x, 0.25);

				// Normalize over sample count
				if (u.x > 0) density.x /= u.x;
				if (u.y > 0) density.y /= u.y;

				float4 result;
				result.r = density.x;
				result.g = density.y;
				result.b = 0;
				result.a = 1;
				
				return lerp(1, result, _Intensity);
			}
			ENDCG
		}
	}
}