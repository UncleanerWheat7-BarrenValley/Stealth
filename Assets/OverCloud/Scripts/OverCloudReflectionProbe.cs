///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OC
{
	/// <summary>
	/// The OverCloudReflectionProbe component enables rendering of OverCloud in Reflection Probes.
	/// It also offers controls for when and how to update the reflection probe,
	/// whether to spread the workload over multiple frames or not,
	/// and functionality for saving the cubemap to a file (using horizontal face layout).
	/// </summary>
	#if UNITY_EDITOR
	[InitializeOnLoad]
	#endif
	[ExecuteInEditMode, RequireComponent(typeof(ReflectionProbe))]
	public class OverCloudReflectionProbe : MonoBehaviour
	{
		[System.Serializable]
		public enum UpdateMode
		{
			OnSkyChanged,
			OnEnable,
			Realtime,
			ScriptOnly
		}

		[System.Serializable]
		public enum SpreadMode
		{
			_7Frames,
			_1Frame
		}

		private static Quaternion[] orientations = new Quaternion[]
        {
            Quaternion.LookRotation(Vector3.right,		Vector3.down),
            Quaternion.LookRotation(Vector3.left,		Vector3.down),
            Quaternion.LookRotation(Vector3.up,			Vector3.forward),
            Quaternion.LookRotation(Vector3.down,		Vector3.back),
            Quaternion.LookRotation(Vector3.forward,	Vector3.down),
            Quaternion.LookRotation(Vector3.back,		Vector3.down)
        };
		private static Quaternion[] orientationsFlipped = new Quaternion[]
        {
            Quaternion.LookRotation(Vector3.right,		Vector3.up),
            Quaternion.LookRotation(Vector3.left,		Vector3.up),
            Quaternion.LookRotation(Vector3.up,			Vector3.back),
            Quaternion.LookRotation(Vector3.down,		Vector3.forward),
            Quaternion.LookRotation(Vector3.forward,	Vector3.up),
            Quaternion.LookRotation(Vector3.back,		Vector3.up)
        };

		public bool hasFinishedRendering { get; private set;}

		Camera					m_Camera;
		OverCloudCamera			m_OverCloudCamera;
		ReflectionProbe			m_Probe;
		RenderTexture			m_Result;
		RenderTexture			m_CubeMap;
		Material				m_TransferMaterial;
		int						m_CurrentFace			= -1;

		[Tooltip("If and when the reflection probe should be updated. If set to ScriptOnly, the reflection probe will not render unless RenderProbe is manually called.")]
		public UpdateMode		updateMode				= UpdateMode.OnSkyChanged;
		[Tooltip("How many frames to spread the reflection probe update over. 1 frame will make the result available immediately after calling RenderProbe. 7 frames will spread the render work over the first 6 frames, and calculate mip maps on the 7th.")]
		public SpreadMode		spreadMode				= SpreadMode._7Frames;

		[Header("General")]
		[Tooltip("The level of downsampling to use when rendering the volumetric clouds and volumetric lighting. This enables you to render the effects at 1/2, 1/4 or 1/8 resolution and can give you a big performance boost in exchange for fidelity.")]
		public DownSampleFactor	downsampleFactor		= DownSampleFactor.Half;

		[Header("Volumetric Clouds")]
		[Tooltip("Toggle the rendering of the volumetric clouds.")]
		public bool				renderVolumetricClouds			= true;
		[Tooltip("Toggle the rendering of the 2D fallback cloud plane for the volumetric clouds.")]
		public bool				render2DFallback		= true;
		[Tooltip("The number of samples to use when ray-marching the lighting for the volumetric clouds. A higher value will look nicer at the cost of performance.")]
        public SampleCount      lightSampleCount        = SampleCount.Low;
		[Tooltip("Use the high-resolution 3D noise for the light ray-marching for the volumetric clouds, which is normally only used for the alpha.")]
        public bool				highQualityClouds		= false;
		[Tooltip("Downsample the 2D clouds along with the volumetric ones. Can save performance at the cost of fidelity, especially around the horizon.")]
        public bool				downsample2DClouds		= false;

		[Header("Atmosphere")]
		[Tooltip("Toggle the rendering of atmospheric scattering and fog.")]
		public bool				renderAtmosphere		= true;
		[Tooltip("Enable the scattering mask (god rays).")]
		public bool				renderScatteringMask	= false;
		[Tooltip("Include the cascaded shadow map in the scattering mask.")]
		public bool				includeCascadedShadows	= true;
		[Tooltip("How many samples the scattering mask should use when rendering. More results in higher quality but slower rendering.")]
		public SampleCount		scatteringMaskSamples = SampleCount.Normal;

		[Header("Weather")]
		[Tooltip("Enable the rain height mask.")]
		public bool				renderRainMask			= false;

		[Header("Camera Settings")]
		[Tooltip("Enable rendering of shadows in the reflection probe (shadows need to be enabled in quality settings also).")]
		public bool		enableShadows = false;

		[Header("Misc")]
		[Tooltip("Print debug information in the console.")]
		public bool		debug = false;

		[Header("Cubemap Saving")]
		[Tooltip("The file path to store the cubemap .exr when saving (the filename will be OverCloudReflectionProbe.exr).")]
		public string	filePath = "";

		private bool	m_FlippedRendering = false;

		private void OnEnable()
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
				EditorApplication.update += RenderUpdate;
			#endif

			Initialize();

			RenderProbe();
		}

		private void OnValidate()
		{
			Reset();
		}

		private void Initialize ()
		{
			// Initialize the camera used to render reflections
			if (!m_Camera)
			{
				var go					= new GameObject("ReflectionCamera");
				go.transform.parent		= transform;
				go.hideFlags			= HideFlags.HideAndDontSave;
				m_Camera				= go.AddComponent<Camera>();
				m_Camera.enabled		= false;
				m_Camera.tag			= "Untagged";
				m_OverCloudCamera		= m_Camera.gameObject.AddComponent<OverCloudCamera>();
			}
			m_Probe						= GetComponent<ReflectionProbe>();
			var desc					= new RenderTextureDescriptor(m_Probe.resolution, m_Probe.resolution, RenderTextureFormat.DefaultHDR);
			desc.useMipMap				= true;
			desc.autoGenerateMips		= false;
			desc.depthBufferBits		= 0;

			m_Result					= new RenderTexture(desc);
			m_Result.dimension			= TextureDimension.Cube;
			m_Result.antiAliasing		= 1;
			m_Result.filterMode			= FilterMode.Trilinear;
			m_CubeMap					= new RenderTexture(desc);
			m_CubeMap.dimension			= TextureDimension.Cube;
			m_CubeMap.antiAliasing		= 1;
			m_CubeMap.filterMode		= FilterMode.Trilinear;

			// Set up mip maps?
			Graphics.Blit(Texture2D.whiteTexture, m_Result);
			Graphics.Blit(Texture2D.whiteTexture, m_CubeMap);
			m_Result.GenerateMips();
			m_CubeMap.GenerateMips();

			m_Probe.mode				= ReflectionProbeMode.Custom;
			m_Probe.customBakedTexture	= m_CubeMap;

			m_TransferMaterial			= new Material(Shader.Find("Hidden/OverCloud/Utilities"));
			//m_ConvolutionMaterial		= new Material(Shader.Find("Hidden/CubeBlur"));
		}

		public void RenderProbe ()
		{
			hasFinishedRendering = false;
			m_CurrentFace = -1;
			if (spreadMode == SpreadMode._1Frame)
				RenderUpdate();
		}

		private void OnDisable()
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
				EditorApplication.update -= RenderUpdate;
			#endif
			m_Probe.mode = ReflectionProbeMode.Baked;
		}

		private void Update()
		{
			if (Application.isPlaying)
				RenderUpdate();
		}

		/// <summary>
		/// The main render update loop, where the script decides what to do based on currentFace
		/// </summary>
		private void RenderUpdate ()
		{
			if (m_CubeMap.width != m_Probe.resolution)
			{
				Initialize();
				Reset();
			}

			if (m_CurrentFace > 6)
			{
				// Rendering is considered "finished" when m_CurrentFace is larger than 6, but we might want to restart it depending on the update mode
				if (updateMode == UpdateMode.OnSkyChanged && OverCloud.skyChanged)
					m_CurrentFace = -1;
				else
					return;
			}

			if (m_CurrentFace < 0)
				Reset();

			if (!m_Camera)
				Initialize();
			
			while (m_CurrentFace < 6)
			{
				hasFinishedRendering = false;

				// Frame 1-6: Render one face of the cubemap

				if (debug)
					Debug.Log("OverCloudReflectionProbe: Updating face " + (m_CurrentFace + 1).ToString() + ".");

				UpdateCamera();

				// Render a single side of the cubemap
				Shader.EnableKeyword("OVERCLOUD_REFLECTION");
				var shadowsPrev = QualitySettings.shadows;
				if (!enableShadows)
					QualitySettings.shadows = ShadowQuality.Disable;
				var desc = new RenderTextureDescriptor(m_CubeMap.width, m_CubeMap.height, m_CubeMap.format, 16);
				desc.useMipMap = false;
				var rt = RenderTexture.GetTemporary(desc);
				m_Camera.targetTexture = rt;
				m_Camera.Render();
				QualitySettings.shadows = shadowsPrev;
				Shader.DisableKeyword("OVERCLOUD_REFLECTION");

				// Transfer to cubemap face + mip maps
				var wasActive = RenderTexture.active;
				var face = (CubemapFace)m_CurrentFace;
				//Graphics.SetRenderTarget(m_Result, 0, face);
				Graphics.SetRenderTarget(m_CubeMap, 0, face);
				m_TransferMaterial.SetTexture("_MainTex", rt);
				m_TransferMaterial.SetInt("_Flip", m_FlippedRendering ? 0 : 1);
				Graphics.Blit(rt, m_TransferMaterial, 2);
				RenderTexture.ReleaseTemporary(rt);
				RenderTexture.active = wasActive;

				m_CurrentFace++;

				// If we are spreading updates across multiple frames, we break here
				if (spreadMode == SpreadMode._7Frames)
					return;
			}

			// Frame 7: Generate mip maps

			if (debug)
				Debug.Log("OverCloudReflectionProbe: Generating mip maps.");

			// This is a poor solution as the mip maps will not use specular convolution
			// Unfortunately, Unity's shader for doing so is very complex and no info on it exists
			// If you solve this, contact me and I will update the plugin
			m_CubeMap.GenerateMips();

			if (updateMode == UpdateMode.Realtime)
				// Force a reset next frame
				m_CurrentFace = -1;
			else
				m_CurrentFace++; // = 7

			hasFinishedRendering = true;

			return;

			/*
			//m_CubeMap.GenerateMips();
			//m_Result.GenerateMips();

			// Calculate mip map count
			int mipCount = 7;// 1 + Mathf.FloorToInt(Mathf.Log10(m_CubeMap.width) / Mathf.Log10(2f));

			GL.PushMatrix();
			GL.LoadOrtho();
			
			// Custom mip mapping using specular convolution
			for (int mip = 0; mip < mipCount + 1; mip++)
			{
				// Ping to m_Cubemap (copy each face)
				Graphics.CopyTexture(m_Result, 0, mip, m_CubeMap, 0, mip);
				Graphics.CopyTexture(m_Result, 1, mip, m_CubeMap, 1, mip);
				Graphics.CopyTexture(m_Result, 2, mip, m_CubeMap, 2, mip);
				Graphics.CopyTexture(m_Result, 3, mip, m_CubeMap, 3, mip);
				Graphics.CopyTexture(m_Result, 4, mip, m_CubeMap, 4, mip);
				Graphics.CopyTexture(m_Result, 5, mip, m_CubeMap, 5, mip);

				// Destination mip level
				int dstMip = mip + 1;

				if (dstMip == mipCount)
					// We just copied the last mip level, don't need to convolve any further
					break;

				// The resolution of the source mip level
				//int mipRes = m_CubeMap.width / (int)Mathf.Pow(2, mip);
				int mipRes = m_CubeMap.width;
				for (int u = 0; u < mip; u++)
					mipRes /= 2;

				// Pong to m_Result (convolve)
				m_ConvolutionMaterial.SetTexture("_MainTex", m_CubeMap);
				m_ConvolutionMaterial.SetFloat("_Texel", 1f / mipRes);
				m_ConvolutionMaterial.SetFloat("_Level", mip);

				// Activate the material for rendering
				m_ConvolutionMaterial.SetPass(0);

				// The CubeBlur shader uses 3D texture coordinates when sampling the cubemap,
				// so we can't use Graphics.Blit here.
				// Instead we build a cube (sort of) and render using that.

				// Positive X
				Graphics.SetRenderTarget(m_Result, dstMip, CubemapFace.PositiveX);
				GL.Begin(GL.QUADS);
				GL.TexCoord3( 1, 1, 1);
				GL.Vertex3(0, 0, 1);
				GL.TexCoord3( 1,-1, 1);
				GL.Vertex3(0, 1, 1);
				GL.TexCoord3( 1,-1,-1);
				GL.Vertex3(1, 1, 1);
				GL.TexCoord3( 1, 1,-1);
				GL.Vertex3(1, 0, 1);
				GL.End();

				// Negative X
				Graphics.SetRenderTarget(m_Result, dstMip, CubemapFace.NegativeX);
				GL.Begin(GL.QUADS);
				GL.TexCoord3(-1, 1,-1);
				GL.Vertex3(0, 0, 1);
				GL.TexCoord3(-1,-1,-1);
				GL.Vertex3(0, 1, 1);
				GL.TexCoord3(-1,-1, 1);
				GL.Vertex3(1, 1, 1);
				GL.TexCoord3(-1, 1, 1);
				GL.Vertex3(1, 0, 1);
				GL.End();

				// Positive Y
				Graphics.SetRenderTarget(m_Result, dstMip, CubemapFace.PositiveY);
				GL.Begin(GL.QUADS);
				GL.TexCoord3(-1, 1,-1);
				GL.Vertex3(0, 0, 1);
				GL.TexCoord3(-1, 1, 1);
				GL.Vertex3(0, 1, 1);
				GL.TexCoord3( 1, 1, 1);
				GL.Vertex3(1, 1, 1);
				GL.TexCoord3( 1, 1,-1);
				GL.Vertex3(1, 0, 1);
				GL.End();

				// Negative Y
				Graphics.SetRenderTarget(m_Result, dstMip, CubemapFace.NegativeY);
				GL.Begin(GL.QUADS);
				GL.TexCoord3(-1,-1, 1);
				GL.Vertex3(0, 0, 1);
				GL.TexCoord3(-1,-1,-1);
				GL.Vertex3(0, 1, 1);
				GL.TexCoord3( 1,-1,-1);
				GL.Vertex3(1, 1, 1);
				GL.TexCoord3( 1,-1, 1);
				GL.Vertex3(1, 0, 1);
				GL.End();

				// Positive Z
				Graphics.SetRenderTarget(m_Result, dstMip, CubemapFace.PositiveZ);
				GL.Begin(GL.QUADS);
				GL.TexCoord3(-1, 1, 1);
				GL.Vertex3(0, 0, 1);
				GL.TexCoord3(-1,-1, 1);
				GL.Vertex3(0, 1, 1);
				GL.TexCoord3( 1,-1, 1);
				GL.Vertex3(1, 1, 1);
				GL.TexCoord3( 1, 1, 1);
				GL.Vertex3(1, 0, 1);
				GL.End();

				// Negative Z
				Graphics.SetRenderTarget(m_Result, dstMip, CubemapFace.NegativeZ);
				GL.Begin(GL.QUADS);
				GL.TexCoord3( 1, 1,-1);
				GL.Vertex3(0, 0, 1);
				GL.TexCoord3( 1,-1,-1);
				GL.Vertex3(0, 1, 1);
				GL.TexCoord3(-1,-1,-1);
				GL.Vertex3(1, 1, 1);
				GL.TexCoord3(-1, 1,-1);
				GL.Vertex3(1, 0, 1);
				GL.End();
			}

			GL.PopMatrix();

			// Force a reset next frame
			if (updateMode == UpdateMode.Realtime)
				m_CurrentFace = -1;
			else
				m_CurrentFace++; // = 7

			hasFinishedRendering = true;
			*/
		}

		/// <summary>
		/// Start the rendering process over
		/// </summary>
		private void Reset()
		{
			m_CurrentFace = 0;
		}

		/// <summary>
		/// Update the reflection camera with the proper settings
		/// </summary>
		private void UpdateCamera ()
		{
			m_Camera.transform.position = transform.position;
			if (m_FlippedRendering)
				m_Camera.transform.rotation = orientationsFlipped[m_CurrentFace];
			else
				m_Camera.transform.rotation = orientations[m_CurrentFace];

			m_Camera.cameraType			= CameraType.Reflection;
			m_Camera.fieldOfView		= 90;
			m_Camera.cameraType			= CameraType.Reflection;
            m_Camera.farClipPlane		= m_Probe.farClipPlane;
            m_Camera.nearClipPlane		= m_Probe.nearClipPlane;
            m_Camera.cullingMask		= m_Probe.cullingMask;
            m_Camera.clearFlags			= (CameraClearFlags)m_Probe.clearFlags;
            m_Camera.backgroundColor	= m_Probe.backgroundColor;
            m_Camera.allowHDR			= m_Probe.hdr;
			m_Camera.allowMSAA			= false;

			m_OverCloudCamera.renderVolumetricClouds	= renderVolumetricClouds;
			m_OverCloudCamera.render2DFallback			= render2DFallback;
			m_OverCloudCamera.renderAtmosphere			= renderAtmosphere;
			m_OverCloudCamera.renderScatteringMask		= renderScatteringMask;
			m_OverCloudCamera.includeCascadedShadows	= includeCascadedShadows;
			m_OverCloudCamera.scatteringMaskSamples		= scatteringMaskSamples;
			m_OverCloudCamera.renderRainMask			= renderRainMask;
			m_OverCloudCamera.downsampleFactor			= downsampleFactor;
			m_OverCloudCamera.lightSampleCount			= lightSampleCount;
			m_OverCloudCamera.highQualityClouds			= highQualityClouds;
			m_OverCloudCamera.downsample2DClouds		= downsample2DClouds;
		}

		public void SaveCubemap ()
		{
			var prevMode  = spreadMode;
			var wasActive = RenderTexture.active;
			if (filePath == "")
				filePath = Application.dataPath + "/";

			// Force full refresh of cubemap
			spreadMode			= SpreadMode._1Frame;
			m_CurrentFace		= -1;
			m_FlippedRendering	= true;
			RenderUpdate();
			m_FlippedRendering	= false;

			// Create the output texture
			Texture2D tex = new Texture2D(m_CubeMap.width * 6, m_CubeMap.height, UnityEngine.TextureFormat.RGBAHalf, false, false);
			
			// Copy each cubemap face
			Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.PositiveX);
			tex.ReadPixels(new Rect(0, 0, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 0, 0);
			Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.NegativeX);
			tex.ReadPixels(new Rect(0, 0, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 1, 0);
			Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.PositiveY);
			tex.ReadPixels(new Rect(0, 0, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 2, 0);
			Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.NegativeY);
			tex.ReadPixels(new Rect(0, 0, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 3, 0);
			Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.PositiveZ);
			tex.ReadPixels(new Rect(0, 0, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 4, 0);
			Graphics.SetRenderTarget(m_CubeMap, 0, CubemapFace.NegativeZ);
			tex.ReadPixels(new Rect(0, 0, m_CubeMap.width, m_CubeMap.height), m_CubeMap.width * 5, 0);

			// Encode and output to file
			byte[] bytes;
			bytes = tex.EncodeToEXR();
			System.IO.File.WriteAllBytes(filePath + "OverCloudReflectionProbe.exr", bytes);

			// Clean up
			if (Application.isPlaying)
				Destroy(tex);
			else
				DestroyImmediate(tex);

			Debug.Log("Cubemap saved to " + filePath);

			// Need to render again after resetting so the probe doesn't show the potentially post-processed result
			RenderProbe();

			RenderTexture.active	= wasActive;
			spreadMode				= prevMode;
		}
	}
}