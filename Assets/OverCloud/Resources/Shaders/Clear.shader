Shader "Hidden/OverCloud/Clear"
{
	// Properties
	// {
	// 	_Color ("Clear Color", Color) = (0, 0, 0, 0)
	// }
	SubShader
	{
		// Pass 0, clear color
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			float4 _ClearColor;

			float4 frag(v2f_img i) : COLOR
			{
				return _ClearColor;
			}
			ENDCG
		}

		// Pass 1, clear depth
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			float4 frag(v2f_img i) : COLOR
			{
				return float4(10000, 0, 0, 0);
			}
			ENDCG
		}
	}
}