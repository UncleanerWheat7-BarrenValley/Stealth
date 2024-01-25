///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

Shader "OverCloud/OverCloudMain"
{
	Properties
	{
		_PLightMul		 		("Point Light Absorption", 	Range(0, 1)) 		= 1
		_RayLength 				("Directional Step Size", 	Range(0, 2)) 		= 1
		_PointRayLength 		("Point Step Size", 		Range(0, 2)) 		= 1
		_PrecipitationMul 		("Precipitation Density Mul", Range(1, 8)) 		= 2
		_InvFade 				("Soft Particles", 			Range(0, 1)) 		= 0.5
	}

	Category
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Lighting Off
		ZWrite Off
		 
		SubShader
		{
			Pass
			{
				Blend One OneMinusSrcAlpha

				CGPROGRAM
					// Mie scattering needs to always be sampled in the fragment shader,
					// as it does not interpolate well when the phase is high
					#define OVERCLOUD_SKIP_MIE

					#include "OverCloudMain.cginc"

					#pragma target 3.0
					#pragma vertex vert
					#pragma fragment frag
					#pragma multi_compile_instancing
					#pragma multi_compile_fog
					#pragma multi_compile __ SAMPLE_COUNT_LOW SAMPLE_COUNT_NORMAL
					#pragma multi_compile __ HQ_LIGHT_SAMPLING
					#pragma multi_compile __ LOD_CLOUDS
					#pragma multi_compile __ OVERCLOUD_POINTLIGHT_ENABLED // You can comment this out if you never plan on using cloud point lights and want to save a keyword

					// #define DEBUG_WIRE			// Enable this to debug particle edges
					// #define DEBUG_OVERDRAW		// Enable this to debug particle screen coverage
					// #define DEBUG_LOD			// Enable this to debug LOD distance
					// #define PER_PIXEL_ATMOSPHERE	// Enable this to calculate fog/scattering per-pixel (as opposed to per-vertex)
					#define OVERCLOUD_ATMOSPHERE	// Disable this if you want to use the default Unity fog

					#define CULL_LOW  0.05
					#define CULL_HIGH 0.1

					#if SAMPLE_COUNT_LOW
						#define SAMPLE_COUNT 3
						#define SAMPLE_COUNT_INV 0.333333
					#elif SAMPLE_COUNT_NORMAL
						#define SAMPLE_COUNT 5
						#define SAMPLE_COUNT_INV 0.2
					#else // SAMPLE_COUNT_HIGH
						#define SAMPLE_COUNT 8
						#define SAMPLE_COUNT_INV 0.125
					#endif

					float2 _OC_ScatteringMaskRadius;

					struct v2f
					{
						float4 pos 				: SV_POSITION;
						fixed4 color 			: COLOR;
						float4 texcoord 		: TEXCOORD0;
						float3 worldPos			: TEXCOORD1;
						float3 worldPos2		: TEXCOORD2;
						float4 screenPos		: TEXCOORD3;
						float3 viewDir 			: TEXCOORD4;
						float4 params 			: TEXCOORD5;
						float4 incomingLight	: TEXCOORD6;
						#if defined(OVERCLOUD_ATMOSPHERE)
							#if !defined(PER_PIXEL_ATMOSPHERE)
								OVERCLOUD_COORDS(7)
							#endif
						#else
							UNITY_FOG_COORDS(7)
						#endif
						UNITY_VERTEX_INPUT_INSTANCE_ID
						UNITY_VERTEX_OUTPUT_STEREO
					};

					v2f vert (appdata_full v)
					{
						UNITY_SETUP_INSTANCE_ID(v);

						v2f o;
						UNITY_INITIALIZE_OUTPUT(v2f,o);
						UNITY_TRANSFER_INSTANCE_ID(v,o);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

						float3 worldPos = mul(unity_ObjectToWorld, v.vertex);

						// World position before expanding particle
						float3 worldCenter = worldPos;

						float cellSpan 		= _RandomRange.x;
						float cellSpanInv 	= 1 / _RandomRange.x;

						float windTime 		= (_OC_GlobalWindTime) * cellSpanInv;
						// windTime += _Time.x * 16;
						float floorWindTime = floor(windTime);
						float fracWindTime 	=  frac(windTime);

						// Move vertices (snaps back because of frac())
						worldPos.x -= fracWindTime * cellSpan;

						// When vertices snap back, move the world position used for noise sampling by one cell span.
						// This will give the appearance of a moving cloud bed
						float3 worldCenter_offset = worldCenter;
						worldCenter_offset.x += floorWindTime * cellSpan;

						// Noise samples used to drive some values
						float2 n = 0;
						n.x = noise2D((worldCenter_offset.xz + _OverCloudOriginOffset.xz) * 1);
						n.y = noise2D((worldCenter_offset.xz + _OverCloudOriginOffset.xz) * 3);

						float2 cscoords = max(worldPos.xz - _OC_CloudWorldExtentsMinMax.xy, 0) * _OC_CloudWorldExtents.zw;
						float4 compositor = tex2Dlod(_OC_CompositorTex, float4(cscoords.xy, 0, 0));
						float cloudDensity = compositor.r;
						cloudDensity = CloudDensityV(worldPos);
						// cloudDensity = smoothstep(_OC_CloudSharpness * 0.499, 1 - _OC_CloudSharpness * 0.499, cloudDensity);

						
						// Offset particles randomly to break the grid-like structure
						float3 offsetVec = float3(n.x, 0, n.y);
						#if LOD_CLOUDS
							worldPos.xyz += offsetVec * cellSpan * 0.5;
						#else
							worldPos.xyz += offsetVec * cellSpan * 0.25;
						#endif

						// TODO: Obsolete stuff left since 2D texture sampling
						// Calculate some stuff
						float2 dim			= float2(1, 8);
						float2 uvSpan		= 1 / dim;
						o.texcoord.xy 		= v.texcoord.xy * uvSpan;
						float2 	tex0 		= o.texcoord.xy;
						float2 	uvCenter 	= uvSpan * 0.5;

						float yOffset 		= floor((n.x*0.5+0.5) * dim.y) / dim.y;
						o.texcoord.y 		+= yOffset;
						uvCenter.y 			+= yOffset;

						float 	radius 		= Radius(cloudDensity);
						float 	angle 		= n.y * 180 + windTime * _PerturbSpeed * 360;
						float	heightMin	= _OC_CloudAltitude - _OC_CloudHeight;
						float	heightMax	= _OC_CloudAltitude + _OC_CloudHeight;
						float	height		= heightMax - heightMin;

						// Particle culling
						// radius *= (cloudDensity > 0.02);
						radius *= smoothstep(CULL_LOW, CULL_HIGH, cloudDensity);

						// Camera fade
						float camDist 	= length(worldPos - _WorldSpaceCameraPos);
						float camDist2 	= max(camDist-radius, 0);
						float camFade 	= saturate(camDist / (radius * 2) - 0);
						float camFade2 	= saturate(camDist2 / radius);
						camFade = lerp(camFade, camFade2, 0.5);

						o.params = 1;//float4(0, 0, camFade, 1);

						// Rotate texture coordinate to create some variance
						// float a = angle * DEGTORAD;
						// float sinX = sin(a);
						// float cosX = cos(a);
						// float sinY = sin(a);
						// float2x2 R = float2x2(cosX, -sinX, sinY, cosX);
						// o.texcoord.xy -= uvCenter;
						// o.texcoord.xy *= dim;
						// o.texcoord.xy = mul( o.texcoord.xy, R );
						// o.texcoord.xy /= dim;
						// o.texcoord.xy += uvCenter;
						o.texcoord.zw = (o.texcoord.xy - uvCenter) * 2 * dim;

						// Camera-facing billboard (without roll)
						float3 forward = worldPos - _WorldSpaceCameraPos;
						#if LOD_CLOUDS
							// Flatten LOD clouds faster to improve the appearance at high altitudes
							forward.y *= 3;
						#endif
						forward = normalize(forward);
						

						// Fade to camera forward vector when getting close to the particle center
						// This prevents the particle from making jarring movements when flying through it
						float3 camForward 	= -UNITY_MATRIX_V[2].xyz;
						forward = lerp(forward, camForward, saturate(1 - camDist * _OC_CloudHeightInv * 0.5));

						// Calculate particle flatness
						float flatness = lerp(_FlatnessMin, _FlatnessMax, cloudDensity);
						flatness = lerp(_FlatnessMax, flatness, 1-oc_pow2(1-saturate(camDist / _FlatnessNearfade)));

						// Flatten LODs
						flatness *= lerp(1 * _ParticleScale.y, 1, abs(forward.y));
						flatness  = lerp(1 * _ParticleScale.y, 1, abs(forward.y));

						// Offset vectors
						float3 right = normalize(cross(forward, float3(0, 1, 0)));
						// float3 right = UNITY_MATRIX_V[0].xyz;
						float3 up = normalize(cross(right, forward));
						// right = cross(forward, up);

						// Vertex offset
						float2 offset = (v.texcoord.xy - float2(0.5, 0.5)) * 2;
						// worldPos.xyz +=  up   * offset.y * radius * flatness; // Apply flatness

						// Particle radius + LOD multiplier
						radius = _OC_CloudHeight * _ParticleScale.x;// * 1.25;

						// Particle vertex culling
						radius *= cloudDensity > CULL_LOW;

						// Far fade
						float dist 		= length(worldCenter - _WorldSpaceCameraPos);
						float startFade = _Radius * 0.5;
						float endFade 	= _Radius;
						float fade 		= saturate(1 - max(dist-startFade, 0) / (endFade - startFade));

						// Near fade
						startFade 		= _NearRadius * 0.5;
						endFade 		= _NearRadius;
						float nearFade 	= saturate(max(dist-startFade, 0) / (endFade - startFade));
						fade 			*= nearFade;

						// Fade cull
						radius *= fade > M_EPSILON;

						// FarZ cull
						radius *= dist < _ProjectionParams.z;

						// Attempt to counter-act particle grid limitations by pushing (dense) particles towards the camera a bit when viewed at steep angles
						worldPos.xyz += forward * radius * -abs(forward.y) * 0.5 * cloudDensity;

						// Try and optimize particle size a bit
						// float viewShiftMax = 1.25;
						// float viewShift    = min(1 / (1-abs(forward.y)), viewShiftMax);
						worldPos.xyz +=  up   * offset.y * radius * flatness;
						worldPos.xyz += right * offset.x * radius;					

						// Density-based height offset
						float3 heightOffset = float3(0, 1, 0) * (radius * _ParticleScale.y - _OC_CloudHeight) * _ParticleScale.y;
						// worldPos.xyz += heightOffset * 0.5;// _OC_CloudAltitudeOffset * 1;

						#if LOD_CLOUDS
							// When the camera faces down, push lod clouds towards the center of the cloud shape,
							// where they will sample the most density
							worldPos.y = lerp(worldPos.y, _OC_CloudAltitude + (_OC_ShapeParams.x-0.5) * _OC_CloudHeight, abs(forward.y)*0.85);
						#endif

						// Calculate view direction
						o.viewDir = normalize(worldPos - _WorldSpaceCameraPos);

						// Transfer world position
						o.worldPos = worldPos;

						// Transfer camera distance
						o.params.x = length(worldPos - _WorldSpaceCameraPos);

						// Transfer color
						o.color = v.color;

						// Sun attenuation
						o.incomingLight.w = OverCloudSunAtten(worldPos);

						// Earth shadow
						float3 sphereNormal = normalize(rs2ws(worldPos) - EARTH_CENTER);
						float earthShadow = dot(sphereNormal, -_OC_LightDir)*0.5+0.5;
						earthShadow = smoothstep(0.5, 0.51, earthShadow);

						// float3 shellNormal = normalize(worldPos - (_WorldSpaceCameraPos * float3(1, 0, 1) - float3(0, EARTH_RADIUS, 0)));
						// o.incomingLight.y = min(max(dot(shellNormal, -_OC_LightDir), 0) * 24, 1);
						// o.incomingLight.z = min(max(dot(shellNormal, -_OC_LightDir), 0) * 4, 1);
						o.incomingLight.yz = earthShadow;

						// Scattering mask inMask
						float base = _OC_CloudAltitude - _OC_CloudHeight * 2;
						float d = max(_WorldSpaceCameraPos.y - base, 0);
						float inMask = 1 - min(d / (_OC_CloudHeight * 1), 1);

						o.color.r 	= radius;
						o.color.g 	= inMask;
						float weight = min(dist * _OC_ScatteringMaskRadius.y, 1);
						weight = (1-weight*weight*weight);
						o.color.b	= lerp(1, CloudShadowsV(worldPos), weight);
						o.color.a 	= fade;

						// Far z fade
						dist = length((worldCenter - _WorldSpaceCameraPos));
						float distFade = saturate(dist / _ProjectionParams.z);
						distFade *= distFade * distFade * distFade;
						o.color.a *= FarClipFade(dist);

						// Particle alpha culling
						o.color.a *= smoothstep(CULL_LOW, CULL_HIGH, cloudDensity);

						// Earth curvature
						// Height offset by earth's curvature
						// float3 earthCenter = _WorldSpaceCameraPos * float3(1, 0, 1) - float3(0, 1, 0) * EARTH_RADIUS;
						// float3 A = _WorldSpaceCameraPos * float3(1, 0, 1) +  float3(0, worldPos.y, 0);
						// float3 B = A - earthCenter;
						// float3 C = worldPos - earthCenter;
						// float h = (length(C) - length(B)) * _OC_PlanetScale;

						float3 worldPos0 = worldPos;

						// Above calculation suffers from precision issues. This is an approximation of the same offset
						float h = oc_pow2(length((worldPos - _WorldSpaceCameraPos).xz)) / _OC_PlanetScale * 8e-8;
						worldPos.y -= h;
						
						// Back to object space
						v.vertex = mul(unity_WorldToObject, float4(worldPos, 1));

						// Finally transform vertex to clip space
						o.pos = UnityObjectToClipPos(v.vertex);

						// Calculate screen position
						o.screenPos = ComputeScreenPos(o.pos);
						COMPUTE_EYEDEPTH(o.screenPos.z);

						o.texcoord.xy = v.texcoord.xy;

						o.worldPos2 = worldPos;

						#if defined(OVERCLOUD_ATMOSPHERE)
							#if !defined(PER_PIXEL_ATMOSPHERE)
								OVERCLOUD_TRANSFER(worldPos0, o)
							#endif
						#else
							UNITY_TRANSFER_FOG(o, o.pos);
						#endif

						return o;
					}

					float remap (float value, float original_min, float original_max, float new_min, float new_max)
					{
						return new_min + (((value - original_min) / (original_max - original_min)) * (new_max - new_min));
					}

					float _PLightMul;

					sampler2D _CameraDepthLowRes;

					float3 CloudPointLight (float3 albedo, float baseDensity, float3 worldPos, float3 viewDir)
					{
						float3 lightColor = _OC_PointLightColor.rgb;

						float3 lightDir = worldPos - _OC_PointLightPosRadius.xyz;
						float distToLight = length(lightDir);
						lightDir /= distToLight;

						float atten = oc_pow2(1 - min(distToLight / _OC_PointLightPosRadius.w, 1));
						if (atten < 0.001)
							return 0;

						// x: Direct
						// y: Indirect
						float lightDensity = 0;

						float s = _OC_CloudHeight * _PointRayLength * SAMPLE_COUNT_INV;

						bool limit = s * SAMPLE_COUNT > distToLight;
						if (limit)
							s = distToLight * SAMPLE_COUNT_INV;

						for (int i = 0; i < SAMPLE_COUNT; i++)
						{
							if (!limit)
								s *= 1.25;
							float3 t = worldPos - lightDir * s * (i+0);
							float3 masks;
							float d = CloudDensity3DBase(t, masks);// * s * 0.01;

							#if !LOD_CLOUDS
								CloudErosion1(t, d, masks.x);
								#if HQ_LIGHT_SAMPLING
									CloudErosion2(t, d, masks.x);
								#endif
							#endif

							lightDensity += d * s;
						}

						// Apply density multipliers
						lightDensity *= lerp(1, _PrecipitationMul, _OC_Precipitation) * _PLightMul;

						// Direct / indirect lighting
						float3 e = CloudLighting
						(
							albedo,
							baseDensity * s,
							lightDensity,
							viewDir,
							lightDir,
							_OC_PointLightColor.rgb * atten
						);

						return e;
					}

					static const float3 dir6[6] = { 
						float3( 1, 0, 0),
						float3(-1, 0, 0),
						float3( 0, 1, 0),
						float3( 0,-1, 0),
						float3( 0, 0, 1),
						float3( 0, 0,-1)
					};

					sampler2D _CameraDepthTexture;

					FragmentOutput frag (v2f i)
					{
						UNITY_SETUP_INSTANCE_ID(i);

						float3 worldPos = i.worldPos;

						#ifdef DEBUG_OVERDRAW
							FragmentOutput t;
							t.color = float4(0.05, 0, 0, 0);
							return t;
						#endif

						#ifdef DEBUG_WIRE
							if (abs(i.texcoord.x-0.5) > 0.49 || abs(i.texcoord.y-0.5) > 0.49)
							{
								FragmentOutput t;
								t.color = float4(0.5, 0, 0, 0.95);
								return t;
							}
						#endif

						float distFromCenter = length(i.texcoord.zw);

						// Circular polygon
						if (distFromCenter > 1)
							discard;

						// Height cull
						if (worldPos.y > _CloudVolumeCeiling || worldPos.y < _CloudVolumeFloor)
							discard;

						float2 screenUV = i.screenPos.xy / i.screenPos.w;

						// Sample scene depth
						// float sceneZ = LinearEyeDepth(tex2D(_CameraDepthTexture, screenUV));
						// Need to use the downsampled depth buffer here for the nearest-depth upsampling to work later
						float sceneZ = LinearEyeDepth(tex2D(_CameraDepthLowRes, screenUV));
						float partZ = i.screenPos.z;

						// Depth cull
						UNITY_BRANCH
						if (partZ > sceneZ)
							discard;

						float3 viewDir = worldPos - _WorldSpaceCameraPos;
						float distToCamera = length(viewDir);
						viewDir /= distToCamera;

						// Planet sphere cull
						float t0, t1; 
						UNITY_BRANCH
						if (RayIntersect(_WorldCamera, normalize(i.worldPos2 - _WorldSpaceCameraPos), EARTH_CENTER, EARTH_RADIUS, t0, t1))
						{
							if (t0 < distToCamera)
								discard;
						}

						// Push particle center towards camera slightly
						worldPos -= viewDir * (1-distFromCenter) * i.color.r * 0.25;

						float3 wp_start = worldPos - i.viewDir * i.color.r;
						float3 wp_end   = worldPos + i.viewDir * i.color.r;

						// Base density sample
						float3 masks;
						float density = CloudDensity3DBase(worldPos, masks);
						float erosionMask = masks.x;
						float heightGrad  = masks.y;

						UNITY_BRANCH
						if (density < 0.002)
							discard;

						float baseDensity = density;
						
						// Erode cloud edges
						CloudErosion1(worldPos, density, erosionMask);
						baseDensity = density;
						#if !LOD_CLOUDS
							CloudErosion2(worldPos, density, erosionMask);
						#endif

						// Set actual albedo color
						float3 albedo = float3(lerp(_OC_CloudAlbedo.rgb, _OC_CloudPrecipitationAlbedo.rgb, _OC_Precipitation * (1-heightGrad)));

						// Volume ray marching
						float lightDensity = 0;
						float lightDensity2 = 0;
						float3 ro = worldPos;

						#if LOD_CLOUDS
							// Because LOD clouds are made flatter in the vertex shader,
							// We need to adjust staring position of the light ray marching,
							// or else we will end up with really dark LOD clouds.
							ro += viewDir.y * -_OC_CloudHeight * 0.25;
						#endif

						float3 rd = -_OC_LightDir;

						float s = _OC_CloudHeight * _RayLength * SAMPLE_COUNT_INV;

						for (int u = 0; u < SAMPLE_COUNT; ++u)
						{
							s *= 1.25;
							float3 t = ro + rd * s * (u+0);
							float d = CloudDensity3DBase(t, masks);// * s * 0.01;
							lightDensity2 += d * s;

							// #if !LOD_CLOUDS
								CloudErosion1(t, d, masks.x);

								#if !LOD_CLOUDS && HQ_LIGHT_SAMPLING
									CloudErosion2(t, d, masks.x);
								#endif
							// #endif

							lightDensity += d * s;
						}

						// Density multipliers
						// lightDensity *= _OC_CloudDensity.y * 4 * lerp(1, _PrecipitationMul, _OC_Precipitation);
						lightDensity *= lerp(1, _PrecipitationMul, _OC_Precipitation) * _OC_CloudDensity.y;
						lightDensity2 *= lerp(1, _PrecipitationMul, _OC_Precipitation) * _OC_CloudDensity.y;

						fixed4 color = 0;
						
						// Direct / indirect lighting
						color.rgb += CloudLighting
						(
							albedo,
							baseDensity * s,
							lightDensity,
							viewDir,
							_OC_LightDir,
							_OC_LightColor * i.incomingLight.y
						);

						// Ambient lighting
						color.rgb += CloudAmbient(albedo, baseDensity);

						float alpha = _OC_CloudAlbedo.a * i.color.a;

						// Transfer alpha
						color.a = saturate(density);
						color.a = smoothstep(_OC_AlphaEdgeParams.x, _OC_AlphaEdgeParams.y, color.a);

						// Transfer fades / etc
						color.a *= alpha;

						color.a *= _OC_CloudDensity.x;

						// Camera near fade
						float camDist = length(worldPos - _WorldSpaceCameraPos);
						color.a *= min(camDist * _OC_CloudHeightInv * 0.5, 1);

						color.a *= 1-distFromCenter;

						// Depth fade (soft particles)
						float fade = saturate ((1-_InvFade) * 0.01 * (sceneZ-partZ));
						color.a *= fade;

						#if OVERCLOUD_POINTLIGHT_ENABLED
							// Cloud point light
							color.rgb += CloudPointLight(albedo, baseDensity, worldPos, viewDir);
						#endif

						#if defined(OVERCLOUD_ATMOSPHERE)
							float scatteringMask = tex2D(_OC_ScatteringMask, screenUV).g;
							// Fade the scattering mask out when close to or above the cloud volume floor
							// This fixes an issue where distant clouds sometimes have their
							// scattering masked out because the scattering mask is catching
							// the cloud which the camera is inside of
							float maskFade = 1-min(max(_CloudVolumeFloor - _WorldSpaceCameraPos.y, 0) * _OC_CloudHeightInv * 4, 1);
							scatteringMask = lerp(scatteringMask, 1, maskFade);

							// Apply atmosphere. Because we are doing some special modification to the scattering mask,
							// we need to call the functions manually instead of using OVERCLOUD_FRAGMENT
							#if defined(PER_PIXEL_ATMOSPHERE)
								Atmosphere atm = OverCloudAtmosphere(worldPos);
								EvaluateAtmosphere(atm, scatteringMask);
								#if defined(OVERCLOUD_SKIP_MIE)
									atm.scattering += OverCloudMie(viewDir, distToCamera) * oc_pow3(scatteringMask);
								#endif
								ApplyAtmosphere(color.rgb, atm);
							#else
								EvaluateAtmosphere(i._atm, scatteringMask);
								#if defined(OVERCLOUD_SKIP_MIE)
									i._atm.scattering += OverCloudMie(viewDir, distToCamera) * oc_pow3(scatteringMask);
								#endif
								ApplyAtmosphere(color.rgb, i._atm);
							#endif

							// Debug the scattering mask
							// color.rgb = scatteringMask;
						#else
							UNITY_APPLY_FOG(i.fogCoord, color);
						#endif

						#ifdef DEBUG_LOD
							#if LOD_CLOUDS
								color.rb = 0;
							#else
								color.gb = 0;
							#endif
						#endif

						// Saturate alpha just to be safe
						color.a = saturate(color.a);

						// Premultiply
						color.rgb *= color.a;

						FragmentOutput o;
						o.color = color;
						// Store depth for transparency blending
						partZ = length(worldPos - _WorldSpaceCameraPos);
						color.a = smoothstep(0, 0.5, color.a);
						o.depth = float4(partZ, 0, 0, 1) * color.a; // Premultiply

						return o;
					}
				ENDCG 
			}
		}
	}
	FallBack "Self-Illumin/Diffuse"
}