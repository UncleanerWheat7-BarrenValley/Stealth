///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

Shader "OverCloud/FogLight"
{
	Properties
	{
		_Atten ("Attenuation Factor", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Overlay" }
		Cull Front
		ZTest Always
		ZWrite Off
		// Blend SrcAlpha OneMinusSrcAlpha
		Blend One One

		Pass
		{
			// Pass 0, point light

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "FogLight.cginc"

			fixed4 frag (v2f i) : SV_Target
			{
				return _frag(i);
			}

			ENDCG
		}

		Pass
		{
			// Pass 2, spot light

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#define SPOTLIGHT

			#include "FogLight.cginc"

			fixed4 frag (v2f i) : SV_Target
			{
				return _frag(i);
			}

			ENDCG
		}
	}
}