///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

#ifndef SKYBOX_INCLUDED
#define SKYBOX_INCLUDED

	#include "Atmosphere.cginc"

	bool IntersectsEarth (float3 viewDir)
	{
		float t0, t1;
		return RayIntersect(_WorldCamera, viewDir, EARTH_CENTER, EARTH_RADIUS, t0, t1);
	}

	bool OverCloudEarth (float3 viewDir, out float3 worldPos, out float3 color)
	{
		worldPos 	= float3(0, 0, 0);
		color 		= float3(0, 0, 0);
		float t0, t1;
		UNITY_BRANCH
		if (RayIntersect(_WorldCamera, viewDir, EARTH_CENTER, EARTH_RADIUS, t0, t1) && viewDir.y < 0)
		{
			worldPos = _WorldCamera + viewDir * t0;
			// Hack to fix precision issues when sampling the atmosphere
			worldPos.y += 1;
			float3 earthNormal = normalize(worldPos - EARTH_CENTER);
			float3 oceanAlbedo = float3(0.3, 0.4, 0.5);
			float3 ocean = OverCloudOcean(worldPos, earthNormal);

			// Scattering is applied in the skybox shader
			color = ocean;
			return true;
		}

		return false;
	}

	float4 OverCloudSun (float3 viewDir, out float3 glow)
	{
		// Calculate glow effect
		// float viewDot = dot(-viewDir, _OC_ActualSunDir)*0.5+0.5;
		// glow = hg_schlick(viewDot, 0.93) * _OC_ActualSunColor * 0.04;
		glow = 0;

		// Ray trace against sun sphere
		float3 sunCenter = -_OC_ActualSunDir * SUN_DIST;
		float AO, BO;
		UNITY_BRANCH
		if (RayIntersect(float3(0, 0, 0), viewDir, sunCenter, SUN_RADIUS * _SkySunSize, AO, BO)
			&& dot(viewDir, -_OC_ActualSunDir) > 0) // Avoid false result from negative sun direction (RayIntersect picks this up)
		{
			float3 intersectionPos = viewDir * AO;
			float3 sunNormal = normalize(intersectionPos - sunCenter);

			float sunDot = dot(sunNormal, _OC_ActualSunDir)*0.5+0.5;

			// Solar limb darkening
			float3 sunColor = lerp(float3(0.22, 0.15, 0.08), float3(1, 1, 1), sunDot * sunDot);

			return float4(sunColor * _OC_ActualSunColor.rgb * _SkySunIntensity, 1);
		}

		return float4(0, 0, 0, 0);
	}

	float3 OverCloudMoonGlow (float3 viewDir)
	{
		float4 moonColor = MoonColor();
		float sunDot = dot(-viewDir, _OC_ActualMoonDir)*0.5+0.5;
		return hg_schlick(sunDot, _SkyMoonGlowG) * moonColor.rgb * moonColor.a * _SkyMoonGlowIntensity;
	}

	float4 OverCloudMoon (float3 viewDir, out float3 glow)
	{
		float4 moonColor = MoonColor();
		// Calculate glow effect
		// float viewDot = dot(-viewDir, _OC_ActualMoonDir)*0.5+0.5;
		// glow = hg_schlick(viewDot, 0.93) * _OC_CurrentMoonColor * 0.04;
		glow = 0;
		// Ray trace against moon sphere
		float3 moonCenter = -_OC_ActualMoonDir * MOON_DIST;
		float AO, BO;
		UNITY_BRANCH
		if (RayIntersect(float3(0, 0, 0), viewDir, moonCenter, MOON_RADIUS * _SkyMoonSize, AO, BO)
			&& dot(viewDir, -_OC_ActualMoonDir) > 0) // Avoid false result from negative moon direction (RayIntersect picks this up)
		{
			float3 intersectionPos = viewDir * AO;
			float3 moonNormal = normalize(intersectionPos - moonCenter);
			float atten = 1;
			if (RayIntersect(intersectionPos, _OC_ActualSunDir, 0, EARTH_RADIUS * 1.1, AO, BO) && dot(_OC_ActualSunDir, -_OC_ActualMoonDir) > 0) // Avoid false result from negative moon direction (RayIntersect picks this up)
			{
				atten = _SkyLunarEclipse.a;
			}

			// Tweaked lambertian model
			float moonShade = 1 - max(dot(-_OC_ActualSunDir, moonNormal), 0);
			moonShade = moonShade * moonShade * moonShade;
			moonShade = (1 - moonShade) * atten * 0.75;

			float3 moonAlbedo = texCUBE(_SkyMoonCubemap, moonNormal).rgb;

			return float4(moonAlbedo * moonShade * moonColor.rgb * _SkyMoonIntensity, 1);
		}

		return float4(0, 0, 0, 0);
	}

	float CirrusAlpha (sampler2D tex, float2 uv, float detailScale, float opacity, float timeScale)
	{
		float alpha = tex2D(tex, uv + float2(_OC_GlobalWindTime, 0) * 0.00001 * timeScale).r;
		float alpha2 = tex2D(tex, uv * detailScale + float2(_OC_GlobalWindTime, 0) * 0.0003 * timeScale).g;

		alpha = alpha * lerp(alpha2, 1, alpha);

		return saturate(alpha - (1-opacity));
	}

	float4 OverCloudCirrus (
		sampler2D tex,
		float4 color,
		float scale,
		float detailScale,
		float height,
		float opacity,
		float timeScale,
		float lightPenetration,
		float lightAbsorption,
		float3 viewDir,
		float sceneDistance,
		float scatteringMask,
		bool isSky,
		bool aboveCloudPlane)
	{
		if (_WorldCamera.y < height && viewDir.y < 0 && IntersectsEarth(viewDir)) // TODO: Earth check unnecessary?
			return 0;

		float t0, t1; 
		UNITY_BRANCH
		if (RayIntersect(_WorldCamera, viewDir, EARTH_CENTER, EARTH_RADIUS + height, t0, t1))
		{
			UNITY_BRANCH
			if (sceneDistance < t0 && !isSky)
				return 0;

			float3 worldPos = _WorldCamera + viewDir * t0;
			float3 normal = normalize(worldPos - EARTH_CENTER);

			float alpha = CirrusAlpha(tex, worldPos.xz * scale, detailScale, opacity, timeScale) * color.a;
			float alpha2 = 0;
			[unroll(4)]
			for (int i = 0; i < 4; i++)
			{
				float direction = aboveCloudPlane ? -1 : 1;

				float a = CirrusAlpha(tex, worldPos.xz * scale - _OC_LightDir.xz * direction * lightPenetration * 0.1 * i / 4, detailScale, opacity, timeScale);
				
				if (aboveCloudPlane)
					alpha2 += 1-a;
				else
					alpha2 += a;
			}
			alpha2 = alpha2 / 4 * color.a;

			fixed3 cirrus = 0;
			cirrus += CloudAmbient(color.rgb, 0);
			cirrus += CloudLighting
			(
				color.rgb,
				alpha,
				alpha2 * lightAbsorption * 100,
				viewDir,
				_OC_LightDir,
				_OC_LightColor,
				0
			);
			
			float4 fog;
			float3 scattering;
			float3 extinction;
			Atmosphere atm = OverCloudAtmosphere(ws2rs(worldPos));
			EvaluateAtmosphere(atm, scatteringMask);

			ApplyAtmosphere(cirrus, atm);

			// Depth fade
			if (sceneDistance < _ProjectionParams.z * 0.5)
			alpha *= min((sceneDistance - t0) * 0.001, 1);

			// Camera near fade
			alpha *= min((t0) * 0.001, 1);

			return max(float4(cirrus, alpha), 0);
		}

		return 0;
	}

	float4 OverCloudSky (float3 worldPos, float2 screenUV)
	{
		// Do all work in true world space
		worldPos = rs2ws(worldPos);
		float3 camera = _WorldCamera;

		float3 viewDir = worldPos - camera;
		float distToCamera = length(viewDir);
		viewDir /= distToCamera;

		#if defined(ENDLESS_HORIZON)
			// worldPos.xz -= camera.xz;
			camera.xz -= camera.xz;
		#endif

		float3 transmittance;
		float3 color = 0;

		float3 earthPos;
		float3 earthColor;
		bool earth = OverCloudEarth(viewDir, earthPos, earthColor);

		// Sun scattering
		float4 sunColor = SunColor();
		color += GetSkyRadiance(camera - _ScattEarthCenter, viewDir, 0, -_OC_ActualSunDir, transmittance) * sunColor.rgb * sunColor.a;

		float scatteringMask = OverCloudScatteringMask(screenUV);
		color *= lerp(scatteringMask, 1, 0.25); // TODO: Could make this a parameter?

		// Moon scattering
		float4 nightColor = NightColor();

		color *= _OC_AtmosphereExposure;

		// Add moon
		float3 moonGlow;
		float4 moon = OverCloudMoon(viewDir, moonGlow);
		color = lerp(color, color + transmittance * moon.rgb, moon.a);

		// Add sun
		float3 sunGlow;
		float4 sun = OverCloudSun(viewDir, sunGlow);
		color = lerp(color, color + transmittance * sun.rgb, sun.a * (1-moon.a)) + sunGlow * (1-moon.a) * transmittance;

		// Night/ambient sky (not physically based)
		UNITY_BRANCH
		if (!earth)
			color += NightScattering(camera, transmittance);

		// Apply star/space map
		color += transmittance * texCUBE(_SkyStarsCubemap, viewDir) * _SkyStarsIntensity * (1-moon.a) * (1-sun.a);

		worldPos = earth ? earthPos : worldPos;

		#if defined(GROUND_PLANE)
		if (earth)
			color = earthColor;
		#endif

		float fogHeight = _OC_FogHeight;
		float dist		= max(camera.y - (fogHeight - _OC_FogFalloffParams.x), 0);
		float falloff	= 1-min(dist * _OC_FogFalloffParams.y, 1);
		
		Atmosphere atm = OverCloudAtmosphere(ws2rs(worldPos));
		
		EvaluateAtmosphere(atm, scatteringMask);

		// Need to add mie scattering here since we are not using the result from OverCloudAtmosphere
		color.rgb += OverCloudMie(viewDir, distToCamera) * scatteringMask;

		#if defined(GROUND_PLANE)
		if (earth)
			ApplyAtmosphere(color.rgb, atm);
		else
		#endif
			color = lerp(color, atm.fog.rgb, atm.fog.a);

		return float4(color, 1);
	}
#endif // SKYBOX_INCLUDED