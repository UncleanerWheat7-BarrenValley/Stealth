#ifndef BICUBIC_LIB_INCLUDED
#define BICUBIC_LIB_INCLUDED

	float4 Cubic (float v)
	{
		float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
		float4 s = n * n * n;
		float x = s.x;
		float y = s.y - 4.0 * s.x;
		float z = s.z - 4.0 * s.y + 6.0 * s.x;
		float w = 6.0 - x - y - z;
		return float4(x, y, z, w) * (1.0/6.0);
	}

	float4 tex2DBicubic (sampler2D tex, float2 texCoords, float2 texSize, float2 texInvSize)
	{
		texCoords = texCoords * texSize - 0.5;

		float2 fxy = frac(texCoords);
		texCoords -= fxy;

		float4 xcubic = Cubic(fxy.x);
		float4 ycubic = Cubic(fxy.y);

		float4 c = texCoords.xxyy + float2(-0.5, +1.5).xyxy;

		float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
		float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;

		offset *= texInvSize.xxyy;

		float4 sample0 = tex2Dlod(tex, float4(offset.xz, 0, 0));
		float4 sample1 = tex2Dlod(tex, float4(offset.yz, 0, 0));
		float4 sample2 = tex2Dlod(tex, float4(offset.xw, 0, 0));
		float4 sample3 = tex2Dlod(tex, float4(offset.yw, 0, 0));

		float sx = s.x / (s.x + s.y);
		float sy = s.z / (s.z + s.w);

		return lerp( lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
	}

#endif