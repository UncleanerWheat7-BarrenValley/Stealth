///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OC
{
	/// <summary>
	/// The OverCloudCamera component needs to be added to any camera you want OverCloud to render in.
	/// It also allows you to control visual quality per-camera.
	/// Having an OverCloudCamera component active on the main camera will also enable rendering in the scene camera.
	/// </summary>
	[ExecuteInEditMode]
	#if UNITY_5_4_OR_NEWER
		[ImageEffectAllowedInSceneView]
	#endif
	public class OverCloudCamera : MonoBehaviour
	{
		[Header("General")]
		/// <summary>
		/// The level of downsampling to use when rendering the volumetric clouds and volumetric lighting. This enables you to render the effects at 1/2, 1/4 or 1/8 resolution and can give you a big performance boost in exchange for fidelity.
		/// </summary>
		[Tooltip("The level of downsampling to use when rendering the volumetric clouds and volumetric lighting. This enables you to render the effects at 1/2, 1/4 or 1/8 resolution and can give you a big performance boost in exchange for fidelity.")]
		public DownSampleFactor	downsampleFactor		= DownSampleFactor.Half;

		[Header("Volumetric Clouds")]
		/// <summary>
		/// Toggle the rendering of the volumetric clouds.
		/// </summary>
		[Tooltip("Toggle the rendering of the volumetric clouds.")]
		public bool				renderVolumetricClouds	= true;
		/// <summary>
		/// Toggle the rendering of the 2D fallback cloud plane for the volumetric clouds.
		/// </summary>
		[Tooltip("Toggle the rendering of the 2D fallback cloud plane for the volumetric clouds.")]
		public bool				render2DFallback		= true;
		/// <summary>
		/// The number of samples to use when ray-marching the lighting for the volumetric clouds. A higher value will look nicer at the cost of performance.
		/// </summary>
		[Tooltip("The number of samples to use when ray-marching the lighting for the volumetric clouds. A higher value will look nicer at the cost of performance.")]
        public SampleCount      lightSampleCount        = SampleCount.Normal;
		/// <summary>
		/// Use the high-resolution 3D noise for the light ray-marching for the volumetric clouds, which is normally only used for the alpha.
		/// </summary>
		[Tooltip("Use the high-resolution 3D noise for the light ray-marching for the volumetric clouds, which is normally only used for the alpha.")]
        public bool				highQualityClouds		= false;
		/// <summary>
		/// Downsample the 2D clouds along with the volumetric ones. Can save performance at the cost of fidelity, especially around the horizon.
		/// </summary>
		[Tooltip("Downsample the 2D clouds along with the volumetric ones. Can save performance at the cost of fidelity, especially around the horizon.")]
        public bool				downsample2DClouds		= false;

		[Header("Atmosphere")]
		/// <summary>
		/// Toggle the rendering of atmospheric scattering and fog.
		/// </summary>
		[Tooltip("Toggle the rendering of atmospheric scattering and fog.")]
		public bool				renderAtmosphere		= true;
		/// <summary>
		/// Enable the scattering mask (god rays).
		/// </summary>
		[Tooltip("Enable the scattering mask (god rays).")]
		public bool				renderScatteringMask	= true;
		/// <summary>
		/// Include the cascaded shadow map in the scattering mask.
		/// </summary>
		[Tooltip("Include the cascaded shadow map in the scattering mask.")]
		public bool				includeCascadedShadows	= true;
		/// <summary>
		/// How many samples the scattering mask should use when rendering. More results in higher quality but slower rendering.
		/// </summary>
		[Tooltip("How many samples the scattering mask should use when rendering. More results in higher quality but slower rendering.")]
		public SampleCount		scatteringMaskSamples = SampleCount.Normal;

		[Header("Weather")]
		/// <summary>
		/// Enable the rain height mask. This is what prevents surfaces from being wet depending on their position beneath other geometry (dynamic wetness).
		/// </summary>
		[Tooltip("Enable the rain height mask. This is what prevents surfaces from being wet depending on their position beneath other geometry (dynamic wetness).")]
		public bool				renderRainMask			= false;

		public new Camera		camera					{ get; private set; }
		// Flag used for multi-pass rendering to prevent running CameraUpdate twice
		bool					hasUpdated				= false;

		private void OnEnable ()
		{
			camera = GetComponent<Camera>();

			#if UNITY_EDITOR
			if (!Application.isPlaying)
				EditorApplication.update += EditorUpdate;
			#endif
		}

		private void OnDisable ()
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
				EditorApplication.update -= EditorUpdate;
			#endif
		}

		void OnPreRender ()
		{
			if (enabled)
			{
				if (gameObject.tag == "MainCamera" && !hasUpdated)
				{
					OverCloud.CameraUpdate(camera);
					hasUpdated = true;
				}
				else if (renderVolumetricClouds)
					OverCloud.PositionCloudVolume(camera);
				OverCloud.Render(
					camera,
					renderVolumetricClouds,
					render2DFallback,
					renderAtmosphere,
					renderScatteringMask,
					includeCascadedShadows,
					downsample2DClouds,
					scatteringMaskSamples,
					renderRainMask,
					downsampleFactor,
					lightSampleCount,
					highQualityClouds
				);
			}
		}

		private void OnPostRender ()
		{
			if (enabled)
			{
				// Force keywords off
				Shader.DisableKeyword("OVERCLOUD_ENABLED");
				Shader.DisableKeyword("OVERCLOUD_ATMOSPHERE_ENABLED");
				Shader.DisableKeyword("DOWNSAMPLE_2D_CLOUDS");
				Shader.DisableKeyword("RAIN_MASK_ENABLED");
				Shader.DisableKeyword("OVERCLOUD_SKY_ENABLED");
				
				// This is set to 1 if the cascaded shadow maps are rendered to.
				// Sometimes, Unity decides that shadows don't need to be rendered which can otherwise lead to weird behaviour.
				Shader.SetGlobalFloat("_CascadeShadowMapPresent", 0f);
			}
			OverCloud.CleanUp();
			// Reset multi-pass flag
			hasUpdated = false;
		}

		void EditorUpdate ()
		{
			if (enabled)
			{
				var camera = Camera.current;
				if (!camera || camera.name != "SceneCamera")
					return;

				if (gameObject.tag == "MainCamera")
					OverCloud.CameraUpdate(camera);
			}
		}
	}
}
