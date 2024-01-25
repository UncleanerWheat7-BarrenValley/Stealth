Shader "Hidden/ScreenDroplets"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../../../../OverCloudInclude.cginc"

			struct v2f
			{
				float4 vertex 		: SV_POSITION;
				float2 texcoord		: TEXCOORD0;
				float4 screenPos	: TEXCOORD1;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D 	_MainTex;
			sampler2D 	_MainTexBlurred;
			sampler2D	_BlurMask;
			float4		_MainTex_TexelSize;
			float2		_PixelSize;
			float		_Intensity;

			v2f vert (appdata_full v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex 	= UnityObjectToClipPos(v.vertex);
				o.texcoord 	= v.texcoord;
				o.screenPos = ComputeScreenPos(o.vertex);

				return o;
			}

			float Droplet (float2 uv)
			{
				float aspect = 10.0 / 16.0;
				uv.y = (uv.y - 0.5) * aspect + 0.5;

				float dist = length(uv - float2(0.5, 0.5)) * 2;
				/*
				float period = dot(normalize(uv - float2(0.5, 0.5)), float2(0, 1))*0.5+0.5;
				if (uv.x < 0.5)
					period = period*0.5;
				else
					period = 1 - period*0.5;
				float2 uv2 = float2(period * 0.75, pow(dist, 0.1) * 1 - _Time.x * 0.025);
				return tex2D(_BlurMask, uv2) * _Intensity;
				*/

				float d = 0;
				d += tex2D(_BlurMask, uv);

				return d * _Intensity * dist;

				float n = tex3D(_OC_3DNoiseTex, float3(uv, _Time.x));

				return smoothstep(0.4, 0.5, n);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				#if UNITY_SINGLE_PASS_STEREO
					float2 uv = i.screenPos.xy / i.screenPos.w;
				#else
					float2 uv = i.texcoord;
				#endif

				// if (uv.x > 0.5)
				// return tex2D(_OC_RainMask, uv) * 0.001;

				// Use the 3D noise texture to generate some droplet normals
				float droplet = Droplet(uv);
				// return droplet;

				float offset = 32;
				float2 xOffset = float2(_PixelSize.x * offset, 0);
				float2 yOffset = float2(0, _PixelSize.y * offset);
				float2 normal = 0;
				normal.x += (Droplet(uv-xOffset) - droplet) - (Droplet(uv+xOffset) - droplet);
				normal.y += (Droplet(uv-yOffset) - droplet) - (Droplet(uv+yOffset) - droplet);

				uv += normal * droplet * 0.2;

				// return float4(normal*0.5+0.5, 0, 1);

				uv = (uv - 0.5) * (1-droplet*0.1) + 0.5;

				fixed3 blur = tex2D(_MainTexBlurred, uv);
				float3 color = tex2D(_MainTex, uv).rgb;

				color = lerp(color, blur, droplet);

				color = max(color, 0);
				
				return float4(color, 1);
			}
			ENDCG
		}
	}
}