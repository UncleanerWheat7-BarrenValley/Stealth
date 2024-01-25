Shader "Hidden/OverCloud/SeparableBlur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Pass // 0, Non depth-aware, vertical pass
		{
			name "7TapVertical"

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment Vertical

				#define VERTICAL

				#include "SeparableBlur.cginc"
			ENDCG
		}

		Pass // 1, Non depth-aware, horizontal pass
		{
			name "7TapHorizontal"

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment Horizontal

				#define HORIZONTAL

				#include "SeparableBlur.cginc"
			ENDCG
		}

		Pass // 2, Depth aware, vertical pass
		{
			name "7TapVertical_Depth"

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment Vertical

				#define DEPTH_AWARE

				#define VERTICAL

				#include "SeparableBlur.cginc"
			ENDCG
		}

		Pass // 3, Depth aware, horizontal pass
		{
			name "7TapHorizontal_Depth"

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment Horizontal

				#define DEPTH_AWARE

				#define HORIZONTAL

				#include "SeparableBlur.cginc"
			ENDCG
		}

		Pass // 4, Compositor blur, vertical pass
		{
			name "7TapVertical_Compositor"

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment Vertical

				#define COMPOSITOR

				#define VERTICAL

				#include "SeparableBlur.cginc"
			ENDCG
		}

		Pass // 5, Compositor blur, horizontal pass
		{
			name "7TapHorizontal_Compositor"

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment Horizontal

				#define COMPOSITOR

				#define HORIZONTAL

				#include "SeparableBlur.cginc"
			ENDCG
		}
	}
}