///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

#ifndef OVERCLOUDMAIN_INCLUDED
#define OVERCLOUDMAIN_INCLUDED

	#include "UnityCG.cginc"
	#include "Lighting.cginc"
	#include "OverCloudCore.cginc"
	#include "Atmosphere.cginc"

	// Math stuff
	// #define M_PI 3.14159265359 // Already defined in OverCloudCore
	#define M_RPI 0.31830988618
	#define M_HPI 1.57079632679
	#define M_LOG2 0.30102999566
	#define DEGTORAD 0.01745329251
	#define EXP(a) exp(a)

	float4		_OC_PointLightPosRadius;
	float4		_OC_PointLightColor;

	float 		_FlatnessMin; // TODO: Check
	float	 	_FlatnessMax; // TODO: Check
	float	 	_FlatnessNearfade; // TODO: Check
	float 		_Curvature; // TODO: Check
	float		_PerturbSpeed; // TODO: Check
	float	 	_RayLength;
	float		_PointRayLength;
	float		_PrecipitationMul;
	float 		_InvFade;

	// Property block data
	float2 	_RandomRange;
	float 	_Radius;
	float	_NearRadius;
	float2 	_ParticleScale;

	#define HG hg_schlick

	float Radius (float density)
	{
		return _OC_CloudHeight * _ParticleScale.x;
	}

	float HeightMap (float height)
	{
		if (height <= _OC_ShapeParams.x)
		{
			return height * _OC_ShapeParams.y;
		}
		else
		{
			return 1 - (height - _OC_ShapeParams.x) * _OC_ShapeParams.z;
		}
	}

	// Base cloud density
	float CloudDensity3DBase (float3 worldPos, out float3 masks)
	{
		if (abs(worldPos.y - _OC_CloudAltitude) > _OC_CloudHeight)
		{
			// Outside of cloud volume
			masks = float3(0, 0, 1);
			return 0;
		}

		float2 cscoords = max(worldPos.xz - _OC_CloudWorldExtentsMinMax.xy, 0) * _OC_CloudWorldExtents.zw;
		float4 compositor = tex2D(_OC_CompositorTex, cscoords.xy);

		// Height mask
		masks.y = worldPos.y - (_OC_CloudAltitude - _OC_CloudHeight);
		masks.y *= _OC_CloudHeightInv * 0.5;

		float shape = compositor.r * HeightMap(masks.y);
		float density = shape;

		float h = 1 - masks.y;
		density *= 1 + lerp(0, _OC_ShapeParams.w, h * h * h * h);

		// Erosion mask
		masks.x = oc_pow4(saturate(_OC_NoiseErosion - shape));

		// Light mask
		masks.z = 1;

		return density;
	}

	// Low-res cloud density
	void CloudErosion1 (float3 worldPos, inout float density, float erosionMask)
	{
		// Need to apply floating origin offset before sampling 3D noise textures
		worldPos += _OverCloudOriginOffset;
		
		// 1st pass erosion
		float3 uv = (worldPos + float3(1, 0, 0) * _OC_GlobalWindTime * (1 + _OC_NoiseTurbulence)) * _OC_NoiseTiling_A;
		// Apply rise factor
		uv.y -= _OC_GlobalWindTime * _OC_NoiseRiseFactor * _OC_NoiseTiling_A;
		// Sample the 3D noise texture
		float n = tex3D(_OC_3DNoiseTex, uv).r;
		// Apply noise
		density *= lerp(1, n, erosionMask * _OC_NoiseIntensity_A);
	}

	// High-res cloud density
	void CloudErosion2 (float3 worldPos, inout float density, float erosionMask)
	{
		// Need to apply floating origin offset before sampling 3D noise textures
		worldPos += _OverCloudOriginOffset;
		
		// 2nd pass erosion. 1st pass should be applied first before calling this function
		float3 uv = (worldPos + float3(1, 0, 0) * _OC_GlobalWindTime * (1 + _OC_NoiseTurbulence)) * _OC_NoiseTiling_B;
		// Apply rise factor
		uv.y -= _OC_GlobalWindTime * _OC_NoiseRiseFactor * _OC_NoiseTiling_B;
		// Sample the 3D noise texture
		float n = tex3D(_OC_3DNoiseTex, uv).r;
		// Apply noise
		density *= lerp(1, n, erosionMask * _OC_NoiseIntensity_B);
	}

	float R2D (float2 st)
	{
		return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
	}

	float random (float s)
	{
		return frac(sin(dot(s, 12.9898)) * 43758.5453123);
	}

	float4 _PixelSizeDS;

	// MRT shader
	struct FragmentOutput
	{
		half4 color : SV_Target0;
		float4 depth : SV_Target1;
	};

#endif // OVERCLOUDMAIN_INCLUDED