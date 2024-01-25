///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

#ifndef OVERCLOUDCORE_INCLUDED
#define OVERCLOUDCORE_INCLUDED

	// ------------------ Core defines and functions ------------------

	#define OVERCLOUD
	#include "UnityCG.cginc"
	#include "BicubicLib.cginc"

	// The current origin offset (floating origin)
	float3 			_OverCloudOriginOffset;

	float			_OC_EarthRadius;
	float			_OC_PlanetScale;
	float			_OC_AtmHeightInv;

	// Render space to true world space
	inline float3 rs2ws (float3 rs)
	{
		return rs + _OverCloudOriginOffset;
	}
	// True world space to render space
	inline float3 ws2rs (float3 ws)
	{
		return ws - _OverCloudOriginOffset;
	}
	// Camera position in render space
	#define _RenderCamera _WorldSpaceCameraPos
	// Camera position in true world space
	#define _WorldCamera rs2ws(_RenderCamera)

	#define EARTH_RADIUS _OC_EarthRadius
	// Earth center relative to true world space camera position
	#define EARTH_CENTER float3(_WorldCamera.x, -EARTH_RADIUS, _WorldCamera.z)
	#define SUN_DIST 149.6 * 1000000 * 1000
	#define SUN_RADIUS 695508 * 1000
	#define MOON_DIST 384400 * 1000
	#define MOON_RADIUS 1737 * 1000
	#define ATM_HEIGHT_INV _OC_AtmHeightInv
	#define M_EPSILON 0.0001

	void OverCloudGammaCorrect (inout float3 color)
	{
		#ifdef UNITY_COLORSPACE_GAMMA
			color = LinearToGammaSpace(color);
		#endif
	}

	struct Atmosphere 
	{
		fixed3 scattering;
		fixed3 transmittance;
		fixed4 fog;
	};

	// ------------------ Atmosphere macros ------------------

	#define OVERCLOUD_COORDS(idx1) 									\
		Atmosphere _atm : TEXCOORD##idx1;

	#define OVERCLOUD_TRANSFER(worldPos, a) 						\
		a._atm = OverCloudAtmosphere(worldPos);

	#define OVERCLOUD_FRAGMENT_LITE(color) 							\
		EvaluateAtmosphere(i._atm, 1); 								\
		ApplyAtmosphere(color.rgb, i._atm);

	#define OVERCLOUD_FRAGMENT(color, screenUV) 					\
		float scatteringMask = OverCloudScatteringMask(screenUV); 	\
		EvaluateAtmosphere(i._atm, scatteringMask); 				\
		ApplyAtmosphere(color.rgb, i._atm);
		
	#define OVERCLOUD_FRAGMENT_FULL(color, screenUV, worldPos) 		\
		Atmosphere _atm = OverCloudAtmosphere(worldPos); 			\
		float scatteringMask = OverCloudScatteringMask(screenUV); 	\
		EvaluateAtmosphere(_atm, scatteringMask); 					\
		ApplyAtmosphere(color.rgb, _atm);

	#define OVERCLOUD_FRAGMENT_FULL_ADD(color, worldPos) 			\
		Atmosphere _atm = OverCloudAtmosphere(worldPos); 			\
		color.rgb = lerp(color.rgb, 0, _atm.fog.a);

	#define OVERCLOUD_OCEAN_BASE(color, screenUV, worldPos)						\
		if (_WorldSpaceCameraPos.y > worldPos.y)								\
		{																		\
			float4 clouds = tex2D(_OverCloudTex, screenUV);						\
			if (clouds.a > 0)													\
				color.rgb = lerp(color.rgb, clouds.rgb / clouds.a, clouds.a);	\
			color.a *= FarClipFade(length(worldPos - _WorldSpaceCameraPos));	\
		}

	#define OVERCLOUD_OCEAN_ADD(color, screenUV, worldPos)						\
		if (_WorldSpaceCameraPos.y > worldPos.y)								\
		{																		\
			float4 clouds = tex2D(_OverCloudTex, screenUV);						\
			if (clouds.a > 0)													\
				color.rgb = lerp(color.rgb, 0, clouds.a);						\
			color.rgb *= FarClipFade(length(worldPos - _WorldSpaceCameraPos));	\
		}

	// ------------------ Parameters ------------------

	// Rarely updated variables
	CBUFFER_START(OverCloudStatic)
		// Compositor etc.
		sampler2D 	_OC_NoiseTex;
		float2		_OC_NoiseScale;
		float 		_OC_Timescale;
		float2		_OC_CellSpan;
		sampler3D 	_OC_3DNoiseTex;
		float4		_OC_NoiseParams1;
		float4		_OC_NoiseParams2;
		float		_OC_Precipitation;
		float2		_OC_CloudOcclusionParams;
		float4		_OC_ShapeParams;
		float		_OC_NoiseErosion;
		float2		_OC_AlphaEdgeParams;

		sampler2D	_OC_RainRippleTex;
		sampler2D	_OC_RainMaskOffsetTex;
		float		_OC_RainMaskOffset;
		float4		_OC_RainMaskTexel;
		float3		_OC_RainMaskRadius;
		float		_OC_RainMaskFalloff;

		float3		_OC_EarthColor;

		// Volumetric cloud plane
		float		_OC_CloudAltitude;
		float		_OC_CloudPlaneRadius;
		float		_OC_CloudHeight;
		float		_OC_CloudHeightInv;

		float		_OC_CloudShadowsSharpen;
		sampler2D	_OC_CloudShadowsEdgeTex;
		float4		_OC_CloudShadowsEdgeTexParams;

		float4		_OC_CloudAlbedo;
		float4		_OC_CloudPrecipitationAlbedo;
		float4 		_OC_CloudParams1;
		float4 		_OC_CloudParams2;
		float4		_OC_CloudParams3;

		// Scattering & fog
		float		_OC_NightScattering;
		float4		_OC_MieScatteringParams;

		float		_OC_FarClipInv;
	CBUFFER_END

	// Volumetric clouds screen buffer
	sampler2D		_OverCloudTex;
	sampler2D 		_OverCloudDepthTex;

	// Compositor etc.
	sampler2D		_OC_CompositorTex;
	sampler2D		_OC_CloudShadowsTex;
	sampler2D 		_OC_RainMask;
	float3			_OC_RainMaskPosition;
	float3 			_OC_LightDir;
	float			_OC_LightDirYInv;
	float3 			_OC_LightColor;
	float 			_OC_GlobalWindTime;
	float			_OC_GlobalWindMultiplier;
	float4 			_OC_GlobalWetnessParams; // Intensity, albedo darken, gloss override, unused
	float4			_OC_GlobalRainParams; // Ripple intensity, ripple scale, flow intensity, flow scale
	float2			_OC_GlobalRainParams2; // Ripple timescale, flow timescale
	
	// Volumetric cloud plane
	float4			_OC_CloudColor;
	float4			_OC_Cloudiness;
	float2			_OC_CloudSharpness;
	float2			_OC_CloudDensity;
	float2			_OC_CloudShadowsParams;
	float4			_OC_CloudWorldExtentsMinMax;
	float4			_OC_CloudWorldExtents;
	float4			_OC_CloudShadowExtentsMinMax;
	float4			_OC_CloudShadowExtents;
	float3			_OC_CloudWorldPos;

	fixed4			_OC_AmbientColor;
	
	// Scattering & fog
	sampler2D		_OC_ScatteringMask;
	float			_OC_ScatteringMaskSoftness;
	float			_OC_ScatteringMaskFloor;

	float4			_OC_FogParams; // Fog ground density, fog air density, fog density height
	float			_OC_FogBlend;
	float4			_OC_FogColor;
	float			_OC_FogHeight;
	float2			_OC_FogFalloffParams;

	// Expand param vectors to separate uniforms
	#define _OC_NoiseTiling_A 				_OC_NoiseParams1.x
	#define _OC_NoiseIntensity_A 			_OC_NoiseParams1.y
	
	#define _OC_NoiseTiling_B 				_OC_NoiseParams1.z
	#define _OC_NoiseIntensity_B 			_OC_NoiseParams1.w

	#define _OC_NoiseTurbulence 			_OC_NoiseParams2.x
	#define _OC_NoiseRiseFactor 			_OC_NoiseParams2.y

	#define _OC_FogShadow					_OC_FogParams.w

	#define _OC_MieScatteringIntensity		_OC_MieScatteringParams.x
	#define _OC_MieScatteringPhase			_OC_MieScatteringParams.y
	#define _OC_MieScatteringFogPhase		_OC_MieScatteringParams.z
	#define _OC_MieScatteringFade			_OC_MieScatteringParams.w

	#define _OC_CloudOcclusionIntensity		_OC_CloudOcclusionParams.x
	#define _OC_CloudOcclusionHeightInv		_OC_CloudOcclusionParams.y

	#define _CloudVolumeCeiling 			(_OC_CloudAltitude + _OC_CloudHeight)
	#define _CloudVolumeFloor 				(_OC_CloudAltitude - _OC_CloudHeight)
	#define _CloudVolumeHeight				(_CloudVolumeCeiling - _CloudVolumeFloor)

	#define _OC_Eccentricity				_OC_CloudParams1.x
	#define _OC_SilverIntensity				_OC_CloudParams1.y
	#define _OC_SilverSpread				_OC_CloudParams1.z
	#define _OC_Direct						_OC_CloudParams1.w

	#define _OC_Indirect					_OC_CloudParams2.x
	#define _OC_Ambient						_OC_CloudParams2.y
	#define _OC_DirectAbsorption			_OC_CloudParams2.z
	#define _OC_IndirectAbsorption			_OC_CloudParams2.w

	#define _OC_IndirectSoftness			_OC_CloudParams3.x
	#define _OC_AmbientAbsorption			_OC_CloudParams3.y
	#define _OC_PowderSize					_OC_CloudParams3.z
	#define _OC_PowderIntensity				_OC_CloudParams3.w

	#define _OC_CloudShadowsDensity			_OC_CloudShadowsParams.x
	#define _OC_CloudShadowsOpacity			_OC_CloudShadowsParams.y

	#define M_PI 3.14159265f

	float			_OC_AtmosphereExposure;
	float			_OC_AtmosphereDensity;
	float			_OC_AtmosphereFarClipFade;
	float3 			_OC_ActualSunDir;
	float3 			_OC_ActualMoonDir;
	float4 			_OC_ActualSunColor;
	float4 			_OC_ActualMoonColor;
	float4			_OC_CurrentSunColor;
	float4			_OC_CurrentMoonColor;
	float			_SkySunSize;
	float			_SkyMoonSize;
	float			_SkySunIntensity;
	float			_SkyMoonIntensity;
	samplerCUBE		_SkyMoonCubemap;
	samplerCUBE		_SkyStarsCubemap;
	float			_SkyStarsIntensity;
	float			_SkySunGlowIntensity;
	float			_SkySunGlowG;
	float4			_SkySolarEclipse;
	float			_SkyMoonGlowIntensity;
	float			_SkyMoonGlowG;
	float4			_SkyLunarEclipse;
	float			_LunarEclipseLightingInfluence;

	sampler2D		_SkyCirrusTex;
	float4			_SkyCirrusColor;
	float			_SkyCirrusHeight;
	float			_SkyCirrusScale;
	float			_SkyCirrusOpacity;
	float			_SkyCirrusWidth;
	float			_SkyCirrusDensity;

	float3 			_ScattEarthCenter;
	float3 			_ScattSunDir;
	float2 			_ScattSunSize;

	// ------------------ Math utility functions ------------------

	inline float oc_pow2 (float f)
	{
		return f * f;
	}

	inline float oc_pow3 (float f)
	{
		return f * f * f;
	}

	inline float oc_pow4 (float f)
	{
		return f * f * f * f;
	}

	inline float oc_pow6 (float f)
	{
		return f * f * f * f * f * f;
	}

	inline float oc_pow8 (float f)
	{
		return f * f * f * f * f * f * f * f;
	}

	// Rotation with angle (in radians) and axis
	float3x3 AngleAxis3x3(float angle, float3 axis) 
	{
		float c, s;
		sincos(angle, s, c);

		float t = 1 - c;
		float x = axis.x;
		float y = axis.y;
		float z = axis.z;

		return float3x3(
			t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
			t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
			t * x * z - s * y,  t * y * z + s * x,  t * z * z + c
		);
	}

	float3 mod289(float3 x)
	{
	    return x - floor(x / 289.0) * 289.0;
	}

	float4 mod289(float4 x)
	{
	    return x - floor(x / 289.0) * 289.0;
	}

	float4 permute(float4 x)
	{
	    return mod289((x * 34.0 + 1.0) * x);
	}

	float4 taylorInvSqrt(float4 r)
	{
	    return 1.79284291400159 - r * 0.85373472095314;
	}

	float4 perm(float4 x){return mod289(((x * 34.0) + 1.0) * x);}

	float noise (float3 p)
	{
		float3 a = floor(p);
		float3 d = p - a;
		d = d * d * (3.0 - 2.0 * d);

		float4 b = a.xxyy + float4(0.0, 1.0, 0.0, 1.0);
		float4 k1 = perm(b.xyxy);
		float4 k2 = perm(k1.xyxy + b.zzww);

		float4 c = k2 + a.zzzz;
		float4 k3 = perm(c);
		float4 k4 = perm(c + 1.0);

		float4 o1 = frac(k3 * (1.0 / 41.0));
		float4 o2 = frac(k4 * (1.0 / 41.0));

		float4 o3 = o2 * d.z + o1 * (1.0 - d.z);
		float2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

		return ((o4.y * d.y + o4.x * (1.0 - d.y)) - 0.5) * 2;
	}

	float noise2D(float2 p)
	{
		return noise(float3(p, 0));
	}

	// Ray-sphere intersection test
	bool RayIntersect
	(
		// Ray
		float3 ro, // Origin
		float3 rd, // Direction

		// Sphere
		float3 center, // Centre
		float radius, // Radius
		out float t0, // First intersection time
		out float t1  // Second intersection time
	)
	{
		float3 L = center - ro;
		float tca = dot(L, rd);
		float radius2 = radius * radius;

		float d2 = dot(L, L) - tca * tca;

		// Intersection point outside the sphere
		if (d2 > radius2)
			return false;

		float thc = sqrt(radius2 - d2);

		t0 = tca - thc;
		t1 = tca + thc;

		// t0 is the distance to the closest intersection in front of the ray
		if (t1 < t0)
		{
			float tmp = t0;
			t0 = t1;
			t1 = tmp;
		}

		if (t0 < 0)
		{
			float tmp = t0;
			t0 = t1;
			t1 = tmp;
			// t0 = t1; // if t0 is negative, let's use t1 instead 
			if (t0 < 0)
				return false; // both t0 and t1 are negative 
		} 

		return true;
	}

	// ---------------- Lighting utility functions ----------------

	// Henyey-Greenstein phase function
	float hg_phase(float costh, float g)
	{
		return (1 - g*g) / (4 * M_PI * pow(1 + g*g - 2 * g * costh, 1.5));
	}

	// Schlick approximation
	float hg_schlick(float costh, float g)
	{
		g = min(g, 0.9381);
		float k = 1.55*g - 0.55*g*g*g;

		float kcosth = k*costh;

		return (1 - k*k) / ((4 * M_PI) * (1-kcosth) * (1-kcosth));
	}

	fixed3 CloudLighting (
		fixed3 albedo,
		float  baseDensity,
		float  densityAlongRay,
		float3 viewDir,
		float3 lightDir,
		float3 lightColor,
		float sugaredPowder = 1)
	{
		// Phase function
		float viewDot = dot(viewDir, -lightDir)*0.5+0.5;
		float phase = max( _OC_Eccentricity, _OC_SilverIntensity * hg_schlick(viewDot, 0.99 - _OC_SilverSpread) );
		// Direct energy
		float e_direct   = exp(-densityAlongRay * _OC_DirectAbsorption) * phase * _OC_Direct;
		// Indirect energy
		float e_indirect = exp(-densityAlongRay * _OC_IndirectAbsorption) * _OC_Indirect;
		// "Sugared powder" effect
		float powder = 1.0 - exp(-baseDensity * (1-_OC_PowderSize)) * _OC_PowderIntensity * 8 * sugaredPowder;
		// float powder = max(1.0 - exp(-baseDensity * baseDensity * (1-_OC_PowderIntensity)), 0);
		powder = max(lerp(powder, 1, viewDot), 0);
		// Final light energy
		float3 e = (e_direct + e_indirect) * powder * lightColor;
		// Final color value
		return albedo * e;
	}

	fixed3 CloudAmbient (fixed3 albedo, float baseDensity)
	{
		return albedo * exp(-baseDensity * _OC_AmbientAbsorption) * _OC_AmbientColor * _OC_Ambient;
	}

	// ------------------ Ray marching functions ------------------

	float4x4 	_FrustumCorners;
	float4x4 	_FrustumCornersLeft;
	float4x4 	_FrustumCornersRight;

	// Get frustum corner ray based on texcoords
	float3 InterpolatedRay (float2 texcoord)
	{
		// Generate index based on vertex coordinates
		int index = texcoord.x + 2 * (1 - texcoord.y);
		
		#if UNITY_SINGLE_PASS_STEREO
			// If we are doing single pass stereoscopic rendering, we have two matrices. One for each eye.
			if (unity_StereoEyeIndex == 0)
				return _FrustumCornersLeft[index];
			else
				return _FrustumCornersRight[index];
		#else
			return _FrustumCorners[index];
		#endif
	}

	float4x4 _WorldFromView;
	float4x4 _ViewFromScreen;
	float4x4 _LeftWorldFromView;
	float4x4 _RightWorldFromView;
	float4x4 _LeftViewFromScreen;
	float4x4 _RightViewFromScreen;

	// Recover world position from depth through inverse projection
	// This ensures all ray march shaders work even in single pass stereo
	void InverseProjectDepth (float depth, float2 texcoord, out float3 worldPos, out float dist, out float3 viewDir)
	{
		float4x4 proj, eyeToWorld;
		#if UNITY_SINGLE_PASS_STEREO
			if (unity_StereoEyeIndex == 0)
			{
				proj 		= _LeftViewFromScreen;
				eyeToWorld 	= _LeftWorldFromView;
			}
			else
			{
				proj 		= _RightViewFromScreen;
				eyeToWorld 	= _RightWorldFromView;
			}
		#else
			proj 		= _ViewFromScreen;
			eyeToWorld 	= _WorldFromView;
		#endif

		#if !UNITY_UV_STARTS_AT_TOP
			texcoord.y = 1 - texcoord.y;
		#endif
		float2 uvClip = texcoord * 2.0 - 1.0;
		float4 clipPos = float4(uvClip, depth, 1.0);
		float4 viewPos = mul(proj, clipPos); // inverse projection by clip position
		viewPos /= viewPos.w; // perspective division
		worldPos = mul(eyeToWorld, viewPos).xyz;
		viewDir = worldPos - _RenderCamera;
		dist = length(viewDir);
		viewDir /= dist;
	}

	// ------------------ Lighting functions ------------------

	float OverCloudScatteringMask (float2 screenUV)
	{
		return tex2D(_OC_ScatteringMask, screenUV).r;
	}

	void OverCloudFragmentFull (inout float3 color, float4 fog, float3 scattering, float3 extinction, float3 worldPos, float2 screenUV)
	{
		float scatteringMask = OverCloudScatteringMask(screenUV);
		scattering *= scatteringMask;
		fog.a *= scatteringMask;
		color.rgb = lerp(color.rgb, fog.rgb, fog.a) * extinction + scattering;
	}

	// Main cloud density function.
	// Used by the compositor to generate sky coverage, shadows and scattering mask (R, G, B)
	// Note that worldPos is in render space
	float CloudDensity (float3 worldPos)
	{
		// Render space -> true world space
		worldPos = rs2ws(worldPos);

		// Local density
		float2 uv = (worldPos.xz + float2(1, 0) * _OC_GlobalWindTime) * _OC_NoiseScale.x;
		
		float density = tex2D(_OC_NoiseTex, uv).r;

		density = max(density - (1 - _OC_Cloudiness.x), 0);
		density = lerp(_OC_Cloudiness.y, 1, density);
		density = smoothstep(_OC_CloudSharpness.x * 0.499, 1 - _OC_CloudSharpness.x * 0.499, density);

		// Macro density
		uv = (worldPos.xz + float2(1, 0) * _OC_GlobalWindTime) * _OC_NoiseScale.y;

		float macroDensity = tex2D(_OC_NoiseTex, uv).g;

		macroDensity = max(macroDensity - (1 - _OC_Cloudiness.z), 0);
		macroDensity = lerp(_OC_Cloudiness.w, 1, macroDensity);
		macroDensity = smoothstep(_OC_CloudSharpness.y * 0.499, 1 - _OC_CloudSharpness.y * 0.499, macroDensity);

		density *= macroDensity;

		return density;
	}

	// Vertex version of above function
	float CloudDensityV (float3 worldPos)
	{
		// Render space -> true world space
		worldPos = rs2ws(worldPos);

		// Local density
		float2 uv = (worldPos.xz + float2(1, 0) * _OC_GlobalWindTime) * _OC_NoiseScale.x;
		float density = tex2Dlod(_OC_NoiseTex, float4(uv.x, uv.y, 0, 0)).r;

		density = max(density - (1 - _OC_Cloudiness.x), 0);
		density = lerp(_OC_Cloudiness.y, 1, density);
		density = smoothstep(_OC_CloudSharpness.x * 0.499, 1 - _OC_CloudSharpness.x * 0.499, density);

		// Macro density
		uv = (worldPos.xz + float2(1, 0) * _OC_GlobalWindTime) * _OC_NoiseScale.y;

		float macroDensity = tex2Dlod(_OC_NoiseTex, float4(uv.x, uv.y, 0, 0)).g;
		macroDensity = max(macroDensity - (1 - _OC_Cloudiness.z), 0);
		macroDensity = lerp(_OC_Cloudiness.w, 1, macroDensity);
		macroDensity = smoothstep(_OC_CloudSharpness.y * 0.499, 1 - _OC_CloudSharpness.y * 0.499, macroDensity);

		density *= macroDensity;

		return density;
	}

	// Calculate cloud attenuation (for transparent blending)
	float CloudAttenuation (float distToCamera, float2 screenUV, float invSoftness)
	{
		float4 depthTex = tex2D(_OverCloudDepthTex, screenUV);
		float4 cloudTex = tex2D(_OverCloudTex, screenUV);
		return 1 - saturate((distToCamera - depthTex.r) * invSoftness) * depthTex.a * cloudTex.a;
	}

	// Smoothly fade from center of cloud plane to below it
	float BelowClouds (float height)
	{
		// _OC_CloudAltitude already contains the offset, don't apply it again
		// height += _OverCloudOriginOffset.y;
		height -= _OC_CloudAltitude;
		return (1 - saturate(height * _OC_CloudHeightInv * 2));
	}

	// Smoothly fade from center of cloud plane to above it
	float AboveClouds (float height)
	{
		// _OC_CloudAltitude already contains the offset, don't apply it again
		// height += _OverCloudOriginOffset.y;
		height -= _OC_CloudAltitude;
		return (1 - saturate(-height * _OC_CloudHeightInv * 2));
	}

	// Cloud ambient occlusion
	float CloudOcclusion (float3 worldPos)
	{
		float2 cscoords = max(worldPos.xz - _OC_CloudWorldExtentsMinMax.xy, 0) * _OC_CloudWorldExtents.zw;
		// float cloudOcclusion = 1 - tex2Dlod(_OC_CompositorTex, float4(cscoords.xy, 0, 3)).r;
		float heightAtten = 1-min(max(_OC_CloudAltitude - worldPos.y, 0) * _OC_CloudOcclusionHeightInv, 1);
		heightAtten *= BelowClouds(worldPos.y);
		return max(1 - tex2D(_OC_CompositorTex, cscoords.xy).b * heightAtten * _OC_CloudOcclusionIntensity, 0);
	}

	float CloudOcclusionLOD (float3 worldPos, float lod = 0)
	{
		float2 cscoords = max(worldPos.xz - _OC_CloudWorldExtentsMinMax.xy, 0) * _OC_CloudWorldExtents.zw;
		// float cloudOcclusion = 1 - tex2Dlod(_OC_CompositorTex, float4(cscoords.xy, 0, 3)).r;
		float heightAtten = 1-min(max(_OC_CloudAltitude - worldPos.y, 0) * _OC_CloudOcclusionHeightInv, 1);
		heightAtten *= BelowClouds(worldPos.y);
		return max(1 - tex2Dlod(_OC_CompositorTex, float4(cscoords.xy, 0, lod)).b * heightAtten * _OC_CloudOcclusionIntensity, 0);
	}

	// Check how much precipitation a surface is receiving from the clouds
	inline float RainSurface (float3 worldPos, float3 worldNormal)
	{
		float2 cscoords = max(worldPos.xz - _OC_CloudWorldExtentsMinMax.xy, 0) * _OC_CloudWorldExtents.zw;
		// worldPos += _OverCloudOriginOffset;
		
		float density = tex2D(_OC_CompositorTex, cscoords.xy).r;//CloudDensity(worldPos);
		// density = smoothstep(_OC_GlobalWetnessParams.x * 0.499, 1 - _OC_GlobalWetnessParams.x * 0.499, density);
		density = smoothstep(0, 1 - _OC_GlobalWetnessParams.x * 0.999, density);

		float rainSurface = density * BelowClouds(worldPos.y) * _OC_Precipitation;

		#if RAIN_MASK_ENABLED
			float2 rainMaskUVmin = _OC_RainMaskPosition.xz - _OC_RainMaskRadius.x;
			float2 rainMaskUV = (worldPos.xz - rainMaskUVmin) * _OC_RainMaskRadius.z;
			// Rain mask bounds check
			if (rainMaskUV.x >= 0 && rainMaskUV.x <= 1 && rainMaskUV.y >= 0 && rainMaskUV.y <= 1)
			{
				float2 rainMaskOffsetUV = (worldPos.xz + _OverCloudOriginOffset.xz) * 1;
				float2 rainMaskOffset = (tex2D(_OC_RainMaskOffsetTex, rainMaskOffsetUV).rg - float2(0.5, 0.5)) * 2;
				rainMaskOffset *= _OC_RainMaskOffset * 1 * _OC_RainMaskRadius.z;

				float rainMask 		= tex2D(_OC_RainMask, rainMaskUV + rainMaskOffset).r;
				float rainHeight 	= _OC_RainMaskPosition.y - rainMask;
				float rainFalloff 	= min(max(rainMask - worldPos.y, 0) * _OC_RainMaskFalloff, 1);
				float distFalloff	= min(length(worldPos.xz - _OC_RainMaskPosition.xz) * _OC_RainMaskRadius.y, 1);
				distFalloff			= 1 - (1 - distFalloff * distFalloff);
				rainFalloff			= lerp(rainFalloff, 0, distFalloff);
				rainSurface 		= lerp(rainSurface, 0, rainFalloff);
				rainSurface 		= lerp(rainSurface, 0, max(-worldNormal.y, 0));
			}
		#endif

		return rainSurface;
	}

	// Surface wetness evaluation function
	// Use in combination with RainSurface to modify albedo and gloss
	inline void EvaluateWetness (inout float3 albedo, inout float gloss, float rainSurface, float wetnessIn = 0)
	{
		float wetness = max(rainSurface * _OC_Precipitation, wetnessIn);
		albedo *= 1 - wetness * _OC_GlobalWetnessParams.y;
		gloss = lerp(gloss, 1, wetness * _OC_GlobalWetnessParams.z);
	}

	// Get rain ripple normals for a surface
	float3 RainRipplesTangent (float3 worldPos, float3 worldNormal, float rainSurface)
	{
		// Render space -> true world space
		worldPos = rs2ws(worldPos);

		float2 uv = worldPos.xz * _OC_GlobalRainParams.y;

		float3 tangent = tex2D(_OC_RainRippleTex, uv)*2-1;

		return lerp(float3(0, 0, 1), tangent, max(worldNormal.y * worldNormal.y, 0) * rainSurface);
	}

	// Get cloud shadow coordinates by projecting sample position up into the cloud layer
	// This function can be evaluated in the vertex shader
	// cscoords:
	// xy: Normalized compositor coordinates
	// z: shadow opacity
	float3 CloudShadowCoordinates (float3 worldPos, float3 lightDir)
	{
		// The compositor already performs the XZ origin offset, and _OC_CloudAltitude is adjusted
		// worldPos += _OverCloudOriginOffset;

		// Project onto cloud plane
		float3 L = -lightDir;
		float h = _OC_CloudAltitude - worldPos.y;
		float3 cloudLayerPos = worldPos.xyz + lightDir * h * _OC_LightDirYInv;

		float3 cscoords;
		cscoords.xy = (cloudLayerPos.xz - _OC_CloudShadowExtentsMinMax.xy) * _OC_CloudShadowExtents.zw;
		cscoords.z = BelowClouds(worldPos.y);
		// Fade out cloud shadows when light is close to horizon
		cscoords.z *= saturate((-lightDir.y-0.025) * 10);

		// Circular falloff since we have limited shadow range
		float fade = min(length(cscoords.xy - float2(0.5, 0.5)) * 2, 1);
		fade = 1 - fade  * fade * fade * fade;
		cscoords.z *= fade;

		return cscoords;
	}

	float3 CloudShadowCoordinates (float3 worldPos)
	{
		return CloudShadowCoordinates(worldPos, _OC_LightDir);
	}

	// Evaluate cloud shadows based on supplied world position and sample coordinates
	inline float EvaluateCloudShadows (float3 worldPos, float3 cscoords, float intensity = 1)
	{
		// Render space -> true world space
		worldPos = rs2ws(worldPos);

		// Generated global shadows
		float shadowTex = tex2D(_OC_CloudShadowsTex, cscoords.xy).r;

		// shadowTex = tex2Dbicubic(
		// 	_OC_CloudShadowsTex,
		// 	cscoords.xy,
		// 	float2(1024, 1024),
		// 	float2(1 / 1024, 1 / 1024)
		// ).r;

		float scaleFactor = _OC_CloudShadowsEdgeTexParams.y;
		// float scaleFactor = 1 / (_OC_CloudPlaneRadius * 2);
		// Refinement texture
		float2 refineUV = (cscoords.xy + rs2ws(_OC_CloudWorldPos).xz * scaleFactor) * _OC_CloudShadowsEdgeTexParams.w * 32;
		refineUV += float2(1, 0) * _OC_GlobalWindTime * _OC_NoiseScale.x;
		float shadowRefine = tex2D(_OC_CloudShadowsEdgeTex, refineUV).r;

		// Refine global shadows
		shadowTex = min(shadowTex + shadowRefine * shadowTex * _OC_CloudShadowsEdgeTexParams.z, 1);
		shadowTex = max(shadowTex - (1-shadowRefine) * (1-shadowTex) * _OC_CloudShadowsEdgeTexParams.z, 0);
		
		shadowTex = smoothstep(_OC_CloudShadowsSharpen * 0.4999, 1 - _OC_CloudShadowsSharpen * 0.4999, shadowTex);

		// Apply fade and return
		return max(1 - shadowTex * cscoords.z * _OC_CloudShadowsOpacity * intensity, 0);
	}

	// Get cloud shadow coordinates and evaluate shadows immediately
	inline float CloudShadows (float3 worldPos, float intensity = 1)
	{
		return EvaluateCloudShadows(worldPos, CloudShadowCoordinates(worldPos), intensity);
	}

	// Get cloud shadow coordinates and evaluate shadows immediately
	inline float CloudShadows (float3 worldPos, float3 lightDir, float intensity = 1)
	{
		return EvaluateCloudShadows(worldPos, CloudShadowCoordinates(worldPos, lightDir), intensity);
	}

	// Vertex shader versions of above functions
	inline float EvaluateCloudShadowsV (float3 cscoords, float intensity = 1)
	{
		float shadowTex = tex2Dlod(_OC_CloudShadowsTex, float4(cscoords.xy, 0, 0)).r;
		return max(1 - shadowTex * cscoords.z * _OC_CloudShadowsOpacity * intensity, 0);
	}
	inline float CloudShadowsV (float3 worldPos, float intensity = 1)
	{
		return EvaluateCloudShadowsV(CloudShadowCoordinates(worldPos), intensity);
	}
	inline float CloudShadowsV (float3 worldPos, float3 lightDir, float intensity = 1)
	{
		return EvaluateCloudShadowsV(CloudShadowCoordinates(worldPos, lightDir), intensity);
	}

	float FogShadow (float3 worldPos, float3 lightDir)
	{
		// Render space -> true world space
		worldPos = rs2ws(worldPos);

		if (worldPos.y > _OC_FogHeight)
			return 1;

		float t0, t1;
		if (RayIntersect(worldPos, -lightDir, EARTH_CENTER, EARTH_RADIUS + _OC_FogHeight, t0, t1))
		{
			float dist = t0;
			float falloff = min(max(_OC_FogHeight - (worldPos.y + _OC_FogHeight) * 0.5, 0) * _OC_FogFalloffParams.y, 1);
			return saturate(exp2(-_OC_FogParams.x * dist * falloff * _OC_FogShadow));
		}

		return 1;
	}

	inline float FogShadow (float3 worldPos)
	{
		return FogShadow(worldPos, _OC_LightDir);
	}

	UNITY_DECLARE_SHADOWMAP(_CascadeShadowMapTexture);

	inline fixed4 GetCascadeWeights_SplitSpheres(float3 wpos)
	{
		float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
		float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
		float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
		float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
		float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

		fixed4 weights = float4(distances2 < unity_ShadowSplitSqRadii);
		weights.yzw = saturate(weights.yzw - weights.xyz);
		return weights;
	}

	inline float4 GetCascadeShadowCoord(float4 wpos, fixed4 cascadeWeights)
	{
		float3 sc0 = mul(unity_WorldToShadow[0], wpos).xyz;
		float3 sc1 = mul(unity_WorldToShadow[1], wpos).xyz;
		float3 sc2 = mul(unity_WorldToShadow[2], wpos).xyz;
		float3 sc3 = mul(unity_WorldToShadow[3], wpos).xyz;
		
		float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
		#if defined(UNITY_REVERSED_Z)
			float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
			shadowMapCoordinate.z += noCascadeWeights;
		#endif
		return shadowMapCoordinate;
	}

#endif // OVERCLOUDCORE_INCLUDED