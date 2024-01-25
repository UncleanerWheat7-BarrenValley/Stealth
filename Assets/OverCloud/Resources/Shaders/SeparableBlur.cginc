#ifndef SEPARABLEBLUR_INCLUDED
#define SEPARABLEBLUR_INCLUDED

	#include "UnityCG.cginc"

	struct v2f
	{
		float4 vertex 		: SV_POSITION;
		float2 texcoord		: TEXCOORD0;
		float4 screenPos	: TEXCOORD1;

		UNITY_VERTEX_OUTPUT_STEREO
	};

	sampler2D	_CameraDepthLowRes;
	sampler2D 	_MainTex;
	float2		_PixelSize;
	float		_DepthThreshold;
	float		_BlurAmount;

	v2f vert (appdata_full v)
	{
		v2f o;
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.vertex 	= UnityObjectToClipPos(v.vertex);
		o.texcoord 	= v.texcoord;
		o.screenPos = ComputeScreenPos(o.vertex);

		return o;
	}

	static const fixed kernel[8] = { 0.14446445f, 0.13543542f, 0.11153505f, 0.08055309f, 0.05087564f, 0.02798160f, 0.01332457f, 0.00545096f };

	float4 BlurPass (v2f i, fixed2 blurDirection, float blurAmount)
	{
		#if defined(DEPTH_AWARE) && UNITY_SINGLE_PASS_STEREO
			fixed2 uv = i.screenPos.xy / i.screenPos.w;
		#else
			fixed2 uv = i.texcoord;
		#endif

		// Center tap
		#if defined(DEPTH_AWARE)
			fixed centerDepth = Linear01Depth(tex2D(_CameraDepthLowRes, uv));
			float depthFalloff = lerp(2000, 500, centerDepth);
		#endif
		float4 color = tex2D(_MainTex, uv) * kernel[0];
		float  accumWeights = kernel[0];

		float sigma_r = 1;
		float g_BlurSigmaRcp2 = -1 / (2 * sigma_r*sigma_r);

		for (int u = 1; u < 8; u++)
		{
			float2 uv0 = uv + blurDirection * _PixelSize * u * blurAmount;
			float2 uv1 = uv - blurDirection * _PixelSize * u * blurAmount;

			#if defined(DEPTH_AWARE)
				fixed tapDepth = Linear01Depth(tex2D(_CameraDepthLowRes, uv0));
				float depthDiff = abs(centerDepth - tapDepth);
				float weight = depthDiff * depthFalloff;
				weight = exp(-weight * weight);
				weight = kernel[u] * weight;
				color += tex2D(_MainTex, uv0) * weight;
				accumWeights += weight;

				tapDepth = Linear01Depth(tex2D(_CameraDepthLowRes, uv1));
				depthDiff = abs(centerDepth - tapDepth);
				weight = depthDiff * depthFalloff;
				weight = exp(-weight * weight);
				weight = kernel[u] * weight;
				color += tex2D(_MainTex, uv1) * weight;
				accumWeights += weight;
			#else
				color += tex2D(_MainTex, uv0) * kernel[u];
				color += tex2D(_MainTex, uv1) * kernel[u];
				accumWeights += kernel[u] * 2;
			#endif
		}
		
		// return tex2D(_MainTex, uv);
		return color / accumWeights;
	}

	float4 Vertical (v2f i) : SV_Target
	{
		float4 color = BlurPass(i, fixed2(0, 1), _BlurAmount);

		#if defined(COMPOSITOR)
			// AO should be fully blurred
			color.b = BlurPass(i, fixed2(0, 1), 1.5).b;
		#endif

		return color;
	}

	float4 Horizontal (v2f i) : SV_Target
	{
		float4 color = BlurPass(i, fixed2(1, 0), _BlurAmount);

		#if defined(COMPOSITOR)
			// AO should be fully blurred
			color.b = BlurPass(i, fixed2(1, 0), 1.5).b;
			// Also, horizontal pass is done after vertical,
			// so we take this opportunity to fade the edges of the texture
			float fade = min(length(i.texcoord - float2(0.5, 0.5)) * 2, 1);
			fade = 1 - fade  * fade;
			color.b *= fade;
		#endif

		return color;
	}
#endif // SEPARABLEBLUR_INCLUDED