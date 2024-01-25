Shader "OverCloud/ExampleContent/Lightning"
{
	Properties
	{
		_MainTex 		("Texture Sheet", 	2D) 			= "white" {}
		_FlowTex 		("Flow Map", 		2D) 			= "white" {}
		_Color 			("Color", 			Color) 			= (1, 1, 1, 1)
		_Intensity		("Intensity",		Float)			= 4
		_TexWidth		("Width",			Int)			= 4
		_TexIndex		("Index",			Int)			= 0
		_Phase			("Phase",			Range(0, 1))	= 0
		_PhaseAsymmetry	("Phase Asymmetry",	Range(0, 1))	= 0.25
		_CloudBlend		("Cloud Blend",		Range(0, 0.01))	= 0.001
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		Blend One One
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "../../OverCloudInclude.cginc"

			struct v2f
			{
				float4 vertex 		: SV_POSITION;
				float2 texcoord		: TEXCOORD0;
				float3 worldPos		: TEXCOORD1;
				float4 screenPos	: TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D 	_MainTex;
			sampler2D	_FlowTex;
			fixed4		_Color;
			float		_Intensity;
			int			_TexWidth;
			int 		_TexIndex;
			float		_Phase;
			float		_PhaseAsymmetry;
			float		_CloudBlend;

			v2f vert (appdata_full v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex 		= UnityObjectToClipPos(v.vertex);
				o.worldPos  	= mul(unity_ObjectToWorld, v.vertex);
				o.screenPos 	= ComputeScreenPos(o.vertex);
				COMPUTE_EYEDEPTH(o.screenPos.z);
				o.texcoord 		= v.texcoord;
				o.texcoord.x 	= (o.texcoord.x + _TexIndex) / _TexWidth;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				float2 screenUV = i.screenPos.xy / i.screenPos.w;

				float4 clouds = tex2D(_OverCloudTex, screenUV);

				fixed4 flow = tex2D(_FlowTex, i.texcoord);
				flow.rg = flow.rg * 2 - 1;

				float p = _Phase / _PhaseAsymmetry;

				float disperse = max(_Phase - _PhaseAsymmetry, 0);

				float2 offset = flow.rg * disperse * 0.2;
				fixed4 tex = tex2D(_MainTex, i.texcoord + offset);
				float t = p > tex.g;
				t *= disperse < flow.b;
				fixed3 color = tex.r * t * _Color * _Intensity;

				color.rgb *= CloudAttenuation(length(i.worldPos - _WorldSpaceCameraPos), screenUV, _CloudBlend);

				return fixed4(color, 1);
			}
			ENDCG
		}
	}
}