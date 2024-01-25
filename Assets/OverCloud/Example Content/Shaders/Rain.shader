Shader "Particles/Rain"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			// This is necessary if you want to sample the volumetric wetness mask
			#pragma multi_compile __ RAIN_MASK_ENABLED

			#include "UnityCG.cginc"
			#include "../../OverCloudInclude.cginc"

			struct v2f
			{
				float4 vertex 	: SV_POSITION;
				float4 color 	: COLOR;
				float2 texcoord	: TEXCOORD0;
				float3 worldPos	: TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D 	_MainTex;
			fixed4		_Color;

			v2f vert (appdata_full v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex 	= UnityObjectToClipPos(v.vertex);
				o.color 	= v.color;
				o.worldPos  = mul(unity_ObjectToWorld, v.vertex);
				o.texcoord 	= v.texcoord;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos);
				float viewDot = dot(-viewDir, _OC_LightDir)*0.5+0.5;
				float3 light = hg_schlick(viewDot, 0.7) * _OC_LightColor;
				light += unity_AmbientSky;

				fixed4 albedo = tex2D(_MainTex, i.texcoord);

				fixed4 color = albedo * i.color * _Color * float4(light, 1);

				// Apply volumetric wetness mask
				float wetness = RainSurface(i.worldPos, float3(0, 1, 0));
				color.a *= wetness;
				
				return color;
			}
			ENDCG
		}
	}
}