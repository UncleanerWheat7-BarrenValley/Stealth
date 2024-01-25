///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

#ifndef OVERCLOUDFOGLIGHT_INCLUDED
#define OVERCLOUDFOGLIGHT_INCLUDED

	#include "UnityCG.cginc"
	#include "Atmosphere.cginc"

	struct v2f
	{
		float4 vertex 		: SV_POSITION;
		float3 worldPos		: TEXCOORD0;
		float4 screenPos	: TEXCOORD1;
		float  atten		: TEXCOORD2;

		UNITY_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_OUTPUT_STEREO
	};

	sampler2D	_FalloffTex;
	samplerCUBE	_Cookie;
	float4x4	_WorldToLocal;
	float4 		_Color;
	float3 		_Center;
	float4		_Params;
	float4		_Params2;
	float3		_SpotParams;
	float3		_SpotParams2;
	float2		_RaymarchSteps;

	#define _Radius 	_Params.x
	#define _InvRadius 	_Params.y
	#define _Intensity 	_Params.z
	#define _Atten	 	_Params.w
	#define _MinDensity	_Params2.x

	v2f vert (appdata_full v)
	{
		UNITY_SETUP_INSTANCE_ID(v);
		v2f o;
		UNITY_INITIALIZE_OUTPUT(v2f, o);
		UNITY_TRANSFER_INSTANCE_ID(v, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.vertex 	= UnityObjectToClipPos(v.vertex);
		o.screenPos = ComputeScreenPos(o.vertex);
		o.worldPos	= mul(unity_ObjectToWorld, v.vertex);
		// The attenuation of the light itself (calculated for the center)
		o.atten 	= 1-FogAttenuation(_WorldSpaceCameraPos, lerp(_WorldSpaceCameraPos, _Center, _Atten));

		return o;
	}

	sampler2D _CameraDepthTexture;

	// Specialized version for fog lights which fix a minor rendering issue
	float FogAttenuation2 (float3 worldPos1, float3 worldPos2)
	{
		// Calculate distance to fragment and view direction
		float3 viewDir 	= worldPos2 - worldPos1;
		float dist 		= length(viewDir);
		viewDir 		/= dist;

		float startHeight = worldPos1.y;
		float endHeight = worldPos2.y;

		// The center of the earth assuming the camera is always located at xz = 0
		float3 earthCenter = float3(0, -EARTH_RADIUS + _OverCloudOriginOffset.y, 0);

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
				if (newDist < dist && viewDir.y > 0)
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

		// if (dist < 100)
		// dist = 500000;

		// Calculate falloff parameter
		float falloff = min(max(_OC_FogHeight - (startHeight + endHeight) * 0.5, 0) * _OC_FogFalloffParams.y, 1);
		falloff = 1;

		// Calculate actual fog opacity
		return 1 - saturate(exp2(-max(_OC_FogParams.x, _MinDensity) * dist * falloff));
	}

	float4 _PixelSize;
	float4 _PixelSizeDS;
	sampler2D _CameraDepthLowRes;

	// Simplified version which does not account for the sign of the ray hits
	bool RayIntersectSimple
	(
		// Ray
		float3 RO, // Origin
		float3 RD, // Direction

		// Sphere
		float3 SC, // Centre
		float SR, // Radius
		out float AO, // First intersection time
		out float BO  // Second intersection time
	)
	{
		float3 L = SC - RO;
		float DT = dot (L, RD);
		float R2 = SR * SR;

		float CT2 = dot(L, L) - DT*DT;

		// Intersection point outside the sphere
		if (CT2 > R2)
			return false;

		float AT = sqrt(R2 - CT2);
		float BT = AT;

		AO = DT - AT;
		BO = DT + BT;
		return true;
	}

	static const float4 bayer[4] = {
		float4(0.0f, 0.5f, 0.125f, 0.625f),
		float4( 0.75f, 0.22f, 0.875f, 0.375f),
		float4( 0.1875f, 0.6875f, 0.0625f, 0.5625),
		float4( 0.9375f, 0.4375f, 0.8125f, 0.3125)
	};

	fixed4 _frag (v2f i)
	{
		// return 1;

		UNITY_SETUP_INSTANCE_ID(i);

		float2 screenUV = i.screenPos.xy / i.screenPos.w;
		// float n = BlueNoise(screenUV, _PixelSizeDS.xy);
		float2 bayerUV = floor(frac(screenUV * _PixelSizeDS.xy / 4) * 4);
		float n = bayer[bayerUV.x][bayerUV.y];

		float3 viewDir = i.worldPos - _WorldSpaceCameraPos;
		float distToCamera = length(viewDir);
		viewDir /= distToCamera;
		// Linear eye depth
		float sceneDepth = LinearEyeDepth(tex2D(_CameraDepthLowRes, screenUV));
		// Linear eye depth -> world space distance
		float3 camFwd = UNITY_MATRIX_V[2].xyz;
		sceneDepth *= 1 / dot(-viewDir, camFwd);
		// We also sample the cloud depth buffer to enable proper blending with the cloud volume
		float cloudDepth = tex2D(_OverCloudDepthTex, screenUV);
		// Pick whichever one is closest
		sceneDepth = min(sceneDepth, cloudDepth);

		float3 color = 0;
		fixed dist = 0;
		fixed dist2 = 0;

		// Calculate intersection with the "fog sphere"
		float a, b;
		if (RayIntersectSimple(_WorldSpaceCameraPos, viewDir, _Center, _Radius, a, b))
		{
			// Check if first intersection is closer than the scene depth (otherwise something is in front of the sphere)
			if (a < sceneDepth)
			{
				a = max(a, 0);
				b = min(b, sceneDepth);

				float width = abs(b - a);

				float3 camera = _WorldSpaceCameraPos;
				if (abs(camera.y - _OC_FogHeight) < 1)
					camera.y = _OC_FogHeight - 1;
				
				UNITY_LOOP
				for (int u = 0; u < _RaymarchSteps.x; u++)
				{
					float3 t = camera + lerp(a, b, (u+n) * _RaymarchSteps.y) * viewDir;
					float3 dir = t - _Center;
					float len = length(dir);
					dir /= len;
					float3 atten = 1;
					#ifdef COOKIE
						atten *= texCUBE(_Cookie, mul(_WorldToLocal, float4(dir, 0)));
					#endif
					#ifdef SPOTLIGHT
						float spot = max(dot(dir, _SpotParams), 0);
						spot = max(spot - _SpotParams2.x, 0) / (1-_SpotParams2.x);
						atten *= spot;
					#endif
					color += atten;

					float falloff = 1 - len * _InvRadius;
					dist  += width * falloff * falloff * (t.y < _OC_FogHeight);
					dist2 += width * falloff * falloff;
				}
				color *= width * _InvRadius * _RaymarchSteps.y;
				dist  *= width * _InvRadius * _RaymarchSteps.y;
				dist2 *= width * _InvRadius * _RaymarchSteps.y;
			}
		}	

		float fog = 1 - saturate(exp2(-max(dist * _OC_FogParams.x, dist2 * _MinDensity)));
		
		return float4(color * _Color.rgb * fog * i.atten * _Intensity, 1);
	}

#endif // OVERCLOUDFOGLIGHT_INCLUDED