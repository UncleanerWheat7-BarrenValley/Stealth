///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

Shader "Hidden/OverCloud/Compositor"
{
	Properties
	{
		
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
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "OverCloudCore.cginc" 

			struct v2f
			{
				float4 vertex 	: SV_POSITION;
				float2 texcoord	: TEXCOORD0;
				float3 worldPos : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata_full v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex 	= UnityObjectToClipPos(v.vertex);
				o.texcoord 	= v.texcoord;

				o.worldPos.x = lerp(_OC_CloudWorldExtentsMinMax.x, _OC_CloudWorldExtentsMinMax.z, o.texcoord.x);
				o.worldPos.z = lerp(_OC_CloudWorldExtentsMinMax.y, _OC_CloudWorldExtentsMinMax.w, o.texcoord.y);
				o.worldPos.y = 0;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				float4 color = 0;

				float density = CloudDensity(i.worldPos);

				color.rgb = density;
				color.a = 1;
				
				return color;
			}
			ENDCG
		}
	}
}