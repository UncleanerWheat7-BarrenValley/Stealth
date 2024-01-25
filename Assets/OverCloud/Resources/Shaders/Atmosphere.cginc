///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

#ifndef ATMOSPHERE_INCLUDED
#define ATMOSPHERE_INCLUDED

	#include "OverCloudCore.cginc"

	#include "../../Atmosphere/Shaders/Definitions.cginc"
	#include "../../Atmosphere/Shaders/UtilityFunctions.cginc"
	#include "../../Atmosphere/Shaders/TransmittanceFunctions.cginc"
	#include "../../Atmosphere/Shaders/ScatteringFunctions.cginc"
	#include "../../Atmosphere/Shaders/IrradianceFunctions.cginc"
	#include "../../Atmosphere/Shaders/RenderingFunctions.cginc"

	sampler2D transmittance_texture;
	sampler2D irradiance_texture;
	sampler3D scattering_texture;
	sampler3D single_mie_scattering_texture;

	float4 SunColor ()
	{
		float4 sunColor = _OC_ActualSunColor;
		sunColor.rgb = lerp(sunColor.rgb, _SkySolarEclipse.rgb, _SkySolarEclipse.a);
		return sunColor;
	}

	float4 MoonColor ()
	{
		float4 moonColor = _OC_ActualMoonColor;
		moonColor.rgb = lerp(moonColor.rgb, _SkyLunarEclipse.rgb, _SkyLunarEclipse.a);
		return moonColor;
	}

	float4 NightColor ()
	{
		float4 nightColor = _OC_ActualMoonColor;
		nightColor.rgb = lerp(nightColor.rgb, _SkyLunarEclipse.rgb, _SkyLunarEclipse.a * _LunarEclipseLightingInfluence);
		return nightColor;
	}

	// Adjust a world position for the curvature of the Earth.
	inline float3 EarthCurvature (float3 worldPos)
	{
		float shift = -oc_pow2(length((worldPos - _RenderCamera).xz)) * 8e-8;
		worldPos.y += shift;
		return worldPos;
	}
	

	// Brunetons atmosphere implementation
	#ifdef RADIANCE_API_ENABLED
		RadianceSpectrum GetSolarRadiance() 
		{
			return solar_irradiance / (PI * sun_angular_radius * sun_angular_radius);
		}

		RadianceSpectrum GetSkyRadiance(
			Position camera, Direction view_ray, Length shadow_length,
			Direction sun_direction, out DimensionlessSpectrum transmittance) 
		{
			return GetSkyRadiance(transmittance_texture,
				scattering_texture, single_mie_scattering_texture,
				camera, view_ray, shadow_length, sun_direction, transmittance);
		}

		RadianceSpectrum GetSkyRadianceToPoint(
			Position camera, Position _point, Length shadow_length,
			Direction sun_direction, out DimensionlessSpectrum transmittance) 
		{
			return GetSkyRadianceToPoint(transmittance_texture,
				scattering_texture, single_mie_scattering_texture,
				camera, _point, shadow_length, sun_direction, transmittance);
		}

		DimensionlessSpectrum GetTransmittanceToPoint(
			Position camera, Position _point) 
		{
			return GetTransmittanceToPoint(transmittance_texture,
				camera, _point);
		}

		IrradianceSpectrum GetSunAndSkyIrradiance(
			Position p, Direction normal, Direction sun_direction,
			out IrradianceSpectrum sky_irradiance) 
		{
			return GetSunAndSkyIrradiance(transmittance_texture,
				irradiance_texture, p, normal, sun_direction, sky_irradiance);
		}
	#else
		Luminance3 GetSolarRadiance()
		{
			return solar_irradiance /
				(PI * sun_angular_radius * sun_angular_radius) *
				SUN_SPECTRAL_RADIANCE_TO_LUMINANCE;
		}

		Luminance3 GetSkyRadiance(
			Position camera, Direction view_ray, Length shadow_length,
			Direction sun_direction, out DimensionlessSpectrum transmittance) 
		{
			return GetSkyRadiance(transmittance_texture,
				scattering_texture, single_mie_scattering_texture,
				camera, view_ray, shadow_length, sun_direction, transmittance) *
				SKY_SPECTRAL_RADIANCE_TO_LUMINANCE;
		}

		Luminance3 GetSkyRadianceToPoint(
			Position camera, Position _point, Length shadow_length,
			Direction sun_direction, out DimensionlessSpectrum transmittance) 
		{
			return GetSkyRadianceToPoint(transmittance_texture,
				scattering_texture, single_mie_scattering_texture,
				camera, _point, shadow_length, sun_direction, transmittance) *
				SKY_SPECTRAL_RADIANCE_TO_LUMINANCE;
		}

		DimensionlessSpectrum GetTransmittanceToPoint(
			Position camera, Position _point) 
		{
			return GetTransmittanceToPoint(transmittance_texture,
				camera, _point);
		}

		Illuminance3 GetSunAndSkyIrradiance(
			Position p, Direction normal, Direction sun_direction,
			out IrradianceSpectrum sky_irradiance) 
		{
			IrradianceSpectrum sun_irradiance = GetSunAndSkyIrradiance(
				transmittance_texture, irradiance_texture, p, normal,
				sun_direction, sky_irradiance);
			sky_irradiance *= SKY_SPECTRAL_RADIANCE_TO_LUMINANCE;
			return sun_irradiance * SUN_SPECTRAL_RADIANCE_TO_LUMINANCE;
		}
	#endif

	// Far clip fade factor
	float FarClipFade (float distToCamera)
	{
		float distFade = saturate(distToCamera * _OC_FarClipInv * _OC_AtmosphereFarClipFade);
		distFade = max(distFade*2-1, 0);
		return 1 - oc_pow2(distFade);
	}

	float3 OverCloudMie (float3 viewDir, float distToCamera)
	{
		// Atmosphere distance fade
		// float atm = 1 - exp(-dist * _OC_MieScatteringFade);
		float atm = 1 - exp2(-distToCamera * _OC_MieScatteringFade);
		atm = oc_pow4(atm);
		// Atmosphere height fade
		float atmFade = 1 - min(_WorldCamera.y * ATM_HEIGHT_INV, 1);
		atmFade *= atmFade;

		float3 transmittance = GetTransmittanceToPoint(_RenderCamera - _ScattEarthCenter, _RenderCamera - _OC_ActualSunDir * 9999999 - _ScattEarthCenter);
		transmittance = lerp(1, transmittance, 0.5);

		// Fade when light is close to horizon
		float horizonFade = saturate((-_OC_LightDir.y) * 10);

		return hg_schlick(dot(-viewDir, _OC_LightDir)*0.5+0.5, _OC_MieScatteringPhase) * _OC_MieScatteringIntensity * _OC_LightColor.rgb * atm * atmFade * horizonFade * transmittance;
	}

	// Night/ambient sky (not physically based)
	float3 NightScattering (float3 camera, float3 transmittance)
	{
		float3 nightColor = NightColor();

		// Transmittance, in a way, defines how far away a point is in the atmosphere
		// However, it is a three-component value (RGB), so we get an approximate value
		// for the night-time scattering by calculating the luminance (dot product below)
		// (since transmittance doesn't change with sun position or color)
		float3 nightScattering = (1-dot(transmittance, float3(0.3, 0.59, 0.11)));
		nightScattering = nightScattering * 0.35 * nightColor.rgb * _OC_NightScattering;
		return nightScattering;// * (1 - min((camera.y + _OverCloudOriginOffset.y) / 200000, 1));
	}

	// Calculate fog attenuation between two points (basically HeightFog without the light color and worldPos shift)
	inline float FogAttenuation (float3 worldPos1, float3 worldPos2)
	{
		// Calculate distance to fragment and view direction
		float3 viewDir 	= worldPos2 - worldPos1;
		float dist 		= length(viewDir);
		viewDir 		/= dist;

		float startHeight = worldPos1.y;
		float endHeight = worldPos2.y;

		// The center of the earth assuming the camera is always located at xz = 0
		float3 earthCenter = float3(0, -EARTH_RADIUS, 0);

		// Fix precision artifacts when camera height is really close to the fog height
		if (abs(worldPos1.y - _OC_FogHeight) < 1)
			worldPos1.y = _OC_FogHeight - 1;
		
		if (worldPos1.y < _OC_FogHeight)
		{
			// Inside fog atmosphere, find exit point
			float t0, t1;
			if (RayIntersect(worldPos1, viewDir, earthCenter, EARTH_RADIUS + _OC_FogHeight, t0, t1))
			{
				// Distance to intersection with height fog atmosphere shell
				float newDist = t0;
				if (newDist < dist)
				{
					// The exit point is closer than the current fragment world position, update dist
					dist = newDist;
					// Used to calculate height falloff
					endHeight = (worldPos1 + viewDir * t0).y;
				}
			}
			else
			{
				// No intersection. Should be impossible, but set regardless
				dist = 0;
			}
		}
		else
		{
			// Outside fog atmosphere, find entry point
			float t0, t1;
			if (RayIntersect(worldPos1, viewDir, earthCenter, EARTH_RADIUS + _OC_FogHeight, t0, t1))
			{
				// In this case dist is either:
				// Distance between entry point and world position
				// Distance between entry and exit points
				// Depending on which is smallest
				float newDist = min(dist, t1) - t0;
				if (newDist < dist)
				{
					// The entry point is closer than the current fragment world position, update dist
					dist = newDist;
					// Used to calculate height falloff
					startHeight = (worldPos1 + viewDir * t0).y;
				}
			}
			else
			{
				// No intersection
				dist = 0;
			}
		}

		// Make sure distance is positive
		dist = max(dist, 0);

		// Calculate falloff parameter
		float falloff = min(max(_OC_FogHeight - (startHeight + endHeight) * 0.5, 0) * _OC_FogFalloffParams.y, 1);

		// Calculate actual fog opacity
		return 1 - saturate(exp2(-_OC_FogParams.x * dist * falloff));
	}

	float _GlobalTest;
	float _GlobalTest2;

	// Calculate height fog between two points. Also shift world position for scattering sampling.
	inline float4 HeightFog (float3 camera, float3 worldPos, float3 viewDir, float dist)
	{
		float distToCamera = dist;
		float startHeight = camera.y;
		float endHeight = worldPos.y;

		// Store this for later
		float maxDist = dist;

		// The center of the earth assuming the camera is always located at xz = 0
		float3 earthCenter = float3(0, -EARTH_RADIUS, 0);

		float minDist = 0;

		// Fix precision artifacts when camera height is really close to the fog height
		if (abs(camera.y - _OC_FogHeight) < 1)
			camera.y = _OC_FogHeight - 1;
		
		if (camera.y < _OC_FogHeight)
		{
			// Inside fog atmosphere, find exit point
			float t0, t1;
			if (RayIntersect(camera, viewDir, earthCenter, EARTH_RADIUS + _OC_FogHeight, t0, t1))
			{
				// Distance to intersection with height fog atmosphere shell
				float newDist = t0;
				if (newDist < dist)
				{
					// The exit point is closer than the current fragment world position, update dist
					dist = newDist;
					// Used to calculate height falloff
					endHeight = (camera + viewDir * t0).y;
				}
			}
			else
			{
				// No intersection. Should be impossible, but set regardless
				dist = 0;
			}
		}
		else
		{
			// Outside fog atmosphere, find entry point
			float t0, t1;
			if (RayIntersect(camera, viewDir, earthCenter, EARTH_RADIUS + _OC_FogHeight, t0, t1))
			{
				// In this case dist is either:
				// Distance between entry point and world position
				// Distance between entry and exit points
				// Depending on which is smallest
				float newDist = min(dist, t1) - t0;
				if (newDist < dist)
				{
					// The entry point is closer than the current fragment world position, update dist
					dist = newDist;
					// Any further and we'll be going out into space
					minDist = t0;
					// Used to calculate height falloff
					startHeight = (camera + viewDir * t0).y;
				}
			}
			else
			{
				// No intersection
				dist = 0;
			}
		}

		// Make sure distance is positive
		dist = max(dist, 0);

		// Calculate falloff parameter
		float falloff = min(max(_OC_FogHeight - (startHeight + endHeight) * 0.5, 0) * _OC_FogFalloffParams.y, 1);

		// Calculate actual fog opacity
		float fog = 1 - exp2(-_OC_FogParams.x * dist * falloff);

		// Maximum distance before fog is fully opaque
		// This is what creates the smooth blending between atmospheric scattering and height fog,
		// and why the scattering + extinction can just be stacked on top of the fog.
		// Essentially, the sampling of the scattering is attenuated, instead of the result
		float fParam = _OC_FogParams.x * fog * fog * fog * fog;//dist * 0.0001;
		float cameraDist = min(maxDist, max(minDist, _OC_FogBlend));

		// Shifted world position used to sample scattering
		float3 samplePos = camera + viewDir * cameraDist;

		if (distToCamera < _ProjectionParams.z)
		{
			// The lookup tables are not valid for samples below 0
			// Clamping height to 1m instead of 0 will get rid of some artifacts
			// Note that we only do this for scene fragments, as otherwise the skybox will look incorrect
			samplePos.y = max(samplePos.y, 1);
			camera.y = max(camera.y, 1);
		}

		// Sun scattering
		float4 sunColor = SunColor();

		// Get fog scattering
		float3 transmittance;
		float3 scattering = GetSkyRadianceToPoint(camera - _ScattEarthCenter, samplePos - _ScattEarthCenter, 0, -_OC_ActualSunDir, transmittance) * sunColor.rgb * sunColor.a;
		scattering = max(scattering * _OC_AtmosphereExposure, 0);
		scattering += NightScattering(camera, transmittance);
		scattering += OverCloudMie(viewDir, cameraDist);

		float4 output = 0;

		// Sun mie
		float hg = hg_schlick(dot(viewDir, -_OC_LightDir)*0.5+0.5, _OC_MieScatteringFogPhase);
		// Density
		output.a = fog * _OC_FogColor.a;

		// Color
		// output.rgb  = _OC_FogColor.rgb * hg * _OC_LightColor * _OC_FogParams.y;
		// output.rgb += _OC_FogColor.rgb * (unity_AmbientSky + _OC_LightColor) * _OC_FogParams.z;

		// Color based on atmosphere horizon lookup
		float3 fogTransmittance;
		output.rgb = GetSkyRadiance(float3(0, 0, 0) - _ScattEarthCenter, viewDir * float3(1, 0, 1), 0, -_OC_ActualSunDir, fogTransmittance) * _OC_AtmosphereExposure;
		output.rgb += NightScattering(float3(0, 0, 0), fogTransmittance);

		output.rgb += OverCloudMie(viewDir, distToCamera);

		output.rgb = _OC_FogColor.rgb * output.rgb * transmittance + scattering;

		return output;
	}

	inline float4 HeightFog (float3 camera, float3 worldPos)
	{
		// Calculate distance to fragment and view direction
		float3 viewDir 	= worldPos - camera;
		float dist 		= length(viewDir);
		viewDir 		/= dist;
		return HeightFog(camera, worldPos, viewDir, dist);
	}

	// Mix fog with the scattering mask
	void EvaluateAtmosphere (inout Atmosphere atm, float scatteringMask)
	{
		// TODO: Add more granular control?
		atm.fog.rgb *= lerp(scatteringMask, 1, _OC_FogParams.z);
		// atm.fog.rgb += _OC_FogColor.rgb * (unity_AmbientSky + 0) * _OC_FogParams.z;
		atm.scattering *= scatteringMask;
	}

	// Brunetons improved atmosphere implementation

	// Calculate fog, scattering and transmittance
	// These effects are later applied in the following way:
	// color.rgb = lerp(color.rgb * transmittance + scattering, fog.rgb, fog.a)
	Atmosphere OverCloudAtmosphere (float3 worldPos, float3 camera)
	{
		Atmosphere atm;

		float3 viewDir = worldPos - camera;
		float distToCamera = length(viewDir);
		viewDir /= distToCamera;

		// Shifted world position which enables a horizon which moves with the camera, giving the appearance of traveling around the globe
		float3 samplePos = rs2ws(worldPos);
		camera = rs2ws(camera);
		// Center xz around camera
		samplePos.xz -= camera.xz;
		camera.xz -= camera.xz;

		// First, get height fog
		atm.fog = HeightFog(camera, samplePos, viewDir, distToCamera);

		// Sun scattering
		float4 sunColor = SunColor();
		
		// Scattering lookup table does not account of camera positions below the horizon,
		// so instead we extrude the result downwards.
		camera.y = max(camera.y, 1);
		// Sample the atmospheric scattering lookup tables
		atm.scattering = GetSkyRadianceToPoint(camera - _ScattEarthCenter, samplePos - _ScattEarthCenter, 0, -_OC_ActualSunDir, atm.transmittance) * sunColor.rgb * sunColor.a;

		// Apply atmosphere exposure (for the precomputed tables)
		atm.scattering = max(atm.scattering * _OC_AtmosphereExposure, 0);

		#if !defined(OVERCLOUD_SKIP_MIE)
			// Mie scattering (not physically based)
			// The precomputed mie scattering suffers from precision issues and has been substituted for a realtime variant,
			// which is not physically based but free of artifacts
			atm.scattering += OverCloudMie(viewDir, distToCamera);
		#endif

		// Night/ambient sky (not physically based)
		atm.scattering += NightScattering(camera, atm.transmittance);

		atm.transmittance = lerp(1, atm.transmittance, _OC_AtmosphereDensity);
		atm.scattering *= _OC_AtmosphereDensity;

		return atm;
	}

	void ApplyAtmosphere (inout float3 color, Atmosphere atm)
	{
		color = lerp(color * atm.transmittance + atm.scattering, atm.fog.rgb, atm.fog.a);
	}

	// Default camera position
	Atmosphere OverCloudAtmosphere (float3 worldPos)
	{
		return OverCloudAtmosphere(worldPos, _RenderCamera);
	}

	// If you want to add shadows to the clouds
	float OverCloudSunAtten (float3 worldPos)
	{
		return 1;
	}

	// Ocean plane definition
	float3 OverCloudOcean (float3 worldPos, float3 normal)
	{
		float3 light = 0;
		light += max(dot(normal, -_OC_ActualSunDir),  0) * _OC_ActualSunColor;
		light += max(dot(normal, -_OC_ActualMoonDir), 0) * _OC_ActualMoonColor;
		return _OC_EarthColor * light;
	}

#endif // ATMOSPHERE_INCLUDED