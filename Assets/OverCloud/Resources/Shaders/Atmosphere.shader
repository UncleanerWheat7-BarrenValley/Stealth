Shader "Hidden/OverCloud/Atmosphere"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		// Pass 0, atmospheric scattering and fog
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile __ OVERCLOUD_SKY_ENABLED

			#define GROUND_PLANE
			#define ENDLESS_HORIZON

			#define OVERCLOUD_SKIP_MIE

			// #define DEBUG_LOD

			#include "UnityCG.cginc"
			#include "Atmosphere.cginc"
			#include "Skybox.cginc"
			#include "OverCloudCore.cginc"
			#include "OverCloudMain.cginc"

			struct v2f
			{
				float4 vertex 		: SV_POSITION;
				float4 texcoord		: TEXCOORD0;
			};

			sampler2D 	_BackBuffer;
			fixed4		_Color;

			// sampler2D 	_CameraDepthTexture;

			v2f vert (appdata_full v)
			{
				v2f o;

				o.vertex = v.vertex;
				#if UNITY_UV_STARTS_AT_TOP
					o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
				#else
					o.texcoord.xy = v.texcoord.xy;
				#endif
				o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
				return o;
			}

			float4 _PixelSize;
			// float4 _PixelSizeDS;

			#define SAMPLE_COUNT 3
			#define SAMPLE_COUNT_INV 0.333333333

			sampler2D _CameraDepthTexture;

			fixed4 frag (v2f i) : SV_Target
			{
				// Calculate world position of fragment
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord.zw);

				float distToCamera;
				float3 worldPos, viewDir;
				InverseProjectDepth(depth, i.texcoord.xy, worldPos, distToCamera, viewDir);

				float4 sky = OverCloudSky(_RenderCamera + viewDir * 9999999, i.texcoord.zw);

				fixed3 color = sky.rgb;

				// Sample scene/backbuffer
				fixed3 scene = tex2D(_BackBuffer, i.texcoord.zw).rgb;

				// Skybox already applies scattering etc, skip those fragments
				if (Linear01Depth(depth) < 1.0)
				{
					// Get scattering etc.
					// Shift the sample position for distant geometry according to earth's curvature.
					// This will hide the fact that the world is flat, but the atmosphere is round.
					// Scene geometry that "sticks out of the atmosphere will still be treated as if it wasn't.
					// This will make the appearance of the atmosphere more uniform across your (flat) scene.
					float3 samplePos = EarthCurvature(worldPos);
					// samplePos.y = max(samplePos.y, 0);
					// Need to take the floating origin into account
					samplePos.y = max(samplePos.y, -_OverCloudOriginOffset.y + 1);
					Atmosphere atm = OverCloudAtmosphere(samplePos);

					// Need to add this manually since we defined OVERCLOUD_SKIP_MIE above
					// The reason for doing so it we modify the sample position to account for some rendering restrictions.
					atm.scattering += OverCloudMie(viewDir, distToCamera);

					// Apply scattering mask
					float scatteringMask = OverCloudScatteringMask(i.texcoord.zw);
					EvaluateAtmosphere(atm, scatteringMask);

					// Apply
					ApplyAtmosphere(scene, atm);

					// Skybox fade
					color = lerp(color, scene, FarClipFade(distToCamera));
				}
				#if !OVERCLOUD_SKY_ENABLED
				else
				{
					color = scene;
				}
				#endif

				// Debug the scattering mask
				// color = OverCloudScatteringMask(i.texcoord.zw);

				return float4(color, 1);
			}
			ENDCG
		}

		// Pass 1, sphere-traced 2D cloud plane
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Blend One OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile __ DOWNSAMPLE_2D_CLOUDS

			#include "UnityCG.cginc"
			#include "Atmosphere.cginc"
			#include "Skybox.cginc"

			struct v2f
			{
				float4 vertex 		: SV_POSITION;
				float4 texcoord		: TEXCOORD0;
			};

			sampler2D 	_MainTex;
			fixed4		_Color;

			sampler2D	_CloudPlaneTex;
			float4		_CloudPlaneParams1;
			float3		_CloudPlaneParams2;
			float4		_CloudPlaneColor;
			float		_AboveCloudPlane;

			#define		_Scale 				_CloudPlaneParams1.x
			#define		_DetailScale 		_CloudPlaneParams1.y
			#define		_Altitude 			_CloudPlaneParams1.z
			#define		_Opacity 			_CloudPlaneParams1.w
			
			#define		_LightPenetration 	_CloudPlaneParams2.x
			#define		_LightAbsorption 	_CloudPlaneParams2.y
			#define		_Timescale 			_CloudPlaneParams2.z

			sampler2D	_CameraDepthLowRes;

			v2f vert (appdata_full v)
			{
				v2f o;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = v.vertex;
				#if UNITY_UV_STARTS_AT_TOP
					o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
				#else
					o.texcoord.xy = v.texcoord.xy;
				#endif
				o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
				return o;
			}

			sampler2D _CameraDepthTexture;

			fixed4 frag (v2f i) : SV_Target
			{
				// Calculate world position of fragment
				#if DOWNSAMPLE_2D_CLOUDS
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthLowRes, i.texcoord.zw);
				#else
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord.zw);
				#endif

				float dist;
				float3 worldPos, viewDir;
				InverseProjectDepth(depth, i.texcoord.xy, worldPos, dist, viewDir);

				float scatteringMask = OverCloudScatteringMask(i.texcoord.zw);
				float4 color = OverCloudCirrus(
					_CloudPlaneTex,
					_CloudPlaneColor,
					_Scale,
					_DetailScale,
					_Altitude,
					_Opacity,
					_Timescale,
					_LightPenetration,
					_LightAbsorption,
					viewDir,
					dist,
					scatteringMask,
					Linear01Depth(depth) == 1.0,
					_AboveCloudPlane == 1.0);

				// Pre-multiply
				color.rgb *= color.a;
				
				return color;
			}
			ENDCG
		}

		// 2: Add cloud AO to deferred buffers
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0

				#include "UnityCG.cginc"
				#include "OverCloudCore.cginc"

				struct v2f
				{
					float4 vertex 	: SV_POSITION;
					float4 texcoord : TEXCOORD0;
				};

				sampler2D 	_MainTex;
				fixed4		_Color;

				sampler2D 	_CameraDepthTexture;

				v2f vert (appdata_full v)
				{
					v2f o;
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.vertex = v.vertex;
					#if UNITY_UV_STARTS_AT_TOP
						o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
					#else
						o.texcoord.xy = v.texcoord.xy;
					#endif
					o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
					return o;
				}

				// Gamma encoding (only needed in gamma lighting mode)
				half EncodeAO(half x)
				{
					half x_g = 1.0 - max(1.055 * pow(1.0 - x, 0.416666667) - 0.055, 0.0);
					// ColorSpaceLuminance.w == 0 (gamma) or 1 (linear)
					return lerp(x_g, x, unity_ColorSpaceLuminance.w);
				}

				struct Output
				{
					half4 gbuffer0 : SV_Target0; // Albedo (rgb), ao (a)
					half4 gbuffer3 : SV_Target1; // Ambient
				};

				Output frag (v2f i)
				{
					// Sample scene/backbuffer
					float3 color = tex2D(_MainTex, i.texcoord.zw).rgb;

					// Calculate world position of fragment
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord.zw);

					float dist;
					float3 worldPos, viewDir;
					InverseProjectDepth(depth, i.texcoord.xy, worldPos, dist, viewDir);

					float ao = 0;
					// Skip skybox fragments
					if (Linear01Depth(depth) < 1.0)
					{
						ao = 1-CloudOcclusion(worldPos);
					}

					Output o;
					o.gbuffer0 = half4(0.0, 0.0, 0.0, ao);
					o.gbuffer3 = half4((half3)EncodeAO(ao), 0.0);
					return o;
				}
			ENDCG
		}

		// 3: Modify albedo and roughness based on wetness
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Blend DstColor Zero, One One

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#pragma multi_compile __ RAIN_MASK_ENABLED

				#include "UnityCG.cginc"
				#include "OverCloudCore.cginc"

				struct v2f
				{
					float4 vertex 	: SV_POSITION;
					float4 texcoord : TEXCOORD0;
				};

				sampler2D 	_MainTex;
				fixed4		_Color;

				sampler2D 	_CameraDepthTexture;
				sampler2D	_GBuffer2; // GBuffer normals

				v2f vert (appdata_full v)
				{
					v2f o;
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.vertex = v.vertex;
					#if UNITY_UV_STARTS_AT_TOP
						o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
					#else
						o.texcoord.xy = v.texcoord.xy;
					#endif
					o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
					return o;
				}

				struct Output
				{
					half4 gbuffer0 : SV_Target0; // Albedo (rgb), ao (a)
					half4 gbuffer1 : SV_Target1; // Specular (rgb), roughness (a)
					half4 gbuffer3 : SV_Target2; // Ambient
				};

				Output frag (v2f i)
				{
					// Sample scene/backbuffer
					float3 color = tex2D(_MainTex, i.texcoord.zw).rgb;

					// Calculate world position of fragment
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord.zw);

					float dist;
					float3 worldPos, viewDir;
					InverseProjectDepth(depth, i.texcoord.xy, worldPos, dist, viewDir);

					float wetness = 0;
					// Skip skybox fragments
					if (Linear01Depth(depth) < 1.0)
					{
						// Sample scene normals
						float3 worldNormal = tex2D(_GBuffer2, i.texcoord.zw).rgb*2-1;

						wetness = RainSurface(worldPos, worldNormal);
					}

					float3 darken = 1 - wetness * _OC_GlobalWetnessParams.y;
					float  roughnessMod = wetness * _OC_GlobalWetnessParams.z;

					Output o;
					// o.gbuffer0 = half4(darken, 1.0);
					// o.gbuffer1 = half4(1.0, 1.0, 1.0, roughnessMod);
					o.gbuffer0 = half4(darken, 1.0);
					o.gbuffer1 = half4(1.0, 1.0, 1.0, roughnessMod);
					o.gbuffer3 = half4(darken, 1.0);
					return o;
				}
			ENDCG
		}

		// 4: Add rain ripples and pouring to world normals
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#pragma multi_compile __ RAIN_MASK_ENABLED

				#include "UnityCG.cginc"
				#include "OverCloudCore.cginc"

				struct v2f
				{
					float4 vertex 	: SV_POSITION;
					float4 texcoord : TEXCOORD0;
				};

				sampler2D 	_MainTex;
				fixed4		_Color;

				sampler2D 	_CameraDepthTexture;
				sampler2D	_GBuffer2Copy; // GBuffer normals
				sampler2D	_OC_RainFlowTex;

				v2f vert (appdata_full v)
				{
					v2f o;
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.vertex = v.vertex;
					#if UNITY_UV_STARTS_AT_TOP
						o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
					#else
						o.texcoord.xy = v.texcoord.xy;
					#endif
					o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
					return o;
				}

				fixed4 frag (v2f i) : SV_Target
				{
					float rippleIntensity 	= _OC_GlobalRainParams.x;
					float flowIntensity 	= _OC_GlobalRainParams.z;
					float flowScale			= _OC_GlobalRainParams.w;
					float flowTimescale		= _OC_GlobalRainParams2.y;

					// Calculate world position of fragment
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord.zw);
					float dist;
					float3 worldPos, viewDir;
					InverseProjectDepth(depth, i.texcoord.xy, worldPos, dist, viewDir);

					float  wetness = 0;
					float3 normal 	= float3(0, 1, 0);
					float  alpha	= 0;
					// Skip skybox fragments
					if (Linear01Depth(depth) < 1.0)
					{
						// Sample and unpack scene normals
						float3 worldNormal = tex2D(_GBuffer2Copy, i.texcoord.zw).rgb*2-1;
						float3 originalNormal = worldNormal;

						// Sample OverCloud wetness
						wetness = RainSurface(worldPos, worldNormal);

						// Get rain ripple tangent (already unpacked)
						float3 rippleTangent = RainRipplesTangent(worldPos, worldNormal, wetness) * float3(-1, 1, 1);
						// Construct TBN
						fixed3 worldTangent  = float3(1, 0, 0);
						fixed3 worldBinormal = cross(worldNormal, worldTangent);
						float3 tSpace0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
						float3 tSpace1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
						float3 tSpace2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);
						// Transform to world space
						fixed3 rippleWorld;
						rippleWorld.x = dot(tSpace0, rippleTangent);
						rippleWorld.y = dot(tSpace1, rippleTangent);
						rippleWorld.z = dot(tSpace2, rippleTangent);
						worldNormal = lerp(worldNormal, rippleWorld, rippleIntensity);

						// Flow tangent (X-direction)
						float3 flowTangent = UnpackNormal(tex2D(_OC_RainFlowTex, (worldPos.zy + float2(0, flowTimescale) * _Time.y) * flowScale));
						// Construct TBN
						worldTangent  = float3(0, 0, 1);
						worldBinormal = cross(worldNormal, worldTangent);
						tSpace0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
						tSpace1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
						tSpace2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);
						// Transform to world space
						fixed3 flowWorld;
						flowWorld.x = dot(tSpace0, flowTangent);
						flowWorld.y = dot(tSpace1, flowTangent);
						flowWorld.z = dot(tSpace2, flowTangent);
						float mask = abs(originalNormal.x)*abs(originalNormal.x)*(1-abs(originalNormal.y))*(1-abs(originalNormal.y));
						worldNormal = lerp(worldNormal, flowWorld, mask * flowIntensity);

						// Flow tangent (Z-direction)
						flowTangent = UnpackNormal(tex2D(_OC_RainFlowTex, (worldPos.xy + float2(0, flowTimescale) * _Time.y) * flowScale));
						// Construct TBN
						worldTangent  = float3(1, 0, 0);
						worldBinormal = cross(worldNormal, worldTangent);
						tSpace0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
						tSpace1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
						tSpace2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);
						// Transform to world space
						flowWorld.x = dot(tSpace0, flowTangent);
						flowWorld.y = dot(tSpace1, flowTangent);
						flowWorld.z = dot(tSpace2, flowTangent);
						mask = abs(originalNormal.z)*abs(originalNormal.z)*(1-abs(originalNormal.y))*(1-abs(originalNormal.y));
						worldNormal = lerp(worldNormal, flowWorld, mask * flowIntensity);

						// Re-pack
						normal = worldNormal*0.5+0.5;
						alpha = wetness;
					}

					return float4(normal, alpha);
				}
			ENDCG
		}

		// 5: Add cloud shadows to screen-space shadow mask
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Blend DstColor Zero

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0

				#include "UnityCG.cginc"
				#include "OverCloudCore.cginc"

				struct v2f
				{
					float4 vertex 	: SV_POSITION;
					float4 texcoord : TEXCOORD0;
				};

				sampler2D 	_MainTex;
				fixed4		_Color;

				sampler2D 	_CameraDepthTexture;

				v2f vert (appdata_full v)
				{
					v2f o;
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.vertex = v.vertex;
					#if UNITY_UV_STARTS_AT_TOP
						o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
					#else
						o.texcoord.xy = v.texcoord.xy;
					#endif
					o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
					return o;
				}

				fixed4 frag (v2f i) : SV_Target
				{
					// Sample scene/backbuffer
					float3 color = tex2D(_MainTex, i.texcoord.zw).rgb;

					// Calculate world position of fragment
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord.zw);

					float dist;
					float3 worldPos, viewDir;
					InverseProjectDepth(depth, i.texcoord.xy, worldPos, dist, viewDir);

					float cloudShadows = 1;
					// Skip skybox fragments
					if (Linear01Depth(depth) < 1.0)
					{
						cloudShadows = CloudShadows(worldPos);
					}

					return float4(cloudShadows, cloudShadows, cloudShadows, 1);
				}
			ENDCG
		}

		// Pass 6: Volumetric cloud LOD plane
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Blend One OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile __ DOWNSAMPLE_2D_CLOUDS

			#include "UnityCG.cginc"
			#include "Atmosphere.cginc"
			#include "Skybox.cginc"
			#include "OverCloudCore.cginc"
			#include "OverCloudMain.cginc"

			struct v2f
			{
				float4 vertex 		: SV_POSITION;
				float4 texcoord		: TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D 	_MainTex;
			fixed4		_Color;

			sampler2D	_CameraDepthLowRes;

			v2f vert (appdata_full v)
			{
				v2f o;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = v.vertex;
				#if UNITY_UV_STARTS_AT_TOP
					o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
				#else
					o.texcoord.xy = v.texcoord.xy;
				#endif
				o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
				return o;
			}

			float4 _PixelSize;
			// float4 _PixelSizeDS;

			float _RenderingVolumetricClouds;

			#define SAMPLE_COUNT 3
			#define SAMPLE_COUNT_INV 0.333333333

			sampler2D _CameraDepthTexture;

			fixed4 frag (v2f i) : SV_Target
			{
				// Fix precision issues with cloud sphere
				float3 camera = _WorldCamera;
				if (abs(camera.y - _OC_CloudAltitude) < 1)
					camera.y += 2;

				// Calculate world position of fragment
				#if DOWNSAMPLE_2D_CLOUDS
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthLowRes, i.texcoord.zw);
				#else
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.texcoord.zw);
				#endif

				float dist;
				float3 worldPos, viewDir;
				InverseProjectDepth(depth, i.texcoord.xy, worldPos, dist, viewDir);

				fixed4 color = 0;

				float r0, r1;
				// Earth intersection
				UNITY_BRANCH
				if (!RayIntersect(camera, viewDir, EARTH_CENTER, EARTH_RADIUS, r0, r1))
					r0 = 999999999;

				float t0, t1;
				// Cloud plane intersection
				// _OC_CloudAltitude is offset by the origin offset, so we need to move it back
				UNITY_BRANCH
				if (RayIntersect(camera, viewDir, EARTH_CENTER, EARTH_RADIUS + (_OC_CloudAltitude + _OverCloudOriginOffset.y), t0, t1))
				{
					UNITY_BRANCH
					if (t0 < r0 && (t0 < dist || dist >= _ProjectionParams.z * 0.9))
					{
						// Position of the cloud plane in true world space
						float3 cloudPos = camera + viewDir * t0;
						float3 sphereNormal = normalize(cloudPos - EARTH_CENTER);

						float cloudAlpha = CloudDensity(ws2rs(cloudPos));

						float3 ro = ws2rs(cloudPos);
						float3 rd = -_OC_LightDir;
						float _RayLength = 2;
						float s = _OC_CloudHeight * _RayLength * SAMPLE_COUNT_INV;

						float lightDensity = 0;
						[unroll(SAMPLE_COUNT)]
						for (int u = 0; u < SAMPLE_COUNT; ++u)
						{
							s *= 1.25;
							float3 t = ro + rd * s * (u+0);
							float d = CloudDensity(t);

							lightDensity += d * s;
						}

						// Density multipliers
						float _PrecipitationMul = 1;
						lightDensity *= lerp(1, _PrecipitationMul, _OC_Precipitation) * _OC_CloudDensity.y;

						// Calculate normal
						float3 cloudPos0 = cloudPos;
						float offset = _OC_CloudHeight;
						float3 cloudPos1 = cloudPos0 + float3(offset, 0, 0);
						float3 cloudPos2 = cloudPos0 + float3(0, 0, offset);
						cloudPos0.y = cloudAlpha * offset;
						cloudPos1.y = CloudDensity(ws2rs(cloudPos1)) * offset;
						cloudPos2.y = CloudDensity(ws2rs(cloudPos2)) * offset;
						float3 normal = cross( normalize(cloudPos1 - cloudPos0), normalize(cloudPos2 - cloudPos0) );

						cloudAlpha = smoothstep(0, 0.5, cloudAlpha);

						float earthShadow = dot(sphereNormal, -_OC_LightDir)*0.5+0.5;
						earthShadow = smoothstep(0.5, 0.55, earthShadow);
						float e_direct = max(dot(normal, _OC_LightDir), 0) * earthShadow;
						float3 ambientColor = unity_AmbientSky;
						ambientColor = lerp(ambientColor, dot(ambientColor, float3(0.3, 0.59, 0.11)), 0.5);
						float3 cloudColor = (ambientColor + e_direct * _OC_LightColor * 0.5);

						cloudColor = 0;
						cloudColor += CloudAmbient(1, 0);
						cloudColor += CloudLighting
						(
							1,
							cloudAlpha * s,
							lightDensity * 0.1,
							viewDir,
							_OC_LightDir,
							_OC_LightColor * earthShadow
						);

						cloudColor *= _OC_CloudAlbedo.rgb;
						cloudAlpha *= _OC_CloudAlbedo.a;

						Atmosphere atm = OverCloudAtmosphere(ws2rs(cloudPos));
						float scatteringMask = OverCloudScatteringMask(i.texcoord.xy);
						EvaluateAtmosphere(atm, scatteringMask);
						ApplyAtmosphere(cloudColor, atm);

						// Near fade, unless we're not rendering volumetric clouds,
						// in which case the cloud plane acts as a replacement even up-close
						if (_RenderingVolumetricClouds == 1.0)
						{
							float adjustedFarClip = _ProjectionParams.z * 0.2; // Matches _OC_FarClipInv in OverCloud.cs
							float radiusFade = min(length(camera - cloudPos) / min(_OC_CloudPlaneRadius, adjustedFarClip), 1);
							cloudAlpha *= oc_pow4(radiusFade);
						}

						#ifdef DEBUG_LOD
							cloudColor.rg = 0;
						#endif

						// Depth fade. This greatly reduces shimmering when the cloud plane intersects the scene.
						if (Linear01Depth(depth) < 1.0)
							cloudAlpha *= min(max(dist - t0, 0) * 0.002, 1);

						// Hack to fix some downscaling issues when the camera is close to the cloud plane shell
						if (abs(dot(sphereNormal, viewDir)) < 0.02)
							cloudAlpha = 0;

						color = float4(cloudColor, cloudAlpha);
					}
				}

				// Pre-multiply
				color.rgb *= color.a;

				return color;
			}
			ENDCG
		}

		// 7: Stereo-enabled blit
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0

				#include "UnityCG.cginc"

				struct v2f
				{
					float4 vertex 	: SV_POSITION;
					float4 texcoord : TEXCOORD0;
				};

				sampler2D 	_BlitTex;
				fixed4		_Color;

				v2f vert (appdata_full v)
				{
					v2f o;
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.vertex = v.vertex;
					#if UNITY_UV_STARTS_AT_TOP
						o.texcoord.xy = v.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
					#else
						o.texcoord.xy = v.texcoord.xy;
					#endif
					o.texcoord.zw = UnityStereoTransformScreenSpaceTex(o.texcoord.xy);
					return o;
				}

				fixed4 frag (v2f i) : SV_Target
				{
					return tex2D(_BlitTex, i.texcoord.zw);
				}
			ENDCG
		}
	}
}