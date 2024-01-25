using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC.ExampleContent
{
	[ExecuteInEditMode]
	#if UNITY_5_4_OR_NEWER
		[ImageEffectAllowedInSceneView]
	#endif
	public class ScreenDroplets : MonoBehaviour
	{
		[SerializeField]
		OverCloudProbe overCloudProbe	 = null;
		[SerializeField]
		Shader		shader				 = null;
		[SerializeField]
		Shader		blurShader			 = null;
		[SerializeField]
		float		blurAmount			= 1;
		[SerializeField]
		Texture2D	blurMask			 = null;

		Material	_material;
		Material	material
		{
			get
			{
				if (!_material)
					_material = new Material(shader);
				return _material;
			}
		}

		Material	_blurMaterial;
		Material	blurMaterial
		{
			get
			{
				if (!_blurMaterial)
					_blurMaterial = new Material(blurShader);
				return _blurMaterial;
			}
		}

		protected void OnRenderImage (RenderTexture source, RenderTexture destination)
		{	
			if (!material || !blurMaterial || !overCloudProbe)
			{
				Graphics.Blit(source, destination);
				return;
			}

			var desc = source.descriptor;
			desc.width /= 4;
			desc.height /= 4;
			var rt = RenderTexture.GetTemporary(desc);
			var rt2 = RenderTexture.GetTemporary(desc);

			Shader.SetGlobalVector("_PixelSize", new Vector2(1f / rt.width, 1f / rt.height));
			Shader.SetGlobalFloat("_BlurAmount", blurAmount);

			// Blur
			Graphics.Blit(source, rt, blurMaterial, 0);
			Graphics.Blit(rt, rt2, blurMaterial, 1);

			material.SetTexture("_MainTexBlurred", rt2);
			material.SetTexture("_BlurMask", blurMask);
			material.SetFloat("_Intensity", Application.isPlaying ? Mathf.Max(overCloudProbe.density, overCloudProbe.rain) : 0);
			Graphics.Blit(source, destination, material, 0);

			RenderTexture.ReleaseTemporary(rt);
			RenderTexture.ReleaseTemporary(rt2);
		}
	}
}