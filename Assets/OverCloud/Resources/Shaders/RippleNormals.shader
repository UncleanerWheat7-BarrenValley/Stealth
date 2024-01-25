Shader "Hidden/OverCloud/RippleNormals"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TimeScale ("Timescale", Float) = 1
		_Intensity ("Intensity", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "OverCloudCore.cginc"

			#define M_PI 3.14159265f

			struct v2f
			{
				float4 vertex 		: SV_POSITION;
				float2 texcoord		: TEXCOORD0;
			};

			sampler2D 	_MainTex;
			float		_TimeScale;
			float		_Intensity;

			v2f vert (appdata_full v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.vertex 		= UnityObjectToClipPos(v.vertex);
				o.texcoord 		= v.texcoord;

				return o;
			}

			// https://seblagarde.wordpress.com/2013/01/03/water-drop-2b-dynamic-rain-and-its-effects/
			float3 Ripple(float2 uv, float time, float weight)
			{
				float4 ripple = tex2D(_MainTex, uv);
				ripple.yz = ripple.yz * 2 - 1;

				 // Apply time shift
				float dropFrac = frac(ripple.w + time);
				float timeFrac = dropFrac - 1.0f + ripple.x;
				float dropFactor = saturate(0.2f + weight * 0.8f - dropFrac);
				float finalFactor = dropFactor * ripple.x * 
				                    sin( clamp(timeFrac * 9.0f, 0.0f, 3.0f) * M_PI);

				return float3(ripple.yz * finalFactor * 0.35f, 1.0f);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.texcoord;

				float4 timeMul = float4(1.0f, 0.85f, 0.93f, 1.13f); 
				float4 timeAdd = float4(0.0f, 0.2f, 0.45f, 0.7f);
				float4 times = (_Time.x * _OC_GlobalRainParams2.x * timeMul + timeAdd);
				times = frac(times);

				// Layer weights
				float4 weight = _Intensity - float4(0, 0.25, 0.5, 0.75);
				weight = saturate(weight * 4);

				// Generate four shifted layers
				float3 ripple0 = Ripple(uv + float2( 0.25f,0.0f), times.x, weight.x);
				float3 ripple1 = Ripple(uv + float2(-0.55f,0.3f), times.y, weight.y);
				float3 ripple2 = Ripple(uv + float2(0.6f, 0.85f), times.z, weight.z);
				float3 ripple3 = Ripple(uv + float2(0.5f,-0.75f), times.w, weight.w);
				
				// Blend
				float4 Z = lerp(1, float4(ripple0.z, ripple1.z, ripple2.z, ripple3.z), weight);
				float3 tangentNormal = float3( weight.x * ripple0.xy +
				                        weight.y * ripple1.xy + 
				                        weight.z * ripple2.xy + 
				                        weight.w * ripple3.xy, 
				                        Z.x * Z.y * Z.z * Z.w);
                           
				return float4(normalize(tangentNormal)*0.5+0.5, 1);
			}
			ENDCG
		}
	}
}