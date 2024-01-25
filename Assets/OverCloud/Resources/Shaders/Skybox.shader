// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "OverCloud/Skybox" 
{
	SubShader 
	{
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass 
		{	
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog

			#pragma multi_compile __ OVERCLOUD_ENABLED
			#pragma multi_compile __ OVERCLOUD_ATMOSPHERE_ENABLED

			#define GROUND_PLANE
			#define ENDLESS_HORIZON

			#include "UnityCG.cginc"
			#include "Skybox.cginc"

			struct v2f 
			{
				float4  pos			: SV_POSITION;
				float4  worldPos	: TEXCOORD0;
				float4  screenPos	: TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos 		= UnityObjectToClipPos(v.vertex);
				o.worldPos 	= mul(unity_ObjectToWorld, v.vertex);
				o.screenPos = ComputeScreenPos(o.pos);

				return o;
			}
			
			float4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				#if OVERCLOUD_ENABLED && OVERCLOUD_ATMOSPHERE_ENABLED
					// If OverCloud is enabled for this camera, the skybox is drawn in the Atmosphere shader
					// (if Atmosphere is checked in OverCloudCamera settings)
					return 0;
				#else
					float4 color = OverCloudSky(i.worldPos, i.screenPos);

					return float4(color.rgb, 1);
				#endif
			}			
			ENDCG
		}
	}

}