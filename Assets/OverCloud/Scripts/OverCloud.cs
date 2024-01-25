///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using Random = UnityEngine.Random;

namespace OC
{
	#region Enums, structs and classes
	/// <summary>
	/// Describes how much rendering should be downsampled to save performance.
	/// </summary>
	[System.Serializable]
	public enum DownSampleFactor
	{
		Full	= 1,
		Half	= 2,
		Quarter	= 4,
		Eight	= 8
	}

	/// <summary>
	/// Used to select the resolution of the compositor texture used to drive other effects.
	/// </summary>
	[System.Serializable]
	public enum CompositorResolution
	{
		_256x256   = 256,
		_512x512   = 512,
		_1024x1024 = 1024,
		_2048x2048 = 2048
	}

	/// <summary>
	/// Used to select the resolution of the cloud shadow buffer.
	/// </summary>
	[System.Serializable]
	public enum ShadowsResolution
	{
		_256x256   = 256,
		_512x512   = 512,
		_1024x1024 = 1024,
		_2048x2048 = 2048,
		_4096x4096 = 4096,
	}

	/// <summary>
	/// Used to select the resolution of the rain depth mask.
	/// </summary>
	[System.Serializable]
	public enum RainMaskResolution
	{
		_256x256   = 256,
		_512x512   = 512,
		_1024x1024 = 1024,
		_2048x2048 = 2048
	}

	/// <summary>
	/// Used to select the resolution of the 3D noise texture.
	/// </summary>
	[System.Serializable]
	public enum _3DNoiseResolution
	{
		_16x16   = 16,
		_32x32   = 32,
		_64x64   = 64,
		_128x128 = 128,
		_256x256 = 256
	}

	/// <summary>
	/// Generic quality enum.
	/// </summary>
	[System.Serializable]
	public enum SampleCount
	{
		Low,
		Normal,
		High
	}

	/// <summary>
	/// Enum describing the different cloud shadow modes.
	/// </summary>
	[System.Serializable]
	public enum CloudShadowsMode
	{
		/// <summary>
		/// Cloud shadows are automatically injected into the screen-space shadow mask.
		/// Unfortunately this means the shadows will use the same fade distance as the cascaded shadows,
		/// however it supports both deferred and forward rendering out of the box.
		/// </summary>
		Injected,
		/// <summary>
		/// Cloud shadows are not injected into the screen-space shadow mask.
		/// Cloud shadows have to be added externally. For deferred rendering you can swap out the 'Internal-DeferredShading' shader
		/// under Project Settings > Graphics for the one in the Resources/Shaders folder.
		/// For forward rendering you have to implement it in custom shaders.
		/// </summary>
		External
	}

	/// <summary>
	/// A container struct for the data returned by an OverCloudProbe.
	/// </summary>
	public struct CloudDensity
	{
		/// <summary>
		/// The cloud density at the probe position.
		/// </summary>
		public float density;
		/// <summary>
		/// The cloud density at the position directly above the probe.
		/// </summary>
		public float coverage;
		/// <summary>
		/// The amount of rain currently hitting the probe.
		/// </summary>
		public float rain;
	}

	/// <summary>
	/// A struct describing a custom float parameter.
	/// </summary>
	[System.Serializable]
	public struct CustomFloat
	{
		/// <summary>
		/// The name of the custom float parameter.
		/// </summary>
		public string	name;
		/// <summary>
		/// An optional string which if provided will automatically set a global shader parameter with the same value.
		/// </summary>
		public string	shaderParameter;
	}

	/// <summary>
	/// A class describing a weather preset (or the current weather). Supports smooth interpolation of all parameters.
	/// </summary>
	[System.Serializable]
	public class WeatherPreset
	{
		[Tooltip("The name of the preset.")]
		public string name;
		[Tooltip("The altitude at which the volumetric cloud plane will appear.")]
		public float cloudPlaneAltitude		= 1200;
		[Tooltip("The height of the volumetric cloud plane.")]
		[Range(0, 1000)]
		public float cloudPlaneHeight		= 400;
		[Tooltip("The small-scale volumetric cloudiness.")]
		[Range(0, 1)]
		public float cloudiness				= 0.75f;
		[Tooltip("A sharpening value to apply to the small-scale volumetric cloudiness.")]
		[Range(0, 1)]
		public float sharpness				= 0.25f;
		[Tooltip("The large-scale volumetric cloudiness.")]
		[Range(0, 1)]
		public float macroCloudiness		= 0.75f;
		[Tooltip("A sharpening value to apply to the large-scale volumetric cloudiness.")]
		[Range(0, 1)]
		public float macroSharpness			= 0.25f;

		[Tooltip("The density value used for calculating the alpha of the clouds.")]
		[Range(0, 8)]
		public float opticalDensity			= 1f;
		[Tooltip("The density value used for calculating the lighting of the clouds.")]
		[Range(0, 8)]
		public float lightingDensity		= 1f;

		[Tooltip("The density of the cloud shadows.")]
		[Range(0, 4)]
		public float cloudShadowsDensity	= 1f;
		[Tooltip("The opacity of the cloud shadows.")]
		[Range(0, 4)]
		public float cloudShadowsOpacity	= 1f;
		[Tooltip("The amount of precipitation (rain/snow) from the clouds.")]
		[Range(0, 1)]
		public float precipitation			= 0;
		[Tooltip("The odds of a lightning strike.")]
		[Range(0, 1)]
		public float lightningChance		= 0;
		
		[Tooltip("A multiplier value which is used when incrementing the wind time.")]
		public float windMultiplier			= 0;
		[Range(0, 1)]
		[Tooltip("A sharpening value to apply to the wetness effect below the clouds.")]
		public float wetnessRemap			= 0.5f;
		[Range(0, 1)]
		[Tooltip("How much the albedo should be darkened by wet areas below the clouds.")]
		public float wetnessDarken			= 0.5f;
		[Range(0, 1)]
		[Tooltip("How much the gloss should be increased by wet areas below the clouds.")]
		public float wetnessGloss			= 0.75f;

		[Tooltip("The density level of the global height fog.")]
		[Range(0, 1)]
		public float fogDensity				= 0;
		[Tooltip("The fog/scattering blend factor. 0 = only fog. 1 = balance between the two. 2 = scattering will appear on top of fog.")]
		[Range(0, 2)]
		public float fogBlend				= 1;
		[Tooltip("The color of the fog.")]
		public Color fogAlbedo				= new Color(0, 0.1f, 0.2f, 1f);
		[Tooltip("How much the fog should be affected by direct lighting from the sun and moon.")]
		[Range(0, 1)]
		public float fogDirectIntensity		= 0.25f;
		[Tooltip("How much the fog should be affected by indirect lighting from the sky.")]
		[Range(0, 1)]
		public float fogAmbientIntensity	= 0.25f;
		[Tooltip("The intensity of the fog shadow effect.")]
		[Range(0, 1)]
		public float fogShadow				= 1f;
		[Tooltip("The upper limit of the volumetric fog volume. Defined in cloud height factors.")]
		[Range(0, 4)]
		public float fogHeight				= 1f;
		[Tooltip("Fog height falloff, in meters.")]
		public float fogFalloff				= 1000f;
		[Tooltip("This is where custom floats show up, if any are defined.")]
		public float[] customFloats;

		public WeatherPreset (string name)
		{
			this.name = name;
		}

		public WeatherPreset (
			string	p_name,
			float	p_cloudPlaneAltitude,
			float	p_cloudPlaneHeight,
			float	p_cloudiness,
			float	p_sharpness,
			float	p_macroCloudiness,
			float	p_macroSharpness,
			float	p_opticalDensity,
			float	p_lightingDensity,
			float	p_cloudShadowsDensity,
			float	p_cloudShadowsOpacity,
			float	p_precipitation,
			float	p_lightningChance,
			float	p_windMultiplier,
			float	p_wetnessRemap,
			float	p_wetnessDarken,
			float	p_wetnessGloss,
			float	p_fogDensity,
			float	p_fogBlend,
			Color	p_fogAlbedo,
			float	p_fogDirectIntensity,
			float	p_fogAmbientIntensity,
			float	p_fogShadow,
			float	p_fogHeight,
			float	p_fogFalloff)
		{
			name						= p_name;
			cloudPlaneAltitude			= p_cloudPlaneAltitude;
			cloudPlaneHeight			= p_cloudPlaneHeight;
			cloudiness					= p_cloudiness;
			sharpness					= p_sharpness;
			macroCloudiness				= p_macroCloudiness;
			macroSharpness				= p_macroSharpness;
			opticalDensity				= p_opticalDensity;
			lightingDensity				= p_lightingDensity;
			cloudShadowsDensity			= p_cloudShadowsDensity;
			cloudShadowsOpacity			= p_cloudShadowsOpacity;
			precipitation				= p_precipitation;
			lightningChance				= p_lightningChance;
			windMultiplier				= p_windMultiplier;
			wetnessRemap				= p_wetnessRemap;
			wetnessDarken				= p_wetnessDarken;
			wetnessGloss				= p_wetnessGloss;
			fogDensity					= p_fogDensity;
			fogBlend					= p_fogBlend;
			fogAlbedo					= p_fogAlbedo;
			fogDirectIntensity			= p_fogDirectIntensity;
			fogAmbientIntensity			= p_fogAmbientIntensity;
			fogShadow					= p_fogShadow;
			fogHeight					= p_fogHeight;
			fogFalloff					= p_fogFalloff;
		}

		// Copy constructor
		public WeatherPreset (WeatherPreset obj)
		{
			name						= obj.name;
			cloudPlaneAltitude			= obj.cloudPlaneAltitude;
			cloudPlaneHeight			= obj.cloudPlaneHeight;
			cloudiness					= obj.cloudiness;
			sharpness					= obj.sharpness;
			macroCloudiness				= obj.macroCloudiness;
			macroSharpness				= obj.macroSharpness;
			opticalDensity				= obj.opticalDensity;
			lightingDensity				= obj.lightingDensity;
			cloudShadowsDensity			= obj.cloudShadowsDensity;
			cloudShadowsOpacity			= obj.cloudShadowsOpacity;
			precipitation				= obj.precipitation;
			lightningChance				= obj.lightningChance;
			windMultiplier				= obj.windMultiplier;
			wetnessRemap				= obj.wetnessRemap;
			wetnessDarken				= obj.wetnessDarken;
			wetnessGloss				= obj.wetnessGloss;
			fogDensity					= obj.fogDensity;
			fogBlend					= obj.fogBlend;
			fogAlbedo					= obj.fogAlbedo;
			fogDirectIntensity			= obj.fogDirectIntensity;
			fogAmbientIntensity			= obj.fogAmbientIntensity;
			fogShadow					= obj.fogShadow;
			fogHeight					= obj.fogHeight;
			fogFalloff					= obj.fogFalloff;
			customFloats				= obj.customFloats;
		}

		public void Lerp (WeatherPreset a, WeatherPreset b, float t)
		{
			cloudiness					= Mathf.Lerp(a.cloudiness, b.cloudiness, t);
			cloudPlaneAltitude			= Mathf.Lerp(a.cloudPlaneAltitude, b.cloudPlaneAltitude, t);
			cloudPlaneHeight			= Mathf.Lerp(a.cloudPlaneHeight, b.cloudPlaneHeight, t);
			sharpness					= Mathf.Lerp(a.sharpness, b.sharpness, t);
			macroCloudiness				= Mathf.Lerp(a.macroCloudiness, b.macroCloudiness, t);
			macroSharpness				= Mathf.Lerp(a.macroSharpness, b.macroSharpness, t);
			opticalDensity				= Mathf.Lerp(a.opticalDensity, b.opticalDensity, t);
			lightingDensity				= Mathf.Lerp(a.lightingDensity, b.lightingDensity, t);
			cloudShadowsDensity			= Mathf.Lerp(a.cloudShadowsDensity, b.cloudShadowsDensity, t);
			cloudShadowsOpacity			= Mathf.Lerp(a.cloudShadowsOpacity, b.cloudShadowsOpacity, t);
			precipitation				= Mathf.Lerp(a.precipitation, b.precipitation, t);
			lightningChance				= Mathf.Lerp(a.lightningChance, b.lightningChance, t);
			windMultiplier				= Mathf.Lerp(a.windMultiplier, b.windMultiplier, t);
			wetnessRemap				= Mathf.Lerp(a.wetnessRemap, b.wetnessRemap, t);
			wetnessDarken				= Mathf.Lerp(a.wetnessDarken, b.wetnessDarken, t);
			wetnessGloss				= Mathf.Lerp(a.wetnessGloss, b.wetnessGloss, t);
			fogDensity					= Mathf.Lerp(a.fogDensity, b.fogDensity, t);
			fogBlend					= Mathf.Lerp(a.fogBlend, b.fogBlend, t);
			fogAlbedo					= Color.Lerp(a.fogAlbedo, b.fogAlbedo, t);
			fogDirectIntensity			= Mathf.Lerp(a.fogDirectIntensity, b.fogDirectIntensity, t);
			fogAmbientIntensity			= Mathf.Lerp(a.fogAmbientIntensity, b.fogAmbientIntensity, t);
			fogShadow					= Mathf.Lerp(a.fogShadow, b.fogShadow, t);
			fogHeight					= Mathf.Lerp(a.fogHeight, b.fogHeight, t);
			fogFalloff					= Mathf.Lerp(a.fogFalloff, b.fogFalloff, t);

			for (int i = 0; i < customFloats.Length; i++)
			{
				customFloats[i] = Mathf.Lerp(a.customFloats[i], b.customFloats[i], t);
			}
		}

		public void AddCustomFloat ()
		{
			var tmp = new List<float>(customFloats);
			tmp.Add(0);
			customFloats = tmp.ToArray();
		}

		public void DeleteCustomFloat (int index)
		{
			var tmp = new List<float>(customFloats);
			tmp.RemoveAt(index);
			customFloats = tmp.ToArray();
		}

		public float GetCustomFloat (string name)
		{
			if (customFloats == null || customFloats.Length < 1)
				return 0;

			int index = OverCloud.instance.GetCustomFloatIndex(name);
			if (index > -1)
				return customFloats[index];

			return 0;
		}
	}
	#endregion

	#region ShaderParameters
	/// <summary>
	/// A helper class for managing shader parameters.
	/// </summary>
	public abstract class ShaderParameter
	{
		static List<ShaderParameter> parameters;
		public string name { get; protected set; }
		public ShaderParameter ()
		{
			if (parameters == null)
				parameters = new List<ShaderParameter>();
			parameters.Add(this);
		}
	}

	public class FloatParameter : ShaderParameter
	{
		public FloatParameter (string name) : base()
		{
			this.name = name;
		}

		float	_value;
		public float value
		{
			get
			{
				return _value;
			}
			set
			{
				#if UNITY_EDITOR
					_value = value;
					Shader.SetGlobalFloat(name, _value);
				#else
					if (_value != value)
					{
						_value = value;
						Shader.SetGlobalFloat(name, _value);
					}
				#endif
			}
		}
	}

	public class Vector2Parameter : ShaderParameter
	{
		public Vector2Parameter (string name) : base()
		{
			this.name = name;
		}

		Vector2	_value;
		public Vector2 value
		{
			get
			{
				return _value;
			}
			set
			{
				#if UNITY_EDITOR
					_value = value;
					Shader.SetGlobalVector(name, _value);
				#else
					if (_value != value)
					{
						_value = value;
						Shader.SetGlobalVector(name, _value);
					}
				#endif
			}
		}
	}

	public class Vector3Parameter : ShaderParameter
	{
		public Vector3Parameter (string name) : base()
		{
			this.name = name;
		}

		Vector3	_value;
		public Vector3 value
		{
			get
			{
				return _value;
			}
			set
			{
				#if UNITY_EDITOR
					_value = value;
					Shader.SetGlobalVector(name, _value);
				#else
					if (_value != value)
					{
						_value = value;
						Shader.SetGlobalVector(name, _value);
					}
				#endif
			}
		}
	}

	public class Vector4Parameter : ShaderParameter
	{
		public Vector4Parameter (string name) : base()
		{
			this.name = name;
		}

		Vector4	_value;
		public Vector4 value
		{
			get
			{
				return _value;
			}
			set
			{
				#if UNITY_EDITOR
					_value = value;
					Shader.SetGlobalVector(name, _value);
				#else
					if (_value != value)
					{
						_value = value;
						Shader.SetGlobalVector(name, _value);
					}
				#endif
			}
		}
	}

	public class ColorParameter : ShaderParameter
	{
		public ColorParameter (string name) : base()
		{
			this.name = name;
		}

		Color	_value;
		public Color value
		{
			get
			{
				return _value;
			}
			set
			{
				#if UNITY_EDITOR
					_value = value;
					Shader.SetGlobalColor(name, _value);
				#else
					if (_value != value)
					{
						_value = value;
						Shader.SetGlobalColor(name, _value);
					}
				#endif
			}
		}
	}

	public class TextureParameter : ShaderParameter
	{
		public TextureParameter (string name) : base()
		{
			this.name = name;
		}

		Texture	_value;
		public Texture value
		{
			get
			{
				return _value;
			}
			set
			{
				#if UNITY_EDITOR
					_value = value;
					Shader.SetGlobalTexture(name, _value);
				#else
					if (_value != value)
					{
						_value = value;
						Shader.SetGlobalTexture(name, _value);
					}
				#endif
			}
		}
	}
	#endregion

	/// <summary>
	/// OverCloud is the main component used to drive the sky, atmosphere, lighting and time of day system.
	/// It controls most effects on a global level and is required for the plugin to run.
	/// Only one OverCloud instance can be active at any given time.
	/// </summary>
	[ExecuteInEditMode]
	public class OverCloud : MonoBehaviour
	{
		#region Utilities
		// Unit quad mesh used for some screen drawing
		static Mesh s_Quad;
		public static Mesh quad
		{
			get
			{
				if (s_Quad != null)
					return s_Quad;

				var vertices = new[]
				{
					new Vector3(-1f, -1f, 0f),
					new Vector3( 1f,  1f, 0f),
					new Vector3( 1f, -1f, 0f),
					new Vector3(-1f,  1f, 0f)
				};

				var uvs = new[]
				{
					new Vector2(0f, 0f),
					new Vector2(1f, 1f),
					new Vector2(1f, 0f),
					new Vector2(0f, 1f)
				};

				var indices = new[] { 0, 1, 2, 1, 0, 3 };

				s_Quad = new Mesh
				{
					vertices = vertices,
					uv = uvs,
					triangles = indices
				};
				s_Quad.RecalculateNormals();
				s_Quad.RecalculateBounds();

				return s_Quad;
			}
		}
		#endregion

		#region Drawer variables
		public bool showDrawerCloud;
		public bool showDrawerNoiseGenerator;
		public bool showDrawerCirrus;
		public bool showDrawerAtmosphere;
		public bool showDrawerTimeOfDay;

		public bool showDrawerCloudCompositorTexture;
		public bool showDrawerPhase;
		public bool showDrawer3DNoise;
		public bool showDrawerFog;
		public bool showDrawerScatteringMask;
		public bool showDrawerRendering;
		public bool showDrawerWeather;
		public bool showDrawerWeatherPresets;
		public bool showDrawerCustomFloats;
		public bool showDrawerLighting;

		public int	drawerSelectedCustomFloat;
		public int	drawerSelectedCloudPlane;
		#endregion

		#region Events
		public delegate void OverCloudEventHandler();
		public static event OverCloudEventHandler beforeCameraUpdate;
		public static event OverCloudEventHandler afterCameraUpdate;
		public static event OverCloudEventHandler beforeRender;
		public static event OverCloudEventHandler afterRender;
		public static event OverCloudEventHandler beforeShaderParametersUpdate;
		public static event OverCloudEventHandler afterShaderParametersUpdate;
		#endregion

		#region Public variables
		/// <summary>
		/// The active OverCloud instance.
		/// </summary>
		public static OverCloud			instance { get; private set; }

		/// <summary>
		/// The current floating origin offset.
		/// </summary>
		public static Vector3			currentOriginOffset;

		/// <summary>
		/// The dominant Light (sun if active, otherwise moon).
		/// </summary>
		public static Light				dominantLight			{ get; private set; }

		/// <summary>
		/// The dominant OverCloudLight (sun if active, otherwise moon).
		/// </summary>
		public static OverCloudLight	dominantOverCloudLight	{ get; private set; }

		/// <summary>
		/// The width of the current render target
		/// If there are multiple OverCloudCamera components active, this might change whenever rendering switches to another camera
		/// </summary>
		public static int				bufferWidth				{ get; private set; }

		/// <summary>
		/// The height of the current render target
		/// If there are multiple OverCloudCamera components active, this might change whenever rendering switches to another camera
		/// </summary>
		public static int				bufferHeight			{ get; private set; }

		/// <summary>
		/// The downsampled width of the current render target
		/// </summary>
		public static int				bufferWidthDS			{ get; private set; }

		/// <summary>
		/// The downsampled height of the current render target
		/// </summary>
		public static int				bufferHeightDS			{ get; private set; }

		/// <summary>
		/// The current solar eclipse factor.
		/// </summary>
		public static float			solarEclipse
		{
			get
			{
				if (instance && components.sun && components.moon)
				{
					float relativeSize = 0.0002f;
					float dot = Vector3.Dot(components.sun.transform.forward, components.moon.transform.forward) * 0.5f + 0.5f;
					return Mathf.Clamp01((dot - (1 - relativeSize)) / relativeSize);
				}
				else
				{
					return 0;
				}
			}
		}

		/// <summary>
		/// The current lunar eclipse factor.
		/// </summary>
		public static float			lunarEclipse
		{
			get
			{
				if (instance && components.sun && components.moon)
				{
					float relativeSize = 0.0002f;
					float dot = Vector3.Dot(-components.sun.transform.forward, components.moon.transform.forward) * 0.5f + 0.5f;
					return Mathf.Clamp01((dot - (1 - relativeSize)) / relativeSize);
				}
				else
				{
					return 0;
				}
			}
		}

		/// <summary>
		/// The current moon fade factor.
		/// </summary>
		public static float			moonFade				{ get; private set; }

		/// <summary>
		/// The render texture containing the compositor.
		/// </summary>
		public static RenderTexture	compositorTexture		{ get { return instance.m_CompositorRT; } }

		/// <summary>
		/// 3D noise slices used for previewing the 3D noise.
		/// </summary>
		public RenderTexture[]		_3DNoiseSlice			{ get { return m_3DNoiseSlice; } }

		/// <summary>
		/// The current weather values.
		/// </summary>
		public static WeatherPreset	current					{ get { return instance ? instance.m_CurrentPreset : null; } }

		/// <summary>
		/// Set to true if OverCloud detected the sky changed during the current frame.
		/// </summary>
		public static bool			skyChanged				{ get; private set; }

		/// <summary>
		/// Altitude, adjusted for origin offset
		/// </summary>
		public static float			adjustedCloudPlaneAltitude { get { return (current != null ? current.cloudPlaneAltitude : 0) - currentOriginOffset.y; } }

		/// <summary>
		/// The screen-space volumetric cloud texture. Only set while a camera is rendering.
		/// </summary>
		public static RenderTexture cloudRT				= null;

		/// <summary>
		/// The screen-space volumetric cloud depth texture. Only set while a camera is rendering.
		/// </summary>
		public static RenderTexture cloudDepthRT		= null;

		/// <summary>
		/// The screen-space scattering mask texture. Only set while a camera is rendering.
		/// </summary>
		public static RenderTexture scatteringMaskRT	= null;

		/// <summary>
		/// The screen-space volume light texture. Only set while a camera is rendering.
		/// </summary>
		public static RenderTexture volumeRT			= null;
		#endregion

		#region Inspector variables
		[System.Serializable]
		public class Components
		{
			/// <summary>
			/// The directional light representing the sun.
			/// </summary>
			[Tooltip("The directional light representing the sun.")]
			public Light sun;

			/// <summary>
			/// The directional light representing the moon. OverCloud will automatically fade this out when the sun is active.
			/// </summary>
			[Tooltip("The directional light representing the moon. OverCloud will automatically fade this out when the sun is active.")]
			public Light moon;

			/// <summary>
			/// The material used for rendering the volumetric clouds. Recommended to not modify.
			/// </summary>
			[Tooltip("The material used for rendering the volumetric clouds. Recommended to not modify.")]
			public Material cloudMaterial;

			/// <summary>
			/// The material used for rendering the skybox. Recommended to not modify.
			/// </summary>
			[Tooltip("The material used for rendering the skybox. Recommended to not modify.")]
			public Material skyMaterial;
		}

		[SerializeField]
		Components m_Components = null;
		/// <summary>
		/// References to key components and resources used by OverCloud.
		/// </summary>
		public static Components components { get { return instance.m_Components; } }

		[System.Serializable]
		public class VolumetricClouds
		{
			/// <summary>
			/// The radius of the high quality pass of the volumetric cloud plane. A larger radius will require larger particles and a higher compositor resolution to look the same.
			/// </summary>
			[Tooltip("The radius of the high quality pass of the volumetric cloud plane. A larger radius will require larger particles and a higher compositor resolution to look the same.")]
			[Range(50, 64000)]
			public float				cloudPlaneRadius = 16000;

			/// <summary>
			/// The resolution of the compostior. Affects volumetric cloud quality, shadows and cloud AO.
			/// </summary>
			[Tooltip("The resolution of the compostior. Affects volumetric cloud quality, shadows and cloud AO.")]
			public CompositorResolution	compositorResolution = CompositorResolution._1024x1024;

			/// <summary>
			/// The amount of blur to apply to the compositor texture after rendering it. A value of 0 will probably give you visible artifacts when the clouds move. Also affects cloud shadows and AO.
			/// </summary>
			[Tooltip("The amount of blur to apply to the compositor texture after rendering it. A value of 0 will probably give you visible artifacts when the clouds move. Also affects cloud shadows and AO.")]
			[Range(0, 1)]
			public float				compositorBlur = 0.2f;

			/// <summary>
			/// The noise texture used to render the compositor.
			/// </summary>
			[Tooltip("The noise texture used to render the compositor.")]
			public Texture2D			noiseTexture;

			/// <summary>
			/// The scale of the small noise pass when rendering the compositor.
			/// </summary>
			[Tooltip("The scale of the small noise pass when rendering the compositor.")]
			[Range(0, 1)]
			public float				noiseScale = 0.5f;

			/// <summary>
			/// The scale of the large noise pass when rendering the compositor.
			/// </summary>
			[Tooltip("The scale of the large noise pass when rendering the compositor.")]
			[Range(0, 1)]
			public float				noiseMacroScale = 0.2f;

			/// <summary>
			/// How many particles the volumetric cloud mesh is made up of. Recommended to leave at the maximum value.
			/// </summary>
			[Tooltip("How many particles the volumetric cloud mesh is made up of. Recommended to leave at the maximum value.")]
			[Range(1000, 16000)]
			public int					particleCount = 16000;

			/// <summary>
			/// The radius multiplier for the lower quality volumetric cloud pass, which is rendered before the high-quality pass. Actual lod radius = Cloud Plane Radius * Lod Radius Multiplier.
			/// </summary>
			[Tooltip("The radius multiplier for the lower quality volumetric cloud pass, which is rendered before the high-quality pass. Actual lod radius = Cloud Plane Radius * Lod Radius Multiplier.")]
			[Range(2, 8)]
			public float				lodRadiusMultiplier = 4;

			/// <summary>
			/// Size multiplier for the low quality pass particles. Actual lod particle size = Radius Max * Lod size.
			/// </summary>
			[Tooltip("Size multiplier for the low quality pass particles. Actual lod particle size = Radius Max * Lod size.")]
			[Range(1, 8)]
			public float				lodParticleSize = 2.5f;

			[System.Serializable]
			public class NoiseSettings
			{
				/// <summary>
				/// The tile rate of the first 3D noise pass.
				/// </summary>
				[Tooltip("The tile rate of the first 3D noise pass.")]
				[Range(0, 0.01f)]
				public float			noiseTiling_A = 0.00035f;

				/// <summary>
				/// The intensity of the first 3D noise pass.
				/// </summary>
				[Tooltip("The intensity of the first 3D noise pass.")]
				[Range(0, 1)]
				public float			noiseIntensity_A = 1f;

				/// <summary>
				/// The tile rate of the second 3D noise pass.
				/// </summary>
				[Tooltip("The tile rate of the second 3D noise pass.")]
				[Range(0, 0.01f)]
				public float			noiseTiling_B = 0.0015f;

				/// <summary>
				/// The intensity of the second 3D noise pass.
				/// </summary>
				[Tooltip("The intensity of the second 3D noise pass.")]
				[Range(0, 1)]
				public float			noiseIntensity_B = 0.5f;

				/// <summary>
				/// The placement (along the cloud plane height) of the density peak.
				/// </summary>
				[Tooltip("The placement (along the cloud plane height) of the density peak.")]
				[SerializeField] [Range(0.001f, 0.999f)]
				public float			shapeCenter = 0.3f;

				/// <summary>
				/// Increase the density at the base of the clouds by a set amount.
				/// </summary>
				[Tooltip("Increase the density at the base of the clouds by a set amount.")]
				[SerializeField] [Range(0, 8)]
				public float			baseDensityIncrease = 2;

				/// <summary>
				/// The amount of noise erosion to apply to the clouds.
				/// </summary>
				[Tooltip("The amount of noise erosion to apply to the clouds.")]
				[SerializeField] [Range(0, 2)]
				public float			erosion = 1.1f;

				/// <summary>
				/// The lower edge when smooth-stepping the alpha value per-particle.
				/// </summary>
				[Tooltip("The lower edge when smooth-stepping the alpha value per-particle.")]
				[SerializeField] [Range(0, 1)]
				public float			alphaEdgeLower = 0.015f;

				/// <summary>
				/// The upper edge when smooth-stepping the alpha value per-particle.
				/// </summary>
				[Tooltip("The upper edge when smooth-stepping the alpha value per-particle.")]
				[SerializeField] [Range(0, 1)]
				public float			alphaEdgeUpper = 0.25f;

				/// <summary>
				/// Adds additional scrolling to the 3D noise, making the volumetric clouds appear more turbulent.
				/// </summary>
				[Tooltip("Adds additional scrolling to the 3D noise, making the volumetric clouds appear more turbulent.")]
				[SerializeField] [Range(-1, 1)]
				public float			turbulence = 0.5f;

				/// <summary>
				/// Adds vertical scrolling to the 3D noise.
				/// </summary>
				[Tooltip("Adds vertical scrolling to the 3D noise.")]
				[SerializeField] [Range(-1, 1)]
				public float			riseFactor = 0.25f;
			}

			[SerializeField]
			NoiseSettings m_NoiseSettings = new NoiseSettings();
			/// <summary>
			/// Settings related to the 3D noise erosion.
			/// </summary>
			public NoiseSettings noiseSettings { get { return m_NoiseSettings; } }

			/// <summary>
			/// Class containing all settings for the 3D noise generation.
			/// </summary>
			[System.Serializable]
			public class NoiseGeneration
			{
				public _3DNoiseResolution			resolution;
				public CloudNoiseGen.NoiseSettings	perlin;
				public CloudNoiseGen.NoiseSettings	worley;
			}

			[SerializeField]
			NoiseGeneration m_NoiseGeneration = new NoiseGeneration();
			/// <summary>
			/// Parameters controlling the 3D noise generation.
			/// </summary>
			public NoiseGeneration noiseGeneration { get { return m_NoiseGeneration; } }
		}

		[SerializeField]
		VolumetricClouds m_VolumetricClouds = null;
		/// <summary>
		/// Settings controlling the appearance of the volumetric clouds.
		/// </summary>
		public static VolumetricClouds volumetricClouds { get { return instance.m_VolumetricClouds; } }

		/// <summary>
		/// Class containing all settings for a 2D cloud plane.
		/// </summary>
		[System.Serializable]
		public class CloudPlane
		{
			public string		name;
			public Texture2D	texture;
			public Color		color				= Color.white;
			public float		scale				= 200000;
			public float		detailScale			= 10000;
			public float		height				= 10000;
			[Range(0, 1)]
			public float		opacity				= 1;
			public float		lightPenetration	= 0.5f;
			public float		lightAbsorption		= 1f;
			public float		windTimescale		= 1f;

			public CloudPlane (string name)
			{
				this.name = name;
			}

			public CloudPlane (CloudPlane obj)
			{
				name				= obj.name + "_copy";
				texture				= obj.texture;
				scale				= obj.scale;
				detailScale			= obj.detailScale;
				height				= obj.height;
				opacity				= obj.opacity;
				lightPenetration	= obj.lightPenetration;
				lightAbsorption		= obj.lightAbsorption;
				windTimescale		= obj.windTimescale;
			}
		}
		
		[SerializeField]
		CloudPlane[] m_CloudPlanes;
		public static CloudPlane[] cloudPlanes { get { return instance.m_CloudPlanes; } }

		[System.Serializable]
		public class Atmosphere
		{
			/// <summary>
			/// When checked, OverCloud will override the scene’s skybox material with the OverCloud skybox material. Unless you really, really want your own skybox it is recommended to leave this checked if you want the atmospheric scattering to match the skybox.
			/// </summary>
			[Tooltip("When checked, OverCloud will override the scene’s skybox material with the OverCloud skybox material. Unless you really, really want your own skybox it is recommended to leave this checked if you want the atmospheric scattering to match the skybox.")]
			public bool					overrideSkyboxMaterial = true;

			/// <summary>
			/// The exposure level of the atmospheric scattering. This has been tweaked to appear natural in Unity if left at 1.
			/// </summary>
			[Tooltip("The exposure level of the atmospheric scattering. This has been tweaked to appear natural in Unity if left at 1.")]
			public float				exposure = 1;

			/// <summary>
			/// The density of the atmosphere. Setting this to a value other than 1 will break the physically-based result, but it can be useful if the atmosphere in the scene appears too dense.
			/// </summary>
			[Tooltip("The density of the atmosphere. Setting this to a value other than 1 will break the physically-based result, but it can be useful if the atmosphere in the scene appears too dense.")]
			[Range(0, 8)]
			public float				density = 1;

			/// <summary>
			/// Controls how quickly the scene will fade into the skybox (if at all).
			/// </summary>
			[Tooltip("Controls how quickly the scene will fade into the skybox (if at all).")]
			[Range(0, 1)]
			public float				farClipFade = 1;

			// -------------------- Precomputation Parameters --------------------

			/// <summary>
			/// Class containing all settings for the atmosphere precomputation.
			/// </summary>
			[System.Serializable]
			public class Precomputation
			{
				/// <summary>
				/// The compute shader used to generate the scattering lookup tables. Don't change this!.
				/// </summary>
				[Tooltip("The compute shader used to generate the scattering lookup tables. Don't change this!.")]
				public ComputeShader shader;

				/// <summary>
				/// The size of the planet, measured in earth radii (6 371 km).
				/// </summary>
				[Tooltip("The size of the planet, measured in earth radii (6 371 km).")]
				public float planetScale			= 1f;

				/// <summary>
				/// The height of the atmosphere, measured in earth atmosphere heights (60 km).
				/// </summary>
				[Tooltip("The height of the atmosphere, measured in earth atmosphere heights (60 km).")]
				public float heightScale	= 1f;

				/// <summary>
				/// The Mie density.
				/// </summary>
				[Tooltip("The Mie density.")]
				public float mie					= 1;
				/// <summary>
				/// The Rayleigh density.
				/// </summary>
				[Tooltip("The Rayleigh density.")]
				public float rayleigh				= 1;
				/// <summary>
				/// The ozone density.
				/// </summary>
				[Tooltip("The ozone density.")]
				public float ozone					= 1;
				/// <summary>
				/// The G term of the Mie scattering phase function.
				/// </summary>
				[Tooltip("The G term of the Mie scattering phase function.")]
				public float phase					= 0.8f;
			}

			[SerializeField]
			Precomputation m_Precomputation = new Precomputation();
			/// <summary>
			/// Settings related to the precomputation of the atmospheric scattering.
			/// </summary>
			public Precomputation precomputation { get { return m_Precomputation; } }

			// -------------------- Sun --------------------

			/// <summary>
			/// The color of the sun in the sky. This is different from the color of the sun directional light, in that it should always stay the same as it represents the physical color of the sun.
			/// </summary>
			[Tooltip("The color of the sun in the sky. This is different from the color of the sun directional light, in that it should always stay the same as it represents the physical color of the sun.")]
			public Color				actualSunColor = Color.white;

			/// <summary>
			/// The size of the sun in the sky. A value of 1 = physical size of the sun on earth, however for most cases you probably want to increase it.
			/// </summary>
			[Tooltip("The size of the sun in the sky. A value of 1 = physical size of the sun on earth, however for most cases you probably want to increase it.")]
			public float				sunSize = 5f;

			/// <summary>
			/// The intensity of the sun in the sky. Useful parameter to tweak when authoring bloom.
			/// </summary>
			[Tooltip("The intensity of the sun in the sky. Useful parameter to tweak when authoring bloom.")]
			public float				sunIntensity = 100f;

			/// <summary>
			/// A multiplicative color to apply to the sun during a solar eclipse. This is useful if you don’t want a solar eclipse to block out all light. Actual solar eclipse color = sun color * solar eclipse color. Set to white to disable solar eclipses entirely.
			/// </summary>
			[Tooltip("A multiplicative color to apply to the sun during a solar eclipse. This is useful if you don’t want a solar eclipse to block out all light. Actual solar eclipse color = sun color * solar eclipse color. Set to white to disable solar eclipses entirely.")]
			public Color				solarEclipseColor = new Color(0.06f, 0.06f, 0.06f, 1);

			// -------------------- Moon --------------------

			/// <summary>
			/// The texture used to render the moon celestial body in the sky.
			/// </summary>
			[Tooltip("The texture used to render the moon celestial body in the sky.")]
			public Cubemap				moonAlbedo;

			/// <summary>
			/// The color of the moon in the sky.
			/// </summary>
			[Tooltip("The color of the moon in the sky.")]
			public Color				actualMoonColor = new Color(0.025f, 0.29f, 0.45f, 1);

			/// <summary>
			/// The size of the moon in the sky. A value of 1 = physical size of the moon on earth, however for most cases you probably want to increase it.
			/// </summary>
			[Tooltip("The size of the moon in the sky. A value of 1 = physical size of the moon on earth, however for most cases you probably want to increase it.")]
			public float				moonSize = 4.5f;

			/// <summary>
			/// The intensity of the moon in the sky.
			/// </summary>
			[Tooltip("The intensity of the moon in the sky.")]
			public float				moonIntensity = 15f;

			/// <summary>
			/// A multiplicative color to apply to the moon during a lunar eclipse. Actual lunar eclipse color = moon color * lunar eclipse color. Set to white to disable lunar eclipses entirely.
			/// </summary>
			[Tooltip("A multiplicative color to apply to the moon during a lunar eclipse. Actual lunar eclipse color = moon color * lunar eclipse color. Set to white to disable lunar eclipses entirely.")]
			public Color				lunarEclipseColor = new Color(0.15f, 0.017f, 0f, 1);

			// -------------------- Earth --------------------
		
			/// <summary>
			/// The color of earth's surface in the skybox.
			/// </summary>
			[Tooltip("The color of earth's surface in the skybox.")]
			public Color				earthColor = new Color(0.015f, 0.017f, 0.02f, 1);
		
			// -------------------- Mie Scattering --------------------

			/// <summary>
			/// The intensity of the sun mie scattering effect.
			/// </summary>
			[Tooltip("The intensity of the sun mie scattering effect.")]
			[Range(0, 8)]
			public float				mieScatteringIntensity = 1f;

			/// <summary>
			/// The G parameter of the Heyney-Greenstein phase function when calculating the Mie scattering. Essentially it controls the “width” of the effect.
			/// </summary>
			[Tooltip("The G parameter of the Heyney-Greenstein phase function when calculating the Mie scattering. Essentially it controls the “width” of the effect.")]
			[Range(0, 1)]
			public float				mieScatteringPhase = 0.9f;

			/// <summary>
			/// Same as above, but used specifically for the fog lighting.
			/// </summary>
			[Tooltip("Same as above, but used specifically for the fog lighting.")]
			[Range(0, 1)]
			public float				mieScatteringFogPhase = 0.7f;

			/// <summary>
			/// A distance fade to apply to the Mie scattering effect. This fade is used when the scattering mask is disabled.
			/// </summary>
			[Tooltip("A distance fade to apply to the Mie scattering effect. This fade is used when the scattering mask is disabled.")]
			[Range(0, 1)]
			public float				mieScatteringDistanceFadeA = 0.6f;

			/// <summary>
			/// A distance fade to apply to the Mie scattering effect. This fade is used when the scattering mask is enabled.
			/// </summary>
			[Tooltip("A distance fade to apply to the Mie scattering effect. This fade is used when the scattering mask is enabled.")]
			[Range(0, 1)]
			public float				mieScatteringDistanceFadeB = 0.1f;

			// -------------------- Night --------------------
			/// <summary>
			/// The amount of night-time scattering to apply to the world and skybox. This is a non-physically based effect, but it adds a lot of atmosphere to night-time scenes. The color of the night scattering is based on the moon color.
			/// </summary>
			[Tooltip("The amount of night-time scattering to apply to the world and skybox. This is a non-physically based effect, but it adds a lot of atmosphere to night-time scenes. The color of the night scattering is based on the moon color.")]
			[Range(0, 4)]
			public float				nightScattering = 1f;

			// -------------------- Stars --------------------

			/// <summary>
			/// The space cubemap.
			/// </summary>
			[Tooltip("The space cubemap.")]
			public Cubemap				starsCubemap;

			/// <summary>
			/// The intensity of the space cubemap in the skybox.
			/// </summary>
			[Tooltip("The intensity of the space cubemap in the skybox.")]
			[Range(0, 1)]
			public float				starsIntensity = 1f;

			// -------------------- Scattering Mask --------------------

			/// <summary>
			/// Class containing all settings for the scattering mask.
			/// </summary>
			[System.Serializable]
			public class ScatteringMaskSettings
			{
				/// <summary>
				/// The range (ratio of the volumetric cloud plane radius) of the scattering mask effect.
				/// </summary>
				[Tooltip("The range (ratio of the volumetric cloud plane radius) of the scattering mask effect.")]
				[Range(0, 1)]
				public float		range		= 0.15f;

				/// <summary>
				/// Any height below the “floor” will skip scattering mask rendering. Recommended to leave at the lowest height of your scene.
				/// </summary>
				[Tooltip("Any height below the “floor” will skip scattering mask rendering. Recommended to leave at the lowest height of your scene.")]
				public float		floor		= 0;
				/// <summary>
				/// The softness of the scattering mask.
				/// </summary>
				[Tooltip("The softness of the scattering mask.")]
				[Range(0, 1)]
				public float		softness	= 0.5f;
				/// <summary>
				/// The intensity of the scattering mask.
				/// </summary>
				[Tooltip("The intensity of the scattering mask.")]
				[Range(0, 1)]
				public float		intensity	= 1;
			}

			[SerializeField]
			ScatteringMaskSettings m_ScatteringMask = new ScatteringMaskSettings();
			/// <summary>
			/// Setting related to the rendering of the scattering mask.
			/// </summary>
			public ScatteringMaskSettings scatteringMask { get { return m_ScatteringMask; } }
		}

		[SerializeField]
		Atmosphere m_Atmosphere = null;
		/// <summary>
		/// Settings related to the rendering of the atmosphere and sky.
		/// </summary>
		public static Atmosphere atmosphere { get { return instance.m_Atmosphere; } }

		[System.Serializable]
		public class Lighting
		{
			// -------------------- Cloud Lighting --------------------
			/// <summary>
			/// Class containing all settings for the cloud lighting.
			/// </summary>
			[System.Serializable]
			public class CloudLighting
			{
				/// <summary>
				/// The color of the clouds.
				/// </summary>
				[Tooltip("The color of the clouds.")]
				public Color		albedo				= Color.white;

				/// <summary>
				/// The color of rain clouds.
				/// </summary>
				[Tooltip("The color of rain clouds.")]
				public Color		precipitationAlbedo	= Color.white;

				/// <summary>
				/// The eccentricity of the lighting. A value of 0 will light the clouds based solely on the phase function. A value of 1 will ensure the energy value for each pixel is at least that of the light.
				/// </summary>
				[Tooltip("The eccentricity of the lighting. A value of 0 will light the clouds based solely on the phase function. A value of 1 will ensure the energy value for each pixel is at least that of the light.")]
				[Range(0, 1)]
				public float		eccentricity		= 0.5f;

				/// <summary>
				/// The intensity of the silver lining effect around the light source.
				/// </summary>
				[Tooltip("The intensity of the silver lining effect around the light source.")]
				[Range(0, 4)]
				public float		silverIntensity		= 1f;

				/// <summary>
				/// The spread of the silver lining effect.
				/// </summary>
				[Tooltip("The spread of the silver lining effect.")]
				[Range(0, 4)]
				public float		silverSpread		= 0.2f;

				/// <summary>
				/// A multiplier for the direct lighting of the clouds.
				/// </summary>
				[Tooltip("A multiplier for the direct lighting of the clouds.")]
				[Range(0, 4)]
				public float		direct				= 1f;

				/// <summary>
				/// Increasing this value will increase the (direct) lighting absorption of the clouds.
				/// </summary>
				[Tooltip("Increasing this value will increase the (direct) lighting absorption of the clouds.")]
				[Range(0, 1)]
				public float		directAbsorption	= 1f;

				/// <summary>
				/// A multiplier for the indirect lighting of the clouds.
				/// </summary>
				[Tooltip("A multiplier for the indirect lighting of the clouds.")]
				[Range(0, 4)]
				public float		indirect			= 1f;

				/// <summary>
				/// Increasing this value will increase the (indirect) lighting absorption of the clouds.
				/// </summary>
				[Tooltip("Increasing this value will increase the (indirect) lighting absorption of the clouds.")]
				[Range(0, 1)]
				public float		indirectAbsorption	= 1f;

				/// <summary>
				/// Interpolate between a softer density sample for the indirect lighting.
				/// </summary>
				[Tooltip("Interpolate between a softer density sample for the indirect lighting.")]
				[Range(0, 1)]
				public float		indirectSoftness	= 0.5f;

				/// <summary>
				/// A multiplier for the ambient lighting of the clouds.
				/// </summary>
				[Tooltip("A multiplier for the ambient lighting of the clouds.")]
				[Range(0, 4)]
				public float		ambient				= 1f;

				/// <summary>
				/// Increasing this value will increase the (ambient) lighting absorption of the clouds.
				/// </summary>
				[Tooltip("Increasing this value will increase the (ambient) lighting absorption of the clouds.")]
				[Range(0, 4)]
				public float		ambientAbsorption	= 1f;

				/// <summary>
				/// Can be used to desaturate the ambient lighting contribution. Mostly used if the clouds appear too blue.
				/// </summary>
				[Tooltip("Can be used to desaturate the ambient lighting contribution. Mostly used if the clouds appear too blue.")]
				[Range(0, 1)]
				public float		ambientDesaturation	= 0.5f;

				/// <summary>
				/// The width of the \"sugared powder\" effect (darkened edges when facing away from the light source).
				/// </summary>
				[Tooltip("The width of the \"sugared powder\" effect (darkened edges when facing away from the light source).")]
				[Range(0, 1)]
				public float		powderSize			= 0.2f;

				/// <summary>
				/// The intensity of the \"sugared powder\" effect (darkened edges when facing away from the light source).
				/// </summary>
				[Tooltip("The intensity of the \"sugared powder\" effect (darkened edges when facing away from the light source).")]
				[Range(0, 1)]
				public float		powderIntensity		= 0.4f;

				ColorParameter		_OC_CloudAlbedo;
				ColorParameter		_OC_CloudPrecipitationAlbedo;
				Vector4Parameter	_OC_CloudParams1;
				Vector4Parameter	_OC_CloudParams2;
				Vector4Parameter	_OC_CloudParams3;

				public CloudLighting ()
				{
					// Initialize shader parameters
					_OC_CloudAlbedo  = new ColorParameter("_OC_CloudAlbedo");
					_OC_CloudPrecipitationAlbedo = new ColorParameter("_OC_CloudPrecipitationAlbedo");
					_OC_CloudParams1 = new Vector4Parameter("_OC_CloudParams1");
					_OC_CloudParams2 = new Vector4Parameter("_OC_CloudParams2");
					_OC_CloudParams3 = new Vector4Parameter("_OC_CloudParams3");
				}

				public void UpdateShaderProperties ()
				{
					_OC_CloudAlbedo.value  = albedo;
					_OC_CloudPrecipitationAlbedo.value = precipitationAlbedo;
					_OC_CloudParams1.value = new Vector4(eccentricity, silverIntensity, silverSpread, direct);
					_OC_CloudParams2.value = new Vector4(indirect, ambient, Mathf.Pow(directAbsorption, 8), Mathf.Pow(indirectAbsorption, 8));
					_OC_CloudParams3.value = new Vector4(indirectSoftness, Mathf.Pow(ambientAbsorption, 8), powderSize, powderIntensity);
				}
			}

			[SerializeField]
			CloudLighting m_CloudLighting = null;
			/// <summary>
			/// Settings related to the lighting of the clouds.
			/// </summary>
			public CloudLighting cloudLighting { get { return m_CloudLighting; } }

			// -------------------- Ambient Lighting --------------------
			/// <summary>
			/// Class containing settings related to the ambient environment lighting.
			/// </summary>
			[System.Serializable]
			public class Ambient
			{
				/// <summary>
				/// The sky color of the ambient gradient over time (OverCloud samples these color gradients based on the elevation of the sun, not the hour of the day).
				/// </summary>
				[Tooltip("The sky color of the ambient gradient over time (OverCloud samples these color gradients based on the elevation of the sun, not the hour of the day).")]
				public Gradient				sky;

				/// <summary>
				/// The equator color of the ambient gradient over time.
				/// </summary>
				[Tooltip("The equator color of the ambient gradient over time.")]
				public Gradient				equator;

				/// <summary>
				/// The equator color of the ambient gradient over time.
				/// </summary>
				[Tooltip("The equator color of the ambient gradient over time.")]
				public Gradient				ground;

				/// <summary>
				/// How much the color of the moon influences the ambient lighting during a lunar eclipse.
				/// </summary>
				[Tooltip("How much the color of the moon influences the ambient lighting during a lunar eclipse.")]
				[Range(0, 1)]
				public float				lunarEclipseLightingInfluence = 0.2f;

				/// <summary>
				/// The intensity of the ambient lighting.
				/// </summary>
				[Tooltip("The intensity of the ambient lighting.")]
				[Range(0, 4)]
				public float				multiplier = 1.75f;
			}

			[SerializeField]
			Ambient m_Ambient = new Ambient();
			/// <summary>
			/// Settings related to the ambient environment lighting.
			/// </summary>
			public Ambient ambient { get { return m_Ambient; } }

			// -------------------- Cloud Shadows --------------------
			/// <summary>
			/// Class containing settings related to the cloud shadows.
			/// </summary>
			[System.Serializable]
			public class CloudShadows
			{
				/// <summary>
				/// Whether to render cloud shadows or not.
				/// </summary>
				[Tooltip("Whether to render cloud shadows or not. If unchecked, cloud shadows will not appear, no matter the intensity.")]
				public bool					enabled = true;
				/// <summary>
				/// Whether to automatically inject cloud shadows in the screenspace shadows mask or not. For deferred rendering, it is better to swap out the deferred shader. Please see the documentation for info on how to do this.
				/// </summary>
				[Tooltip("Whether to automatically inject cloud shadows in the screenspace shadows mask or not. For deferred rendering, it is better to swap out the deferred shader. Please see the documentation for info on how to do this.")]
				public CloudShadowsMode		mode = CloudShadowsMode.Injected;

				/// <summary>
				/// The resolution of the cloud shadows buffer.
				/// </summary>
				[Tooltip("The resolution of the cloud shadows buffer.")]
				public ShadowsResolution	resolution = ShadowsResolution._512x512;

				/// <summary>
				/// The relative size of the compositor covered by the cloud shadows. This value is rounded to a value which will maintain the texel ratio between the cloud shadows and the compostior texture to prevent shadows from shimmering when the camera moves.
				/// </summary>
				[Tooltip("The relative size of the compositor covered by the cloud shadows. This value is rounded to a value which will maintain the texel ratio between the cloud shadows and the compostior texture to prevent shadows from shimmering when the camera moves.")]
				[Range(0, 1)]
				public float				coverage = 0.25f;

				/// <summary>
				/// How much blur to apply to the volumetric cloud shadows.
				/// </summary>
				[Tooltip("How much blur to apply to the volumetric cloud shadows.")]
				[Range(0, 1)]
				public float				blur = 0.25f;	

				/// <summary>
				/// A tiling texture which is used to refine the edges of the cloud shadows, making them appear higher-resolution.
				/// </summary>
				[Tooltip("A tiling texture which is used to refine the edges of the cloud shadows, making them appear higher-resolution.")]
				public Texture2D			edgeTexture;

				/// <summary>
				/// The tile factor of the cloud shadows edge texture.
				/// </summary>
				[Tooltip("The tile factor of the cloud shadows edge texture.")]
				[Range(0, 1)]
				public float				edgeTextureScale = 0.35f;

				/// <summary>
				/// The intensity of the cloud shadows edge refinement.
				/// </summary>
				[Tooltip("The intensity of the cloud shadows edge refinement.")]
				[Range(0, 1)]
				public float				edgeTextureIntensity = 0.5f;

				/// <summary>
				/// A sharpen factor applied after blurring and refining the cloud shadows.
				/// </summary>
				[Tooltip("A sharpen factor applied after blurring and refining the cloud shadows.")]
				[Range(0, 1)]
				public float				sharpen = 0f;
			}

			[SerializeField]
			CloudShadows m_CloudShadows = new CloudShadows();
			/// <summary>
			/// Settings related to the cloud shadows.
			/// </summary>
			public CloudShadows cloudShadows { get { return m_CloudShadows; } }

			// -------------------- Cloud AO --------------------
			/// <summary>
			/// Class containing settings related to the cloud ambient occlusion.
			/// </summary>
			[System.Serializable]
			public class CloudAmbientOcclusion
			{
				/// <summary>
				/// The intensity of the cloud ambient occlusion effect.
				/// </summary>
				[Tooltip("The intensity of the cloud ambient occlusion effect.")]
				[Range(0, 8)]
				public float				intensity = 1.5f;

				/// <summary>
				/// How far down below the cloud layer the cloud ambient occlusion will extend before fading out completely.
				/// </summary>
				[Tooltip("How far down below the cloud layer the cloud ambient occlusion will extend before fading out completely.")]
				public float				heightFalloff = 5000;
			}

			[SerializeField]
			CloudAmbientOcclusion m_CloudAmbientOcclusion = new CloudAmbientOcclusion();
			/// <summary>
			/// Settings related to the cloud ambient occlusion.
			/// </summary>
			public CloudAmbientOcclusion cloudAmbientOcclusion { get { return m_CloudAmbientOcclusion; } }
		}

		[SerializeField]
		Lighting m_Lighting = null;
		/// <summary>
		/// Settings related to the lighting.
		/// </summary>
		public static Lighting lighting { get { return instance.m_Lighting; } }

		/// <summary>
		/// Class containing settings related to weather effects.
		/// </summary>
		[System.Serializable]
		public class Weather
		{
			// -------------------- Wind --------------------
			/// <summary>
			/// The current wind time. This is used to drive the positions of the clouds.
			/// </summary>
			public float				windTime;

			/// <summary>
			/// The timescale for the wind. Should probably be left at 1 unless you want the appearance of time moving at an increased rate.
			/// </summary>
			[Tooltip("The timescale for the wind. Should probably be left at 1 unless you want the appearance of time moving at an increased rate.")]
			[Range(0, 100)]
			public float				windTimescale = 1f;

			[System.Serializable]
			public class Rain
			{
				// -------------------- Rain Mask Rendering --------------------
				/// <summary>
				/// The resolution of the world-space mask volume.
				/// </summary>
				[Tooltip("The resolution of the world-space mask volume.")]
				public RainMaskResolution	maskResolution = RainMaskResolution._1024x1024;

				/// <summary>
				/// A layermask specifying which objects should be rendered into the rain mask.
				/// </summary>
				[Tooltip("A layermask specifying which objects should be rendered into the rain mask.")]
				public LayerMask			maskLayers = 1 << 0;

				/// <summary>
				/// The world-space radius of the rain mask coverage.
				/// </summary>
				[Tooltip("The world-space radius of the rain mask coverage.")]
				public float				maskRadius = 20;

				// -------------------- Rain Mask Sampling --------------------
				/// <summary>
				/// The height falloff of the rain mask. A higher value will give a smoother fade, but will increase the height of when objects start to occlude surfaces beneath them.
				/// </summary>
				[Tooltip("The height falloff of the rain mask. A higher value will give a smoother fade, but will increase the height of when objects start to occlude surfaces beneath them.")]
				public float				maskFalloff = 1f;

				/// <summary>
				/// Apply an optional blur to the rain mask. If set to 0, will skip the blur pass, which is slightly faster.
				/// </summary>
				[Tooltip("Apply an optional blur to the rain mask. If set to 0, will skip the blur pass, which is slightly faster.")]
				[Range(0, 4)]
				public float				maskBlur = 0.25f;

				/// <summary>
				/// A textured used to add some local noise to the rain mask sampling.
				/// </summary>
				[Tooltip("A textured used to add some local noise to the rain mask sampling.")]
				public Texture				maskOffsetTexture;

				/// <summary>
				/// The amount of noise to apply to the rain mask sampling.
				/// </summary>
				[Tooltip("The amount of noise to apply to the rain mask sampling.")]
				[Range(0, 2)]
				public float				maskOffset = 10;

				// -------------------- Rain Normals, Albedo & Gloss --------------------
				/// <summary>
				/// (Deferred rendering only) How much to darken wet surfaces.
				/// </summary>
				[Tooltip("(Deferred rendering only) How much to darken wet surfaces.")]
				[Range(0, 1)]
				public float				albedoDarken = 0.35f;

				/// <summary>
				/// (Deferred rendering only) How much to decrease the roughness of wet surfaces.
				/// </summary>
				[Tooltip("(Deferred rendering only) How much to decrease the roughness of wet surfaces.")]
				[Range(0, 1)]
				public float				roughnessDecrease = 0.75f;

				/// <summary>
				/// The texture used to drive the rain ripples effect. Should probably never be changed.
				/// </summary>
				[Tooltip("The texture used to drive the rain ripples effect. Should probably never be changed.")]
				public Texture				rippleTexture;

				/// <summary>
				/// The texture used to drive the vertical rain flow effect.
				/// </summary>
				[Tooltip("The texture used to drive the vertical rain flow effect.")]
				public Texture				flowTexture;

				/// <summary>
				/// (Deferred rendering only) The intensity of the rain ripple effect.
				/// </summary>
				[Tooltip("(Deferred rendering only) The intensity of the rain ripple effect.")]
				[Range(0, 1)]
				public float				rippleIntensity = 1f;

				/// <summary>
				/// (Deferred rendering only) The scale of the rain ripple effect.
				/// </summary>
				[Tooltip("(Deferred rendering only) The scale of the rain ripple effect.")]
				[Range(0, 1)]
				public float				rippleScale = 1f;

				/// <summary>
				/// (Deferred rendering only) The timescale of the rain ripple effect.
				/// </summary>
				[Tooltip("(Deferred rendering only) The timescale of the rain ripple effect.")]
				[Range(0, 1)]
				public float				rippleTimescale = 0.3f;

				/// <summary>
				/// (Deferred rendering only) The intensity of the rain flow effect.
				/// </summary>
				[Tooltip("(Deferred rendering only) The intensity of the rain flow effect.")]
				[Range(0, 1)]
				public float				flowIntensity = 1f;

				/// <summary>
				/// (Deferred rendering only) The scale of the rain flow effect.
				/// </summary>
				[Tooltip("(Deferred rendering only) The scale of the rain flow effect.")]
				[Range(0, 1)]
				public float				flowScale = 1f;

				/// <summary>
				/// (Deferred rendering only) The timescale of the rain ripple effect.
				/// </summary>
				[Tooltip("(Deferred rendering only) The timescale of the rain ripple effect.")]
				[Range(0, 1)]
				public float				flowTimescale = 0.4f;
			}
			
			[SerializeField]
			Rain m_Rain = new Rain();
			/// <summary>
			/// Settings related to the rain weather effect.
			/// </summary>
			public Rain rain { get { return m_Rain; } }

			// -------------------- Lightning --------------------

			/// <summary>
			/// Class containing all settings for the lightning effect.
			/// </summary>
			[System.Serializable]
			public class LightningSettings
			{
				/// <summary>
				/// The lightning effect GameObject. This is re-enabled when a lightning strike occurs, so your script should use OnEnable as a play function.
				/// </summary>
				[Tooltip("The lightning effect GameObject. This is re-enabled when a lightning strike occurs, so your script should use OnEnable as a play function.")]
				public GameObject	gameObject;

				/// <summary>
				/// The minimum distance at which lightning strikes will appear from the camera.
				/// </summary>
				[Tooltip("The minimum distance at which lightning strikes will appear from the camera.")]
				public float		distanceMin = 1000;

				/// <summary>
				/// The maximum distance at which lightning strikes will appear from the camera.
				/// </summary>
				[Tooltip("The maximum distance at which lightning strikes will appear from the camera.")]
				public float		distanceMax = 10000;

				/// <summary>
				/// Bias lightning strikes towards being in front of the camera. 0 = No bias. 1 = Always right in front.
				/// </summary>
				[Tooltip("Bias lightning strikes towards being in front of the camera. 0 = No bias. 1 = Always right in front.")]
				[Range(0, 1)]
				public float		cameraBias = 0.75f;

				/// <summary>
				/// Lightning strikes will only occur where the cloud density is higher than this value.
				/// </summary>
				[Tooltip("Lightning strikes will only occur where the cloud density is higher than this value.")]
				[Range(0, 1)]
				public float		minimumDensity = 0.75f;

				/// <summary>
				/// The minimum amount of time between lightning strikes.
				/// </summary>
				[Tooltip("The minimum amount of time between lightning strikes.")]
				public float		intervalMin = 4f;

				/// <summary>
				/// The maximum amount of time between lightning strikes.
				/// </summary>
				[Tooltip("The maximum amount of time between lightning strikes.")]
				public float		intervalMax = 20f;

				/// <summary>
				/// The odds that another strike will occur right after another.
				/// </summary>
				[Tooltip("The odds that another strike will occur right after another.")]
				[Range(0, 1)]
				public float		restrikeChance = 0.15f;

				/// <summary>
				/// Enable lightning effects in the editor (your lightning script also needs to support playing in the editor).
				/// </summary>
				[Tooltip("Enable lightning effects in the editor (your lightning script also needs to support playing in the editor).")]
				public bool			enableInEditor = true;
			}

			[SerializeField]
			LightningSettings m_Lightning = new LightningSettings();
			/// <summary>
			/// Settings related to the lightning weather effect.
			/// </summary>
			public LightningSettings lightning { get { return m_Lightning; } }
		}

		[SerializeField]
		Weather m_Weather = null;
		/// <summary>
		/// Settings related to weather effects.
		/// </summary>
		public static Weather weather { get { return instance.m_Weather; } }

		/// <summary>
		/// Class containing all settings for the time of day system.
		/// </summary>
		[System.Serializable]
		public class TimeOfDay
		{
			/// <summary>
			/// When checked, will override the sun and moon positions with the ones calculated from the current latitude, longitude, date and time.
			/// </summary>
			[Tooltip("When checked, will override the sun and moon positions with the ones calculated from the current latitude, longitude, date and time.")]
			public bool					enable;

			/// <summary>
			/// When checked, the moon will be positioned in the sky according to its physical position at that time. Uncheck if you'd like to have a fixed moon in the sky.
			/// </summary>
			[Tooltip("When checked, the moon will be positioned in the sky according to its physical position at that time. Uncheck if you'd like to have a fixed moon in the sky.")]
			public bool					affectsMoon = true;

			/// <summary>
			/// When checked, will override the date and time with that of the local computer.
			/// </summary>
			[Tooltip("When checked, will override the date and time with that of the local computer.")]
			public bool					useLocalTime;

			/// <summary>
			/// Whether to move time forwards automatically or not. Date will be moved forwards automatically when time goes back down to 0.
			/// </summary>
			[Tooltip("Whether to move time forwards automatically or not. Date will be moved forwards automatically when time goes back down to 0.")]
			public bool play			= true;

			/// <summary>
			/// Enable the Play feature in the editor.
			/// </summary>
			[Tooltip("Enable the Play feature in the editor.")]
			public bool playInEditor	= false;

			/// <summary>
			/// The latitude coordinate of the camera.
			/// </summary>
			[Tooltip("The latitude coordinate of the camera.")]
			[Range(-90, 90)]
			public float latitude		= 0;

			/// <summary>
			/// The longitude coordinate of the camera.
			/// </summary>
			[Tooltip("The longitude coordinate of the camera.")]
			[Range(-180, 180)]
			public float longitude		= 0;

			/// <summary>
			/// The year.
			/// </summary>
			[Tooltip("The year.")]
			public int year				= 1992;

			/// <summary>
			/// The month.
			/// </summary>
			[Tooltip("The month.")]
			[Range(1, 12)]
			public int month			= 1;

			/// <summary>
			/// The day.
			/// </summary>
			[Tooltip("The day.")]
			public int day				= 8;

			/// <summary>
			/// The time (in hours, meaning a value of 0.5 is equal to 30 minutes, etc).
			/// </summary>
			[Tooltip("The time (in hours, meaning a value of 0.5 is equal to 30 minutes, etc).")]
			[Range(0, 24)]
			public double time			= 12;

			/// <summary>
			/// The speed at which the time of day moves when Play is enabled. A value of 1 is realtime.
			/// </summary>
			[Tooltip("The speed at which the time of day moves when Play is enabled. A value of 1 is realtime.")]
			public float playSpeed		= 10;

			/// <summary>
			/// The "day number" used to update orbital bodies.
			/// </summary>
			public float dayNumber
			{
				get
				{
					float d = 367*year - 7 * ( year + (month+9)/12 ) / 4 + 275*month/9 + day - 730530;
					return (float)(d + time / 24.0);
				}
			}

			/// <summary>
			/// How many days are there in the current month?
			/// </summary>
			public int daysInMonth
			{
				get
				{
					return DateTime.DaysInMonth (year, month);
				}
			}

			/// <summary>
			/// Step time forwards.
			/// </summary>
			public void Advance ()
			{
				time += Time.deltaTime * (1.0 / 86400.0) * playSpeed;
				if (time > 24)
				{
					day++;
					time -= 24;
				}
				if (day > daysInMonth)
				{
					month++;
					day = 0;
				}
				if (month > 12)
				{
					year++;
					month = 1;
				}
			}

			/// <summary>
			/// The current hour (in 0-24 format)
			/// </summary>
			public int hour
			{
				get
				{
					int h = Mathf.FloorToInt((float)time);
					return h;
				}
			}

			/// <summary>
			/// The current minute (in 0-60 format)
			/// </summary>
			public int minute
			{
				get
				{
					int m = (int)(time * 1440.0) % 60;
					return m;
				}
			}

			/// <summary>
			/// The current second (inf 0-60 format)
			/// </summary>
			public int second
			{
				get
				{
					int s = (int)(time * 86400.0) % 60;
					//s = (int)(((float)s / 60f - Mathf.Floor((float)s / 60f)) * 60f);
					return s;
				}
			}
		}

		[SerializeField]
		TimeOfDay m_TimeOfDay = null;
		/// <summary>
		/// Settings related to the time of day.
		/// </summary>
		public static TimeOfDay timeOfDay { get { return instance.m_TimeOfDay; } }
		#endregion variables

		#region Weather Preset
		[Tooltip("The name of the current active weather preset.")]
		public string				activePreset = "None";

		[Tooltip("The time in seconds for the weather to fully fade to a new preset.")]
		public float				fadeDuration = 10f;

		[Tooltip("Same as above, but when the game is not running.")]
		public float				editorFadeDuration = 10f;

		[SerializeField]
		CustomFloat[]				m_CustomFloats;

		[SerializeField]
		WeatherPreset[]				m_Presets = null;
		#endregion

		#region Private variables
		[SerializeField]
		WeatherPreset						m_CurrentPreset;

		AtmosphereModel						m_AtmosphereModel;
		OverCloudLight						m_OverCloudSun;
		OverCloudLight						m_OverCloudMoon;
		GameObject							m_CloudObject;
		GameObject							m_LodObject;
		MeshFilter							m_Filter;
		MeshFilter							m_LodFilter;
		MeshRenderer						m_Renderer;
		MeshRenderer						m_LodRenderer;
		MaterialPropertyBlock				m_PropBlock;
		MaterialPropertyBlock				m_LodPropBlock;
		Dictionary<Camera, CommandBuffer>	m_CameraBuffers		= new Dictionary<Camera, CommandBuffer>();
		Dictionary<Camera, CommandBuffer>	m_CameraPreBuffers	= new Dictionary<Camera, CommandBuffer>();
		Dictionary<Camera, CommandBuffer>	m_CameraPostBuffers	= new Dictionary<Camera, CommandBuffer>();
		Dictionary<Camera, CommandBuffer>	m_VolumeBuffers		= new Dictionary<Camera, CommandBuffer>();
		Dictionary<Camera, CommandBuffer>	m_OcclusionBuffers	= new Dictionary<Camera, CommandBuffer>();
		Dictionary<Camera, CommandBuffer>	m_WetnessBuffers	= new Dictionary<Camera, CommandBuffer>();
		Dictionary<Light,  CommandBuffer>	m_ShadowBuffers		= new Dictionary<Light,  CommandBuffer>();
		Camera								m_RainCamera;

		Dictionary<Camera, RenderTexture>	m_DownsampledDepthRTs;
		Dictionary<Camera, RenderTexture>	m_CloudRTs;
		Dictionary<Camera, RenderTexture>	m_CloudDepthRTs;
		Dictionary<Camera, RenderTexture>	m_ScatteringMasks;
		Dictionary<Camera, RenderTexture>	m_VolumeRTs;
		RenderTexture						m_CompositorRT;
		RenderTexture						m_CloudShadowsRT;
		RenderTexture						m_RainMask;
		RenderTexture						m_RainRippleRT;	

		Texture3D							m_3DNoise;
		RenderTexture[]						m_3DNoiseSlice = new RenderTexture[3];
		Material							m_CompositorMat;
		Material							m_UtilitiesMat;
		Material							m_DownsampleDepthMat;
		Material							m_UpsampleMat;
		Material							m_ClearMat;
		Material							m_ScatteringMaskRTMat;
		Material							m_AtmosphereMat;
		Material							m_SeparableBlurMat;
		Material							m_RainRippleMat;
		CompositorResolution				m_LastCompositorRes;
		WeatherPreset						m_PrevPreset;
		WeatherPreset						m_TargetPreset;

		WeatherPreset						m_LastFramePreset;
		TimeOfDay							m_LastFrameTimeOfDay;
		Atmosphere.Precomputation	m_LastAtmosphere;
		Vector3								m_LastPos = new Vector3(0, -99999, 0);
		Vector3								m_LastLodPos = new Vector3(0, -99999, 0);

		Rect								m_WorldExtents;
		float								m_FadeTimer;
		float								m_LastRadius;
		float								m_LastLodMultiplier;
		float								m_LightningTimer;
		bool								m_LightningRestrike;
		float								m_LST;

		// Used to render cloud AO to deferred ambient
		readonly RenderTargetIdentifier[] m_OcclusionMRT =
        {
            BuiltinRenderTextureType.GBuffer0, // Albedo, Occ
            BuiltinRenderTextureType.CameraTarget // Ambient
        };

		// Used to render wetness to albedo and gloss
		readonly RenderTargetIdentifier[] m_WetnessMRT =
        {
            BuiltinRenderTextureType.GBuffer0, // Albedo, Occ
            BuiltinRenderTextureType.GBuffer1, // Specular + roughness
			BuiltinRenderTextureType.CameraTarget // Ambient
        };
		#endregion

		#region Initialization
		void Awake ()
		{
			// We want to make instance available to other scripts ASAP
			if (instance && instance != this)
				Debug.LogError("Multiple OverCloud instances found");
			instance = this;

			m_CameraBuffers		= new Dictionary<Camera, CommandBuffer>();
			m_CameraPreBuffers	= new Dictionary<Camera, CommandBuffer>();
			m_CameraPostBuffers = new Dictionary<Camera, CommandBuffer>();
			m_OcclusionBuffers	= new Dictionary<Camera, CommandBuffer>();
			m_WetnessBuffers	= new Dictionary<Camera, CommandBuffer>();
			m_VolumeBuffers		= new Dictionary<Camera, CommandBuffer>();

			m_WorldExtents = new Rect(new Vector2(0, 0), Vector2.one * volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2);

			// Initialize the atmospheric model (this will execute the compute shaders, so it is quite expensive)
			InitializeAtmosphere();
		}

		void TryLoadShader (string name, out Material material)
		{
			var shader = Shader.Find(name);
			if (!shader)
			{
				Debug.LogError("OverCloud fatal error: Unable to find shader " + name);
				material = null;
				return;
			}
			material = new Material(shader);
			if (!material)
				Debug.LogError("Unable to load shader " + name + ", (file accidentally deleted?).");
		}

		void OnEnable ()
		{
			// Make SURE instance is set
			if (instance && instance != this)
				Debug.LogError("Multiple OverCloud instances found");
			instance = this;

			if (m_AtmosphereModel == null || !m_AtmosphereModel.initialized)
				InitializeAtmosphere();

			// Load shaders
			TryLoadShader("Hidden/OverCloud/Compositor",		out m_CompositorMat);
			TryLoadShader("Hidden/OverCloud/Utilities",			out m_UtilitiesMat);
			TryLoadShader("Hidden/OverCloud/DownsampleDepth",	out m_DownsampleDepthMat);
			TryLoadShader("Hidden/OverCloud/DepthUpsampling",	out m_UpsampleMat);
			TryLoadShader("Hidden/OverCloud/ScatteringMask",	out m_ScatteringMaskRTMat);
			TryLoadShader("Hidden/OverCloud/Clear",				out m_ClearMat);
			TryLoadShader("Hidden/OverCloud/Atmosphere",		out m_AtmosphereMat);
			TryLoadShader("Hidden/OverCloud/SeparableBlur",		out m_SeparableBlurMat);			

			UpdateShaderProperties();
			CheckComponents();
			InitializeMeshes();

			if (m_3DNoise == null)
				InitializeNoise();

			if (components.cloudMaterial != null)
			{
				m_Renderer.sharedMaterial		= components.cloudMaterial;
				m_LodRenderer.sharedMaterial	= components.cloudMaterial;
			}

			m_Renderer.enabled		= true;
			m_LodRenderer.enabled	= true;

			InitializeCompositor();
			InitializeWeather();

			if (m_DownsampledDepthRTs	== null) m_DownsampledDepthRTs	= new Dictionary<Camera, RenderTexture>();
			if (m_CloudRTs				== null) m_CloudRTs				= new Dictionary<Camera, RenderTexture>();
			if (m_CloudDepthRTs			== null) m_CloudDepthRTs		= new Dictionary<Camera, RenderTexture>();
			if (m_ScatteringMasks		== null) m_ScatteringMasks		= new Dictionary<Camera, RenderTexture>();
			if (m_VolumeRTs				== null) m_VolumeRTs			= new Dictionary<Camera, RenderTexture>();

			FindTargetPreset();
			m_CurrentPreset = new WeatherPreset(m_TargetPreset);
			m_PrevPreset	= new WeatherPreset(m_CurrentPreset);

			// Reset the wind time
			weather.windTime = 0;

			// Reset floating origin if in editor + scene view
			if (!Application.isPlaying)
				ResetOrigin();
		}

		void OnDisable ()
		{
			if (m_Renderer)
				m_Renderer.enabled		= false;
			if (m_LodRenderer)
				m_LodRenderer.enabled	= false;

			// Clear buffers
			foreach (var buffer in m_CameraBuffers)
			{
				if (buffer.Key)
					// CameraEvent.AfterSkybox will probably give better compatibility with other assets,
					// unfortunately, Unity does not execute this event in the same order in the scene view,
					// resulting in lack of clouds.
					buffer.Key.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, buffer.Value);
			}
			foreach (var buffer in m_CameraPreBuffers)
			{
				if (buffer.Key)
					buffer.Key.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, buffer.Value);
			}
			foreach (var buffer in m_CameraPreBuffers)
			{
				if (buffer.Key)
					buffer.Key.RemoveCommandBuffer(CameraEvent.AfterLighting, buffer.Value);
			}
			foreach (var buffer in m_CameraPostBuffers)
			{
				if (buffer.Key)
					buffer.Key.RemoveCommandBuffer(CameraEvent.AfterEverything, buffer.Value);
			}
			foreach (var buffer in m_VolumeBuffers)
			{
				if (buffer.Key)
					buffer.Key.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, buffer.Value);
			}
			foreach (var buffer in m_OcclusionBuffers)
			{
				if (buffer.Key)
					buffer.Key.RemoveCommandBuffer(CameraEvent.BeforeReflections, buffer.Value);
			}
			foreach (var buffer in m_WetnessBuffers)
			{
				if (buffer.Key)
					buffer.Key.RemoveCommandBuffer(CameraEvent.BeforeReflections, buffer.Value);
			}
			foreach (var buffer in m_ShadowBuffers)
			{
				if (buffer.Key)
					buffer.Key.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, buffer.Value);
			}

			m_CameraBuffers.Clear();
			m_CameraPreBuffers.Clear();
			m_CameraPostBuffers.Clear();
			m_VolumeBuffers.Clear();
			m_OcclusionBuffers.Clear();
			m_WetnessBuffers.Clear();
			m_ShadowBuffers.Clear();
		}

		/// <summary>
		/// Attempt to find a matching preset, given the active preset string
		/// </summary>
		void FindTargetPreset ()
		{
			m_TargetPreset = null;
			foreach (var preset in m_Presets)
			{
				if (preset.name == activePreset)
				{
					m_TargetPreset = preset;
					return;
				}
			}
		}

		void InitializeCompositor ()
		{
			var desc = new RenderTextureDescriptor((int)volumetricClouds.compositorResolution, (int)volumetricClouds.compositorResolution, RenderTextureFormat.ARGB32, 0);
			desc.useMipMap = true;
			desc.autoGenerateMips = true;
			desc.sRGB = false;
			m_CompositorRT = new RenderTexture(desc.width, desc.height, desc.depthBufferBits, desc.colorFormat, RenderTextureReadWrite.Linear);
			m_CompositorRT.filterMode = FilterMode.Bilinear;
			m_LastCompositorRes = volumetricClouds.compositorResolution;
		}

		void InitializeAtmosphere ()
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				// Skip if headless server
				return;
			}

			if (m_AtmosphereModel == null)
				m_AtmosphereModel = new AtmosphereModel();
			m_AtmosphereModel.m_compute = atmosphere.precomputation.shader;
			m_AtmosphereModel.planetScale = atmosphere.precomputation.planetScale;
			m_AtmosphereModel.heightScale = atmosphere.precomputation.heightScale;
			m_AtmosphereModel.Initialize(atmosphere.precomputation);
		}

		void InitializeWeather ()
		{
			RenderTextureDescriptor desc = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGB32, 0);
			desc.autoGenerateMips = true;
			desc.useMipMap = true;
			m_RainRippleRT = new RenderTexture(desc);
			m_RainRippleRT.wrapMode = TextureWrapMode.Repeat;
			m_RainRippleRT.filterMode = FilterMode.Bilinear;
			TryLoadShader("Hidden/OverCloud/RippleNormals", out m_RainRippleMat);

			m_LightningTimer = Random.Range(weather.lightning.intervalMin, weather.lightning.intervalMax);

			if (!m_RainCamera)
			{
				var go = new GameObject("Rain Camera");
				go.hideFlags = HideFlags.HideAndDontSave;
				m_RainCamera = go.AddComponent<Camera>();
				m_RainCamera.enabled = false;
			}
		}

		public static void MoveOrigin (Vector3 offset)
		{
			currentOriginOffset += offset;
			instance.m_LastPos += offset;
			instance.m_LastLodPos += offset;
			Shader.SetGlobalVector("_OverCloudOriginOffset", currentOriginOffset);
		}

		public static void ResetOrigin ()
		{
			if (instance)
			{
				instance.m_LastPos -= currentOriginOffset;
				instance.m_LastLodPos -= currentOriginOffset;
			}
			currentOriginOffset = Vector3.zero;
			Shader.SetGlobalVector("_OverCloudOriginOffset", currentOriginOffset);
		}

		void OnValidate ()
		{
			if (!instance)
				return;

			if (volumetricClouds.compositorResolution != m_LastCompositorRes)
				InitializeCompositor();

			// Sanity check some stuff
			timeOfDay.day = Mathf.Clamp(timeOfDay.day, 1, timeOfDay.daysInMonth);
			weather.rain.maskFalloff = Mathf.Max(weather.rain.maskFalloff, 0.01f);

			if (timeOfDay.useLocalTime)
				UpdateTime();

			UpdateOrbital();

			m_LastPos = Vector3.one * 999999;
			m_LastLodPos = Vector3.one * 999999;
			UpdateShaderProperties();

			// Force sky changed
			skyChanged = true;

			float min = Mathf.Min(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper);
			float max = Mathf.Max(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper);
			volumetricClouds.noiseSettings.alphaEdgeLower = min;
			volumetricClouds.noiseSettings.alphaEdgeUpper = max;

			// Snap shadow coverage so that the shadow texel size matches the compositor texel size, or a higher multiple
			float maxRatio = Mathf.Min((float)lighting.cloudShadows.resolution / (float)volumetricClouds.compositorResolution, 1f);
			lighting.cloudShadows.coverage = Mathf.Max(lighting.cloudShadows.coverage, 0.01f);
			if (lighting.cloudShadows.coverage >= maxRatio)
			{
				// Easy case, just floor down to max ratio
				lighting.cloudShadows.coverage = maxRatio;
			}
			else
			{
				// Need to find the two closest half division multiples and round to one of them
				float lowerRatio = maxRatio;
				float upperRatio = lowerRatio;
				while (lowerRatio > lighting.cloudShadows.coverage)
				{
					upperRatio = lowerRatio;
					lowerRatio *= 0.5f;
				}
				if (upperRatio - lighting.cloudShadows.coverage < lighting.cloudShadows.coverage - lowerRatio)
					lighting.cloudShadows.coverage = upperRatio;
				else
					lighting.cloudShadows.coverage = lowerRatio;
			}
		}

		/// <summary>
		/// Check availability of components and initialize where necessary
		/// </summary>
		void CheckComponents()
		{
			bool reinitialize = false;

			if (!m_CloudObject)
			{
				m_CloudObject = transform.Find("CloudObject") ? transform.Find("CloudObject").gameObject : null;
				if (!m_CloudObject)
				{
					m_CloudObject = new GameObject("CloudObject");
					m_CloudObject.hideFlags = HideFlags.HideAndDontSave;
					m_CloudObject.transform.SetParent(transform);
				}
				reinitialize = true;
			}
			if (!m_Renderer)
			{
				m_Renderer = m_CloudObject.GetComponent<MeshRenderer>();
				if (!m_Renderer)
					m_Renderer = m_CloudObject.AddComponent<MeshRenderer>();
				m_Renderer.sharedMaterial = components.cloudMaterial;
				m_Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				reinitialize = true;
			}
			if (!m_Filter)
			{
				m_Filter = m_CloudObject.GetComponent<MeshFilter>();
				if (!m_Filter)
					m_Filter = m_CloudObject.AddComponent<MeshFilter>();
				reinitialize = true;
			}

			if (!m_LodObject)
			{
				m_LodObject = transform.Find("CloudLOD") ? transform.Find("CloudLOD").gameObject : null;

				if (!m_LodObject)
				{
					m_LodObject = new GameObject("CloudLOD");
					m_LodObject.hideFlags = HideFlags.HideAndDontSave;
					m_LodObject.transform.SetParent(transform);
					m_LodObject.transform.localPosition = Vector3.zero;
					m_LodObject.transform.localRotation = Quaternion.identity;
				}
				reinitialize = true;
			}
			if (!m_LodRenderer)
			{
				m_LodRenderer = m_LodObject.GetComponent<MeshRenderer>();
				if (!m_LodRenderer)
					m_LodRenderer = m_LodObject.AddComponent<MeshRenderer>();
				m_LodRenderer.sharedMaterial = components.cloudMaterial;
				m_LodRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				reinitialize = true;
			}
			if (!m_LodFilter)
			{
				m_LodFilter = m_LodObject.GetComponent<MeshFilter>();
				if (!m_LodFilter)
					m_LodFilter = m_LodObject.AddComponent<MeshFilter>();
				reinitialize = true;
			}

			if (reinitialize)
				InitializeMeshes();

			if (!m_OverCloudSun && components.sun)
				m_OverCloudSun = components.sun.GetComponent<OverCloudLight>();
			if (!m_OverCloudMoon && components.moon)
				m_OverCloudMoon = components.moon.GetComponent<OverCloudLight>();

		}

		/// <summary>
		/// Initialize all meshes
		/// </summary>
		void InitializeMeshes ()
		{
			InitializeMesh(m_Filter, m_Renderer, m_PropBlock);
			InitializeMesh(m_LodFilter, m_LodRenderer, m_LodPropBlock, volumetricClouds.lodRadiusMultiplier);
		}

		/// <summary>
		/// Initialize a cloud plane point grid mesh
		/// </summary>
		/// <param name="filter">The MeshFilter component to put the mesh in</param>
		/// <param name="renderer">The MeshRenderer component to set material property block properties for</param>
		/// <param name="propBlock">The material property block to use</param>
		/// <param name="radiusMultiplier">Cloud plane radius multiplier</param>
		void InitializeMesh (MeshFilter filter, MeshRenderer renderer, MaterialPropertyBlock propBlock, float radiusMultiplier = 1)
		{
			var actualRadius = volumetricClouds.cloudPlaneRadius * radiusMultiplier;

			int cellCount = (int)Mathf.Floor(Mathf.Sqrt(volumetricClouds.particleCount));
			float cellSpan = (actualRadius * 2) / (float)cellCount;

			int quadCount = cellCount * cellCount;
			int vertexCount = quadCount * 4;
			int indexCount = quadCount * 2 * 3;

			Vector3[] vertices = new Vector3[vertexCount];
			int i = 0;
			for (int x = 0; x < cellCount; x++)
			{
				for (int y = 0; y < cellCount; y++)
				{
					Vector3 pos = Vector3.zero;
					pos.x = (float)x / (float)cellCount * actualRadius * 2 - actualRadius + cellSpan * 0.5f;
					pos.z = (float)y / (float)cellCount * actualRadius * 2 - actualRadius + cellSpan * 0.5f;

					vertices[i + 0] = pos;
					vertices[i + 1] = pos;
					vertices[i + 2] = pos;
					vertices[i + 3] = pos;

					i += 4;
				}
			}

			Vector2[] uv0 = new Vector2[vertexCount];
			Vector2[] uv1 = new Vector2[vertexCount];
			for (i = 0; i < quadCount; i++)
			{
				uv0[i * 4 + 0] = new Vector2(0, 0);
				uv0[i * 4 + 1] = new Vector2(1, 0);
				uv0[i * 4 + 2] = new Vector2(0, 1);
				uv0[i * 4 + 3] = new Vector2(1, 1);
			}

			Vector3[] normals = new Vector3[vertexCount];
			for (i = 0; i < quadCount; i++)
			{
				normals[i * 4 + 0] = Vector3.up;
				normals[i * 4 + 1] = Vector3.up;
				normals[i * 4 + 2] = Vector3.up;
				normals[i * 4 + 3] = Vector3.up;
			}

			int[] triangles = new int[indexCount];
			for (i = 0; i < quadCount; i++)
			{
				triangles[i * 6 + 0] = i * 4 + 0;
				triangles[i * 6 + 1] = i * 4 + 1;
				triangles[i * 6 + 2] = i * 4 + 2;
				triangles[i * 6 + 3] = i * 4 + 1;
				triangles[i * 6 + 4] = i * 4 + 3;
				triangles[i * 6 + 5] = i * 4 + 2;
			}

			Color[] colors = new Color[vertexCount];
			for (i = 0; i < quadCount; i++)
			{
				Color c = new Color(1, 1, 1, 1);
				c.a = Random.Range(0f, 1f);

				colors[i * 4 + 0] = c;
				colors[i * 4 + 1] = c;
				colors[i * 4 + 2] = c;
				colors[i * 4 + 3] = c;
			}

			var mesh = new Mesh();
			mesh.name = "OverCloud_Mesh";

			mesh.vertices = vertices;
			mesh.uv = uv0;
			mesh.uv2 = uv1;
			mesh.normals = normals;
			mesh.triangles = triangles;
			mesh.colors = colors;
			mesh.RecalculateBounds();
			var bounds = mesh.bounds;
			var extents = bounds.extents;
			extents.y = 10000;
			bounds.extents = extents;
			mesh.bounds = bounds;

			filter.sharedMesh = mesh;

			if (propBlock == null)
				propBlock = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(propBlock);

			propBlock.SetFloat("_RandomRange", cellSpan);
			propBlock.SetFloat("_Radius", actualRadius);
			propBlock.SetFloat("_Altitude", adjustedCloudPlaneAltitude);

			renderer.SetPropertyBlock(propBlock);

			filter.sharedMesh = SortTriangles(filter.sharedMesh);
		}
		#endregion

		#region Rendering

#if UNITY_2019_3_OR_NEWER
		static List<XRDisplaySubsystem> s_xrDisplaySubsystems = new List<XRDisplaySubsystem>();
#endif

		public static VRTextureUsage GetVRUsageFromCamera (Camera camera)
		{
#if !UNITY_2017_2_OR_NEWER
			// Old VR namespace
			return (camera.stereoTargetEye != StereoTargetEyeMask.None && Application.isPlaying && UnityEngine.VR.VRSettings.enabled && UnityEngine.VR.VRDevice.isPresent) ? VRTextureUsage.TwoEyes : VRTextureUsage.None;
#else
			bool XRDevicePresent = false;

	#if UNITY_2019_3_OR_NEWER
			// Newer XR display system
			SubsystemManager.GetInstances<XRDisplaySubsystem>(s_xrDisplaySubsystems);
			foreach (var xrDisplay in s_xrDisplaySubsystems)
			{
				if (xrDisplay.running)
				{
					XRDevicePresent = true;
				}
			}
	#else
			// New but old XR namespace
			XRDevicePresent = XRDevice.isPresent;
	#endif

			return (camera.stereoTargetEye != StereoTargetEyeMask.None && Application.isPlaying && XRSettings.enabled && XRDevicePresent) ? VRTextureUsage.TwoEyes : VRTextureUsage.None;
#endif
		}

		/// <summary>
		/// Ensure accurate position/orientation data is fed to the ray marching shaders.
		/// </summary>
		/// <param name="camera"></param>
		void UpdateRaymarchingMatrices (Camera camera)
		{
			// Set up some data needed for ray marching, as late as possible

			// Determine if we are doing VR rendering or not
#if UNITY_2017_2_OR_NEWER
			VRTextureUsage vrUsage = GetVRUsageFromCamera(camera);
#endif

			// In 2018.3 and onward we can use XRSettings.stereoRenderingMode to tell from script if we are using single pass stereo
			// In earlier versions we just have to always set both versions.
#if UNITY_2018_3_OR_NEWER
			if (vrUsage == VRTextureUsage.None || XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
			{
#endif
				// Inverse view matrix
				Matrix4x4 world_from_view = camera.cameraToWorldMatrix;

				// Inverse projection matrix
				Matrix4x4 screen_from_view = camera.projectionMatrix;
				Matrix4x4 view_from_screen = GL.GetGPUProjectionMatrix(screen_from_view, true).inverse;

				// CBuffer state negation
				view_from_screen[1, 1] *= -1;

				// Set matrices
				Shader.SetGlobalMatrix("_WorldFromView", world_from_view);
				Shader.SetGlobalMatrix("_ViewFromScreen", view_from_screen);
#if UNITY_2018_3_OR_NEWER
			}
			else
			{
#endif
				// Both stereo eye inverse view matrices
				Matrix4x4 left_world_from_view = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
				Matrix4x4 right_world_from_view = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;

				// Both stereo eye inverse projection matrices
				Matrix4x4 left_screen_from_view = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
				Matrix4x4 right_screen_from_view = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
				Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(left_screen_from_view, true).inverse;
				Matrix4x4 right_view_from_screen = GL.GetGPUProjectionMatrix(right_screen_from_view, true).inverse;

				// CBuffer state negation
				left_view_from_screen[1, 1]  *= -1;
				right_view_from_screen[1, 1] *= -1;

				// Set matrices
				Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
				Shader.SetGlobalMatrix("_RightWorldFromView", right_world_from_view);
				Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
				Shader.SetGlobalMatrix("_RightViewFromScreen", right_view_from_screen);
#if UNITY_2018_3_OR_NEWER
			}
#endif
		}

		/// <summary>
		/// Render the clouds
		/// </summary>
		/// <param name="camera">Camera to render the clouds in</param>
		public static void Render (Camera camera, bool renderVolumetricClouds, bool render2DFallback, bool renderAtmosphere, bool renderScatteringMask, bool includeCascadedShadows, bool downsample2DClouds, SampleCount scatteringMaskSamples, bool renderRainMask, DownSampleFactor downsampleFactor, SampleCount lightSampleCount, bool highQualityClouds)
		{
			if (!camera || !instance)
				return;

			instance.mRender(camera, renderVolumetricClouds, render2DFallback, renderAtmosphere, renderScatteringMask, includeCascadedShadows, downsample2DClouds, scatteringMaskSamples, renderRainMask, downsampleFactor, lightSampleCount, highQualityClouds);
		}

		/// <summary>
		/// Render the clouds (member function)
		/// </summary>
		/// <param name="camera">Camera to render the clouds in</param>
		void mRender (Camera camera, bool renderVolumetricClouds, bool render2DFallback, bool renderAtmosphere, bool renderScatteringMask, bool includeCascadedShadows, bool downsample2DClouds, SampleCount scatteringMaskSamples, bool renderRainMask, DownSampleFactor downsampleFactor, SampleCount lightSampleCount, bool highQualityClouds)
		{
			if (beforeRender != null)
				beforeRender.Invoke();

			RenderTextureDescriptor desc;

			// Force scattering mask off if we are not rendering any shadows
			if (!lighting.cloudShadows.enabled && !includeCascadedShadows)
				renderScatteringMask = false;

			// Override skybox material
			if (components.skyMaterial && atmosphere.overrideSkyboxMaterial)
				RenderSettings.skybox = components.skyMaterial;
			if (components.sun)
				RenderSettings.sun = components.sun;

			// ****************************************
			// -1. Render rain mask
			// ****************************************

			if (renderRainMask && m_CurrentPreset.precipitation > Mathf.Epsilon)
			{
				var activePrev = RenderTexture.active;

				int rainMaskRes = (int)weather.rain.maskResolution;
				float rainMaskWidth = weather.rain.maskRadius * 2;
				float texelSize = rainMaskWidth / (float)rainMaskRes * 9;
				var pos = camera.transform.position;
				// Snap to rain mask texels
				pos.x = Mathf.Round(pos.x / texelSize) * texelSize;
				pos.z = Mathf.Round(pos.z / texelSize) * texelSize;
				pos.y = adjustedCloudPlaneAltitude;
				m_RainCamera.transform.position = pos;
				m_RainCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
				Shader.SetGlobalVector("_OC_RainMaskPosition", m_RainCamera.transform.position);
				Shader.SetGlobalVector("_OC_RainMaskRadius", new Vector3(weather.rain.maskRadius, 1f / weather.rain.maskRadius, 1f / rainMaskWidth));
				Shader.SetGlobalFloat("_OC_RainMaskFalloff", 1f / weather.rain.maskFalloff);
				Shader.SetGlobalVector("_OC_RainMaskTexel", new Vector4(rainMaskRes, 1f / (float)rainMaskRes, weather.rain.maskOffsetTexture.width, 1f / (float)weather.rain.maskOffsetTexture.width));
				Shader.SetGlobalFloat("_OC_RainMaskOffset", weather.rain.maskOffset);

				// Set up camera and RT
				if (!m_RainMask || m_RainMask.width != rainMaskRes || m_RainMask.height != rainMaskRes)
					m_RainMask					= new RenderTexture(rainMaskRes, rainMaskRes, 16, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
				m_RainMask.filterMode			= FilterMode.Bilinear;
				m_RainCamera.orthographic		= true;
				m_RainCamera.orthographicSize	= weather.rain.maskRadius;
				m_RainCamera.aspect				= 1;
				m_RainCamera.clearFlags			= CameraClearFlags.SolidColor;
				m_RainCamera.backgroundColor	= Color.clear;
				m_RainCamera.cullingMask		= weather.rain.maskLayers;
				m_RainCamera.allowMSAA			= false;
				m_RainCamera.farClipPlane		= adjustedCloudPlaneAltitude;
				m_RainCamera.nearClipPlane		= 1;
				m_RainCamera.renderingPath		= RenderingPath.Forward;

				// Add depth copy command buffer
				var depthCopyBuffer = new CommandBuffer();
				Shader.SetGlobalTexture("_OC_RainMaskOffsetTex", weather.rain.maskOffsetTexture);
				depthCopyBuffer.Blit(null, m_RainMask, m_UtilitiesMat, 3);
				m_RainCamera.AddCommandBuffer(CameraEvent.AfterDepthTexture, depthCopyBuffer);

				// Instead of using replacement shaders, we force the camera to render a depth texture and grab it afterwards.
				// This is slightly wasteful since the camera will render the scene twice, when really all we need is a depth pass.
				// The reason for this is Unity doesn't expose its internal method of rendering depth passes using the shader's own ShadowCaster pass,
				// so if we want to offer support for user-authored ShadowCaster passes, this is the only way to do it.
				m_RainCamera.depthTextureMode	= DepthTextureMode.Depth;
				var shadowsEnabled = QualitySettings.shadows;
				QualitySettings.shadows = ShadowQuality.Disable;
				#if UNITY_EDITOR
					// If we render the camera without setting the target texture, it might render to a GUI element in the editor for some reason
					var foo = RenderTexture.GetTemporary(m_RainMask.width, m_RainMask.height, 16, RenderTextureFormat.R8);
					m_RainCamera.targetTexture = foo;
				#endif
				m_RainCamera.Render();
				#if UNITY_EDITOR
					RenderTexture.ReleaseTemporary(foo);
				#endif
				QualitySettings.shadows = shadowsEnabled;

				// Remove depth copy command buffer
				m_RainCamera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, depthCopyBuffer);

				if (weather.rain.maskBlur > Mathf.Epsilon)
				{
					// Blur rain mask
					var tmp = RenderTexture.GetTemporary(m_RainMask.descriptor);
					Shader.SetGlobalVector("_PixelSize", new Vector2(1f / (float)m_RainMask.width, 1f / (float)m_RainMask.height));
					Shader.SetGlobalFloat("_BlurAmount", weather.rain.maskBlur);
					// Vertical blur pass
					Graphics.Blit(m_RainMask, tmp, m_SeparableBlurMat, 0);
					// Horizontal blur pass
					Graphics.Blit(tmp, m_RainMask, m_SeparableBlurMat, 1);
					RenderTexture.ReleaseTemporary(tmp);
				}

				// Set rain mask texture
				Shader.SetGlobalTexture("_OC_RainMask", m_RainMask);

				RenderTexture.active = activePrev;
			}
			else
			{
				Shader.SetGlobalTexture("_OC_RainMask", Texture2D.whiteTexture);
			}

			// ****************************************
			// 0. Rendering initialization
			// ****************************************

			UpdateRaymarchingMatrices(camera);

			// Determine if we are doing VR rendering or not
			VRTextureUsage vrUsage = GetVRUsageFromCamera(camera);

			// To guarantee that the scene is not clipped at any point and instead fades smoothly in all directions,
			// we have to apply a small adjustment to the far clip distance in the form of a 0.65x multiplier.
			Shader.SetGlobalFloat("_OC_FarClipInv", 1f / (camera.farClipPlane * 0.65f));

			// The distance fade is camera-dependent, hence why this is set here and not in UpdateShaderProperties
			_OC_MieScatteringParams.value = new Vector4(
				atmosphere.mieScatteringIntensity * 0.1f,
				atmosphere.mieScatteringPhase,
				atmosphere.mieScatteringFogPhase,
				renderScatteringMask && includeCascadedShadows ? Mathf.Pow(1-atmosphere.mieScatteringDistanceFadeB, 8) : Mathf.Pow(1-atmosphere.mieScatteringDistanceFadeA, 8));

			// Force depth buffer pass for camera (can't render clouds otherwise)
			if (camera.actualRenderingPath == RenderingPath.Forward && camera.depthTextureMode == DepthTextureMode.None)
				camera.depthTextureMode = DepthTextureMode.Depth;
		
			// Camera render size // TODO: Could probably use XRSettings.eyeTextureDesc for stereo rendering, instead
			bufferWidth = camera.pixelWidth * (vrUsage == VRTextureUsage.TwoEyes ? 2 : 1);
			bufferHeight = camera.pixelHeight;

			// Downsampled render size
			bufferWidthDS  = bufferWidth / (int)downsampleFactor;
			bufferHeightDS = bufferHeight / (int)downsampleFactor;
			
			// Snap buffer width to an even value to properly support stereo rendering (nearest-depth upscale filter will not work properly for the right eye, otherwise)
			if (bufferWidthDS % 2 != 0)
				bufferWidthDS++;

			// TODO: This might not be necessary
			UpdateShaderProperties();

			// Disable renderers since we are rendering manually
			m_Renderer.enabled					= false;
			m_LodRenderer.enabled				= false;

			// Create command buffers (if necessary) TODO: Is it really necessary to re-create the buffers each frame?
			CommandBuffer bufPre = null;
			if (m_CameraPreBuffers.ContainsKey(camera))
			{
				// Command buffer already exists, grab and clear the old one
				bufPre = m_CameraPreBuffers[camera];
				bufPre.Clear();
			}
			else
			{
				// Need to create a new command buffer
				bufPre = new CommandBuffer();
				bufPre.name = "CloudBufPre";
				m_CameraPreBuffers.Add(camera, bufPre);

				// For forward rendering
				camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, bufPre);
				// For deferred rendering
				camera.AddCommandBuffer(CameraEvent.AfterLighting, bufPre);
			}

			CommandBuffer buf = null;
			if (m_CameraBuffers.ContainsKey(camera))
			{
				// Command buffer already exists, grab and clear the old one
				buf = m_CameraBuffers[camera];
				buf.Clear();
			}
			else
			{
				// Need to create a new command buffer
				buf = new CommandBuffer();
				buf.name = "CloudBuf";
				m_CameraBuffers.Add(camera, buf);

				// Executes in both forward and deferred rendering paths
				// CameraEvent.AfterSkybox will probably give better compatibility with other assets,
				// unfortunately, Unity does not execute this event in the same order in the scene view,
				// resulting in lack of clouds.
				camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, buf);
			}

			CommandBuffer bufPost = null;
			if (m_CameraPostBuffers.ContainsKey(camera))
			{
				// Command buffer already exists, grab and clear the old one
				bufPost = m_CameraPostBuffers[camera];
				bufPost.Clear();
			}
			else
			{
				// Need to create a new command buffer
				bufPost = new CommandBuffer();
				bufPost.name = "CloudBufPost";
				m_CameraPostBuffers.Add(camera, buf);

				// Executes in both forward and deferred rendering paths
				camera.AddCommandBuffer(CameraEvent.AfterEverything, bufPost);
			}

			// Global shader keyword which is always set when an OverCloud-enabled camera is rendering
			bufPre.EnableShaderKeyword("OVERCLOUD_ENABLED");

			// Set when an OverCloud-enabled camera is rendering with the atmosphere flag set to true
			if (renderAtmosphere)
				bufPre.EnableShaderKeyword("OVERCLOUD_ATMOSPHERE_ENABLED");
			else
				bufPre.DisableShaderKeyword("OVERCLOUD_ATMOSPHERE_ENABLED");

			// Set when 2D clouds are downsampled along with volumetric ones (determines which depth buffer they use)
			if (downsample2DClouds)
				bufPre.EnableShaderKeyword("DOWNSAMPLE_2D_CLOUDS");
			else
				bufPre.DisableShaderKeyword("DOWNSAMPLE_2D_CLOUDS");

			// Set when an OverCloud-enabled camera is rendering with the rain mask flag set to true
			if (renderRainMask)
				bufPre.EnableShaderKeyword("RAIN_MASK_ENABLED");
			else
				bufPre.DisableShaderKeyword("RAIN_MASK_ENABLED");

			// Set when OverCloud is rendering its own sky
			if (RenderSettings.skybox == components.skyMaterial || atmosphere.overrideSkyboxMaterial)
				bufPre.EnableShaderKeyword("OVERCLOUD_SKY_ENABLED");
			else
				bufPre.DisableShaderKeyword("OVERCLOUD_SKY_ENABLED");

			switch (downsampleFactor)
			{
				case DownSampleFactor.Full:
					m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0);
				break;
				case DownSampleFactor.Half:
					m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0.5f);
				break;
				case DownSampleFactor.Quarter:
					m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 1);
				break;
				case DownSampleFactor.Eight:
					m_UpsampleMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 2);
				break;
			}

			// ****************************************
			// 1. Down-sample the depth buffer
			// ****************************************

			bufPre.SetGlobalVector("_PixelSize", new Vector4(bufferWidth, bufferHeight, 1f / (float)bufferWidth, 1f / (float)bufferHeight));
			bufPre.SetGlobalVector("_PixelSizeDS", new Vector4(bufferWidthDS, bufferHeightDS, 1f / (float)bufferWidthDS, 1f / (float)bufferHeightDS));
			switch (downsampleFactor)
			{
				case DownSampleFactor.Full:
					m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0);
				break;
				case DownSampleFactor.Half:
					m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 0.5f);
				break;
				case DownSampleFactor.Quarter:
					m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 1);
				break;
				case DownSampleFactor.Eight:
					m_DownsampleDepthMat.SetVector("_HalfTexel", new Vector2(1f / (float)bufferWidth, 1f / (float)bufferHeight) * 2);
				break;
			}

			// Downsampled depth descriptor
			desc			= new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.RFloat, 0);
			desc.sRGB		= false;
			desc.vrUsage	= vrUsage;

			// Get the downsampled depth render texture for this camera
			if (!m_DownsampledDepthRTs.ContainsKey(camera))
			{
				m_DownsampledDepthRTs.Add(camera, new RenderTexture(desc));
			}

			// Render texture initializiation/reinitialization
			if (m_DownsampledDepthRTs[camera] == null || m_DownsampledDepthRTs[camera].width != bufferWidthDS || m_DownsampledDepthRTs[camera].height != bufferHeightDS)
			{
				if (m_DownsampledDepthRTs[camera] != null)
				{
					m_DownsampledDepthRTs[camera].DiscardContents();
					m_DownsampledDepthRTs[camera].Release();
				}
				m_DownsampledDepthRTs[camera] = new RenderTexture(desc);
			}

			// Downsampled depth RT
			var downsmpledDepthRT = m_DownsampledDepthRTs[camera];
			// Make sure we are not using any filtering
			downsmpledDepthRT.filterMode = FilterMode.Point;

			// Make sure the depth buffer doesn't contain garbage values
			bufPre.SetRenderTarget(downsmpledDepthRT);
			bufPre.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

			// Downsample depth
			bufPre.Blit(null, downsmpledDepthRT, m_DownsampleDepthMat);
			bufPre.SetGlobalTexture("_CameraDepthLowRes", downsmpledDepthRT);

			// ****************************************
			// 2. Render the scattering mask
			// ****************************************

			if (renderAtmosphere && renderVolumetricClouds && renderScatteringMask && dominantOverCloudLight)
			{
				// Scattering mask descriptor
				desc			= new RenderTextureDescriptor(bufferWidth, bufferHeight, RenderTextureFormat.RG32, 0);
				desc.vrUsage	= vrUsage;

				// Scattering mask RT
				if (!m_ScatteringMasks.ContainsKey(camera))
					m_ScatteringMasks.Add(camera, new RenderTexture(desc));
				if (m_ScatteringMasks[camera] == null || m_ScatteringMasks[camera].width != desc.width || m_ScatteringMasks[camera].height != desc.height)
				{
					m_ScatteringMasks[camera] = new RenderTexture(desc);
				}
				scatteringMaskRT = m_ScatteringMasks[camera];

				// Downsampled scattering mask descriptor
				var scatteringMaskDSDesc		= new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.RGHalf, 0);
				scatteringMaskDSDesc.vrUsage	= vrUsage;

				// Downsampled scattering mask RT
				var scatteringMaskDS = Shader.PropertyToID("_ScatteringMaskDS");
				bufPre.GetTemporaryRT(scatteringMaskDS, scatteringMaskDSDesc);

				float scatteringRadius = Mathf.Max(volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * atmosphere.scatteringMask.range, 10);
				Shader.SetGlobalVector("_OC_ScatteringMaskRadius", new Vector2(scatteringRadius, 1f / scatteringRadius));
				m_ScatteringMaskRTMat.SetFloat("_Intensity", atmosphere.scatteringMask.intensity);
				m_ScatteringMaskRTMat.SetFloat("_Floor", atmosphere.scatteringMask.floor);
				m_ScatteringMaskRTMat.SetVector("_Random", Random.insideUnitCircle);
				m_ScatteringMaskRTMat.SetVector("_ShadowDistance", new Vector2(QualitySettings.shadowDistance, 1f / QualitySettings.shadowDistance));
				// Check if we should included the cascaded shadow map in the scattering mask
				bool cascadedShadowsEnabled = includeCascadedShadows && QualitySettings.shadows != ShadowQuality.Disable;
				if (dominantLight)
				{
					cascadedShadowsEnabled = cascadedShadowsEnabled && dominantLight.shadows != LightShadows.None;
					cascadedShadowsEnabled = cascadedShadowsEnabled && dominantLight.isActiveAndEnabled;
				}
				else
				{
					cascadedShadowsEnabled = false;
				}
				m_ScatteringMaskRTMat.SetFloat("_CascadedShadowsEnabled", cascadedShadowsEnabled ? 1f : 0f);

				switch (scatteringMaskSamples)
				{
					case SampleCount.Low:
						m_ScatteringMaskRTMat.EnableKeyword("SAMPLE_COUNT_LOW");
						m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_MEDIUM");
					break;
					case SampleCount.Normal:
						m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_LOW");
						m_ScatteringMaskRTMat.EnableKeyword("SAMPLE_COUNT_MEDIUM");
					break;
					case SampleCount.High:
						m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_LOW");
						m_ScatteringMaskRTMat.DisableKeyword("SAMPLE_COUNT_MEDIUM");
					break;
				}
			
				// Render scattering mask
				bufPre.Blit(null, scatteringMaskDS, m_ScatteringMaskRTMat);

				// Blur scattering mask
				var blurResult = Shader.PropertyToID("_BlurResult");
				bufPre.GetTemporaryRT(blurResult, scatteringMaskDSDesc);
				bufPre.SetGlobalFloat("_DepthThreshold", 0.1f);
				bufPre.SetGlobalVector("_PixelSize", new Vector2(1f / (float)bufferWidthDS, 1f / (float)bufferHeightDS));
				bufPre.SetGlobalFloat("_BlurAmount", 1f);
				// Vertical blur pass
				bufPre.Blit(scatteringMaskDS, blurResult, m_SeparableBlurMat, 2);
				// Horizontal blur pass
				bufPre.Blit(blurResult, scatteringMaskDS, m_SeparableBlurMat, 3);
				bufPre.ReleaseTemporaryRT(blurResult);

				// Upsample scattering mask
				bufPre.Blit(scatteringMaskDS, scatteringMaskRT, m_UpsampleMat, 0);

				// Don't need this anymore
				bufPre.ReleaseTemporaryRT(scatteringMaskDS);

				// Set upsampled scattering mask
				bufPre.SetGlobalTexture("_OC_ScatteringMask", scatteringMaskRT);
			}
			else
			{
				bufPre.SetGlobalTexture("_OC_ScatteringMask", Texture2D.whiteTexture);
			}

			// ****************************************
			// 3. Render the atmosphere
			// ****************************************

			if (renderAtmosphere)
			{
				// Get temporary RT
				var tmp = Shader.PropertyToID("_BackBuffer");
				#if UNITY_2017_2_OR_NEWER
				if (XRSettings.enabled && camera.stereoEnabled)
				{
					desc = XRSettings.eyeTextureDesc;
					// There seems to be a bug in Unity 2019.1 where XRSettings.eyeTextureDesc fails to take HDR formats into account, so we force it based on camera settings.
					desc.colorFormat = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
					bufPre.GetTemporaryRT(tmp, desc);
				}
				else
					bufPre.GetTemporaryRT(tmp, new RenderTextureDescriptor(bufferWidth, bufferHeight, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, 0));
				#else
				bufPre.GetTemporaryRT(tmp, new RenderTextureDescriptor(bufferWidth, bufferHeight, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, 0));
				#endif
				
				// Copy backbuffer to temporary RT
				bufPre.SetRenderTarget(tmp);
				bufPre.SetGlobalTexture("_BlitTex", BuiltinRenderTextureType.CameraTarget);
				bufPre.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 7);
				// Render the atmosphere
				bufPre.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				bufPre.SetGlobalTexture("_BackBuffer", tmp);
				bufPre.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 0);
				// Release temporary RT
				bufPre.ReleaseTemporaryRT(tmp);
			}

			// ****************************************
			// 3. Cloud AO (deferred only)
			// ****************************************
			CommandBuffer occlusionBuffer = null;
			if (m_OcclusionBuffers.ContainsKey(camera))
			{
				// Command buffer already exists, grab the old one
				occlusionBuffer = m_OcclusionBuffers[camera];
				// Check if rendering path was changed (if so, we need to remove the command buffer)
				if (camera.renderingPath != RenderingPath.DeferredShading || lighting.cloudAmbientOcclusion.intensity <= Mathf.Epsilon)
				{
					camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, occlusionBuffer);
					m_OcclusionBuffers.Remove(camera);
				}
			}
			else if (camera.renderingPath == RenderingPath.DeferredShading && lighting.cloudAmbientOcclusion.intensity > Mathf.Epsilon)
			{
				// Need to create a new command buffer
				occlusionBuffer = new CommandBuffer();
				occlusionBuffer.name = "OverCloudAmbientOcclusion";
				m_OcclusionBuffers.Add(camera, occlusionBuffer);

				occlusionBuffer.SetRenderTarget(m_OcclusionMRT, BuiltinRenderTextureType.CameraTarget);
                occlusionBuffer.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 2);

				camera.AddCommandBuffer(CameraEvent.BeforeReflections, occlusionBuffer);
			}

			// ****************************************
			// 3. Wetness (deferred only)
			// ****************************************
			CommandBuffer wetnessBuffer = null;
			if (m_WetnessBuffers.ContainsKey(camera))
			{
				// Command buffer already exists, grab the old one
				wetnessBuffer = m_WetnessBuffers[camera];
				wetnessBuffer.Clear();
				// Check if rendering path was changed (if so, we need to remove the command buffer)
				if (camera.renderingPath != RenderingPath.DeferredShading)
				{
					camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, wetnessBuffer);
					m_WetnessBuffers.Remove(camera);
				}
			}
			else if (camera.renderingPath == RenderingPath.DeferredShading)
			{
				// Need to create a new command buffer
				wetnessBuffer = new CommandBuffer();
				wetnessBuffer.name = "OverCloudWetness";
				m_WetnessBuffers.Add(camera, wetnessBuffer);

				camera.AddCommandBuffer(CameraEvent.BeforeReflections, wetnessBuffer);
			}

			if (wetnessBuffer != null)
			{
				// If we managed to grab a wetness buffer, we can safely assume we are on deferred path

				if (renderRainMask)
					wetnessBuffer.EnableShaderKeyword("RAIN_MASK_ENABLED");
				else
					wetnessBuffer.DisableShaderKeyword("RAIN_MASK_ENABLED");

				// Bind normals buffer
				wetnessBuffer.SetGlobalTexture("_GBuffer2", BuiltinRenderTextureType.GBuffer2);

				// Modify albedo and gloss
				wetnessBuffer.SetRenderTarget(m_WetnessMRT, BuiltinRenderTextureType.CameraTarget);
				wetnessBuffer.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 3);

				// Need to copy normals buffer, since we'll be using it to modify itself
				desc			= new RenderTextureDescriptor(bufferWidth, bufferHeight, RenderTextureFormat.ARGB2101010, 0);
				desc.vrUsage	= vrUsage;
				var gbuffer2Copy = Shader.PropertyToID("_GBuffer2Copy");
				wetnessBuffer.GetTemporaryRT(gbuffer2Copy, desc);
				wetnessBuffer.SetGlobalTexture("_GBuffer2Copy", gbuffer2Copy);
				wetnessBuffer.Blit(BuiltinRenderTextureType.GBuffer2, gbuffer2Copy);

				wetnessBuffer.SetGlobalTexture("_OC_RainFlowTex", weather.rain.flowTexture);
				wetnessBuffer.SetRenderTarget(BuiltinRenderTextureType.GBuffer2, BuiltinRenderTextureType.CameraTarget);
				wetnessBuffer.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 4);

				wetnessBuffer.ReleaseTemporaryRT(gbuffer2Copy);
			}

			// ****************************************
			// 4. Render cloud planes + volumetric clouds
			// ****************************************

			// Get the cloud render texture for this camera
			if (!m_CloudRTs.ContainsKey(camera))
			{
				desc			= new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBHalf, 0);
				desc.vrUsage	= vrUsage;
				m_CloudRTs.Add(camera, new RenderTexture(desc));
			}
			if (!m_CloudDepthRTs.ContainsKey(camera))
			{
				desc			= new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBFloat, 0);
				desc.vrUsage	= vrUsage;
				m_CloudDepthRTs.Add(camera, new RenderTexture(desc));
			}

			// Render texture initializiation/reinitialization
			if (m_CloudRTs[camera] == null || m_CloudRTs[camera].width != bufferWidthDS || m_CloudRTs[camera].height != bufferHeightDS)
			{
				if (m_CloudRTs[camera] != null)
				{
					m_CloudRTs[camera].DiscardContents();
					m_CloudRTs[camera].Release();
				}
				desc			= new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBHalf, 0);
				desc.vrUsage	= vrUsage;
				m_CloudRTs[camera] = new RenderTexture(desc);
			}
			if (m_CloudDepthRTs[camera] == null || m_CloudDepthRTs[camera].width != bufferWidthDS || m_CloudDepthRTs[camera].height != bufferHeightDS)
			{
				if (m_CloudDepthRTs[camera] != null)
				{
					m_CloudDepthRTs[camera].DiscardContents();
					m_CloudDepthRTs[camera].Release();
				}
				desc			= new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.ARGBHalf, 0);
				desc.vrUsage	= vrUsage;
				m_CloudDepthRTs[camera] = new RenderTexture(desc);
			}

			cloudRT = m_CloudRTs[camera];
			cloudDepthRT = m_CloudDepthRTs[camera];

			// Clear cloud + depth buffer
			buf.SetRenderTarget(cloudRT);
			buf.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
			buf.SetGlobalFloat("_FarZ", camera.farClipPlane);
			buf.Blit(null, cloudDepthRT, m_ClearMat, 1);

			// Sort according to distance from camera
			var cloudPlanesSorted = cloudPlanes.OrderBy(o=>Mathf.Abs((camera.transform.position.y + currentOriginOffset.y) - o.height)).Reverse().ToArray();

			float adjustedCameraHeight = camera.transform.position.y + currentOriginOffset.y;

			// Render cloud planes which should appear behind the volumetric clouds
			foreach (var cloudPlane in cloudPlanesSorted)
			{
				if (!(cloudPlane.height > current.cloudPlaneAltitude && adjustedCameraHeight > cloudPlane.height ||
					cloudPlane.height < current.cloudPlaneAltitude && adjustedCameraHeight < cloudPlane.height))
				{
					buf.SetGlobalTexture("_CloudPlaneTex", cloudPlane.texture);
					buf.SetGlobalVector("_CloudPlaneParams1", new Vector4(1f / cloudPlane.scale, 1f / (cloudPlane.detailScale / cloudPlane.scale), cloudPlane.height, cloudPlane.opacity));
					buf.SetGlobalVector("_CloudPlaneParams2", new Vector3(cloudPlane.lightPenetration, cloudPlane.lightAbsorption, cloudPlane.windTimescale));
					buf.SetGlobalVector("_CloudPlaneColor", cloudPlane.color);
					if (adjustedCameraHeight > cloudPlane.height)
						buf.SetGlobalFloat("_AboveCloudPlane", 1f);
					else
						buf.SetGlobalFloat("_AboveCloudPlane", 0f);

					if (downsample2DClouds)
						buf.SetRenderTarget(cloudRT);
					else
						buf.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

					buf.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 1);
				}
			}

			// Volumetric cloud LOD plane
			if (render2DFallback)
			{
				buf.SetGlobalFloat("_RenderingVolumetricClouds", renderVolumetricClouds ? 1 : 0);
				if (downsample2DClouds)
					buf.SetRenderTarget(cloudRT);
				else
					buf.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				buf.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 6);
			}

			if (renderVolumetricClouds)
			{
				if (highQualityClouds)
					components.cloudMaterial.EnableKeyword("HQ_LIGHT_SAMPLING");
				else
					components.cloudMaterial.DisableKeyword("HQ_LIGHT_SAMPLING");

				switch (lightSampleCount)
				{
					case SampleCount.Low:
						components.cloudMaterial.EnableKeyword("SAMPLE_COUNT_LOW");
						components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_NORMAL");
					break;
					default:
					case SampleCount.Normal:
						components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_LOW");
						components.cloudMaterial.EnableKeyword("SAMPLE_COUNT_NORMAL");
					break;
					case SampleCount.High:
						components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_LOW");
						components.cloudMaterial.DisableKeyword("SAMPLE_COUNT_NORMAL");
					break;
				}

				if (highQualityClouds)
					components.cloudMaterial.EnableKeyword("HQ_LIGHT_SAMPLING");
				else
					components.cloudMaterial.DisableKeyword("HQ_LIGHT_SAMPLING");

				// Render the clouds
				var mrt = new RenderTargetIdentifier[2] {cloudRT, cloudDepthRT};
				buf.SetRenderTarget(mrt, mrt[0]);
				buf.EnableShaderKeyword("LOD_CLOUDS");
				buf.DrawRenderer(m_LodRenderer, components.cloudMaterial, 0, 0);
				buf.DisableShaderKeyword("LOD_CLOUDS");
				buf.DrawRenderer(m_Renderer, components.cloudMaterial, 0, 0);

				if (!downsample2DClouds)
				{
					// Make cloud buffers available ASAP
					buf.SetGlobalTexture("_OverCloudDepthTex", cloudDepthRT);
					buf.SetGlobalTexture("_OverCloudTex", cloudRT);

					// Transfer clouds to back buffer
					buf.Blit(cloudRT, BuiltinRenderTextureType.CameraTarget, m_UpsampleMat, 1);
				}
			}

			// Render cloud planes which should appear in front of the volumetric clouds
			foreach (var cloudPlane in cloudPlanesSorted)
			{
				if (cloudPlane.height > current.cloudPlaneAltitude && adjustedCameraHeight > cloudPlane.height ||
					cloudPlane.height < current.cloudPlaneAltitude && adjustedCameraHeight < cloudPlane.height)
				{
					buf.SetGlobalTexture("_CloudPlaneTex", cloudPlane.texture);
					buf.SetGlobalVector("_CloudPlaneParams1", new Vector4(1f / cloudPlane.scale, 1f / (cloudPlane.detailScale / cloudPlane.scale), cloudPlane.height, cloudPlane.opacity));
					buf.SetGlobalVector("_CloudPlaneParams2", new Vector3(cloudPlane.lightPenetration, cloudPlane.lightAbsorption, cloudPlane.windTimescale));
					buf.SetGlobalVector("_CloudPlaneColor", cloudPlane.color);
					if (adjustedCameraHeight > cloudPlane.height)
						buf.SetGlobalFloat("_AboveCloudPlane", 1f);
					else
						buf.SetGlobalFloat("_AboveCloudPlane", 0f);

					if (downsample2DClouds)
						buf.SetRenderTarget(cloudRT);
					else
						buf.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

					buf.DrawMesh(quad, Matrix4x4.identity, m_AtmosphereMat, 0, 1);
				}
			}

			if (downsample2DClouds)
			{
				// Make cloud buffers available ASAP
				buf.SetGlobalTexture("_OverCloudDepthTex", cloudDepthRT);
				buf.SetGlobalTexture("_OverCloudTex", cloudRT);

				// Transfer clouds to back buffer
				buf.Blit(cloudRT, BuiltinRenderTextureType.CameraTarget, m_UpsampleMat, 1);
			}

			// ****************************************
			// 8. Render volume light RT to screen
			// ****************************************

			CommandBuffer volumeBuffer = null;
			if (m_VolumeBuffers.ContainsKey(camera))
			{
				// Command buffer already exists, grab and clear the old one
				volumeBuffer = m_VolumeBuffers[camera];
				volumeBuffer.Clear();
			}
			else
			{
				// Need to create a new command buffer
				volumeBuffer = new CommandBuffer();
				volumeBuffer.name = "VolumeLightBuffer";
				m_VolumeBuffers.Add(camera, volumeBuffer);

				// Executes in both forward and deferred rendering paths
				camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, volumeBuffer);
			}

			if (OverCloudFogLight.fogLights != null && OverCloudFogLight.fogLights.Count > 0)
			{
				// Initialize and clear downsampled volume light render texture
				desc			= new RenderTextureDescriptor(bufferWidthDS, bufferHeightDS, RenderTextureFormat.DefaultHDR, 0);
				desc.vrUsage	= vrUsage;
				var volumeRT	= Shader.PropertyToID("_VolumeLightRT");
				volumeBuffer.GetTemporaryRT(volumeRT, desc);
				volumeBuffer.SetGlobalColor("_ClearColor", Color.black);
				volumeBuffer.Blit(null, volumeRT, m_ClearMat, 0);

				// Render fog lights
				volumeBuffer.SetRenderTarget(volumeRT);
				foreach (var fogLight in OverCloudFogLight.fogLights)
				{
					fogLight.BufferRender(volumeBuffer);
				}

				// Blur volume light buffer
				var blurResult = Shader.PropertyToID("_BlurResult");
				volumeBuffer.GetTemporaryRT(blurResult, desc);
				volumeBuffer.SetGlobalFloat("_DepthThreshold", 0.1f);
				volumeBuffer.SetGlobalVector("_PixelSize", new Vector2(1f / (float)bufferWidthDS, 1f / (float)bufferHeightDS));
				volumeBuffer.SetGlobalFloat("_BlurAmount", 1f);
				// Vertical blur pass
				volumeBuffer.Blit(volumeRT, blurResult, m_SeparableBlurMat, 2);
				// Horizontal blur pass
				volumeBuffer.Blit(blurResult, volumeRT, m_SeparableBlurMat, 3);

				// Upscale and transfer
				volumeBuffer.Blit(volumeRT, BuiltinRenderTextureType.CameraTarget, m_UpsampleMat, 2);

				volumeBuffer.ReleaseTemporaryRT(volumeRT);
				volumeBuffer.ReleaseTemporaryRT(blurResult);
			}

			// ****************************************
			// 9. Post-rendering cleanup
			// ****************************************
			//bufPost.ReleaseTemporaryRT(downsmpledDepthRT);
			//bufPost.ReleaseTemporaryRT(scatteringMaskRT);
			//bufPost.DisableShaderKeyword("RAIN_MASK_ENABLED");

			if (afterRender != null)
				afterRender.Invoke();
		}

		/// <summary>
		/// Called after OverCloudCamera finishes rendering. Do not call this from elsewhere or something might break.
		/// </summary>
		public static void CleanUp ()
		{
			cloudRT				= null;
			cloudDepthRT		= null;
			scatteringMaskRT	= null;
			volumeRT			= null;

			// This ensures the skybox does not sample the scattering mask for another camera (problem with reflection probes)
			Shader.SetGlobalTexture("_OC_ScatteringMask", Texture2D.whiteTexture);
		}

		#endregion

		#region Updates

		/// <summary>
		/// The main update function. Should be run every frame once by the main camera, NOT once for every camera!
		/// </summary>
		/// <param name="camera">The camera to use as reference when updating OverCloud. 
		/// This will affect where the cloud volume appears in the world (around the camera).</param>
		public static void CameraUpdate (Camera camera)
		{
			if (!camera || !instance)
				return;

			if (beforeCameraUpdate != null)
				beforeCameraUpdate.Invoke();

			instance.CheckComponents();

			PositionCloudVolume(camera);

			// Sky changed defaults to false
			skyChanged = false;

			// Update weather
			instance.UpdateWeather(camera);

			// Update time
			instance.UpdateTime();

			// Update environment lighting
			instance.UpdateLighting();

			// Update orbital elements
			instance.UpdateOrbital();

			if (afterCameraUpdate != null)
				afterCameraUpdate.Invoke();
		}

		/// <summary>
		/// Position the cloud volume around a camera. Needs to be called for clouds to look correct in the camera.
		/// </summary>
		/// <param name="camera">The camera to position the cloud volume around.</param>
		public static void PositionCloudVolume (Camera camera)
		{
			if (!instance || !instance.enabled)
				return;

			// Update cloud position
			instance.UpdatePosition(camera);

			// Update cloud compositor texture
			instance.UpdateCompositor(camera);

			// Update cloud shadows texture
			instance.UpdateCloudShadows();

			instance.UpdatePointLight(camera);
		}

		/// <summary>
		/// Update the cloud shadows render texture by projecting the cloud volume onto a flat plane
		/// </summary>
		void UpdateCloudShadows ()
		{
			if (!lighting.cloudShadows.enabled || (current.cloudShadowsDensity * current.cloudShadowsOpacity * current.cloudiness * current.macroCloudiness < Mathf.Epsilon))
			{
				// Skip rendering shadows if intensity/density is ~= 0
				Shader.SetGlobalTexture("_OC_CloudShadowsTex", Texture2D.blackTexture);
			}
			else
			{
				if (!m_CloudShadowsRT || m_CloudShadowsRT.width != (int)lighting.cloudShadows.resolution)
				{
					m_CloudShadowsRT = new RenderTexture((int)lighting.cloudShadows.resolution, (int)lighting.cloudShadows.resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				}

				// Scale compositor extents to get shadows coverage
				var shadowExtents		= m_WorldExtents;
				var center				= shadowExtents.center;
				shadowExtents.center	= Vector2.zero;
				shadowExtents.size		*= lighting.cloudShadows.coverage;
				shadowExtents.center	= center;

				Shader.SetGlobalVector("_OC_CloudShadowExtentsMinMax", new Vector4(shadowExtents.min.x, shadowExtents.min.y, shadowExtents.max.x, shadowExtents.max.y));
				Shader.SetGlobalVector("_OC_CloudShadowExtents", new Vector4(shadowExtents.width, shadowExtents.height, 1f / shadowExtents.width, 1f / shadowExtents.height));

				Graphics.Blit(null, m_CloudShadowsRT, m_UtilitiesMat, 0);

				if (lighting.cloudShadows.blur > Mathf.Epsilon)
				{ 
					// Blur cloud shadows
					var rt1 = RenderTexture.GetTemporary(m_CloudShadowsRT.width, m_CloudShadowsRT.height, 0, m_CloudShadowsRT.format, RenderTextureReadWrite.Linear);
					Shader.SetGlobalVector("_PixelSize", new Vector2(1f / (float)rt1.width, 1f / (float)rt1.height));
					Shader.SetGlobalFloat("_BlurAmount", lighting.cloudShadows.blur);
					// Vertical blur pass
					Graphics.Blit(m_CloudShadowsRT, rt1, m_SeparableBlurMat, 0);
					// Horizontal blur pass
					Graphics.Blit(rt1, m_CloudShadowsRT, m_SeparableBlurMat, 1);
					// Release RT
					RenderTexture.ReleaseTemporary(rt1);
				}

				Shader.SetGlobalTexture("_OC_CloudShadowsTex", m_CloudShadowsRT);
			}		
		}

		/// <summary>
		/// Update the lighting
		/// </summary>
		void UpdateLighting ()
		{
			if (!components.sun)
				return;

			// Default fade value
			moonFade = 1;

			// Update ambient lighting based on the zenith of the sun
			if (components.sun)
			{
				var sunDot = Vector3.Dot(components.sun.transform.forward, Vector3.down)*0.5f+0.5f;
				RenderSettings.ambientSkyColor		= lighting.ambient.sky.Evaluate(sunDot)		* lighting.ambient.multiplier;
				RenderSettings.ambientEquatorColor	= lighting.ambient.equator.Evaluate(sunDot)	* lighting.ambient.multiplier;
				RenderSettings.ambientGroundColor	= lighting.ambient.ground.Evaluate(sunDot)	* lighting.ambient.multiplier;

				// Lunar eclipse
				RenderSettings.ambientSkyColor		*= Color.Lerp(Color.white, atmosphere.lunarEclipseColor, lunarEclipse * lighting.ambient.lunarEclipseLightingInfluence);
				RenderSettings.ambientEquatorColor	*= Color.Lerp(Color.white, atmosphere.lunarEclipseColor, lunarEclipse * lighting.ambient.lunarEclipseLightingInfluence);
				RenderSettings.ambientGroundColor	*= Color.Lerp(Color.white, atmosphere.lunarEclipseColor, lunarEclipse * lighting.ambient.lunarEclipseLightingInfluence);
				// Solar eclipse
				RenderSettings.ambientSkyColor		*= Color.Lerp(Color.white, atmosphere.solarEclipseColor, solarEclipse);
				RenderSettings.ambientEquatorColor	*= Color.Lerp(Color.white, atmosphere.solarEclipseColor, solarEclipse);
				RenderSettings.ambientGroundColor	*= Color.Lerp(Color.white, atmosphere.solarEclipseColor, solarEclipse);


				if (components.moon)
				{
					// If we have both a sun and a moon, we need to fade the moon in when the sun goes down, and out when the sun goes up
					// Ideally, both lights would be active at the same time, but this is not practical/desired for most setups
					float horizon = Mathf.Max(components.sun.transform.forward.y, 0);
					moonFade = Mathf.Min(horizon, 1);
				}
			}

			// Desaturated ambient property
			Color ambient = RenderSettings.ambientSkyColor.linear;
			float h, s, v;
			Color.RGBToHSV(ambient, out h, out s, out v);
			Shader.SetGlobalColor("_OC_AmbientColor", Color.Lerp(ambient, Color.HSVToRGB(h, 0, v), lighting.cloudLighting.ambientDesaturation));
		}

		void UpdatePointLight (Camera camera)
		{
			if (OverCloudLight.lights != null && OverCloudLight.lights.Count > 0)
			{
				OverCloudLight closestLight = null;
				float closestDist = Mathf.Infinity;
				for (int i = 0; i < OverCloudLight.lights.Count; i++)
				{
					if (!OverCloudLight.lights[i].hasActiveLight || OverCloudLight.lights[i].type != OverCloudLight.Type.Point)
						continue;

					float dist = Vector3.Distance(camera.transform.position, OverCloudLight.lights[i].transform.position);
					if (dist < closestDist)
					{
						closestLight = OverCloudLight.lights[i];
						closestDist = dist;
					}
				}

				if (!closestLight)
				{
					Shader.DisableKeyword("OVERCLOUD_POINTLIGHT_ENABLED");
					return;
				}

				Shader.EnableKeyword("OVERCLOUD_POINTLIGHT_ENABLED");
				var pos = closestLight.transform.position;
				Shader.SetGlobalVector("_OC_PointLightPosRadius", new Vector4(pos.x, pos.y, pos.z, closestLight.pointRadius));
				Shader.SetGlobalVector("_OC_PointLightColor", closestLight.pointColor);
			}
			else
			{
				Shader.DisableKeyword("OVERCLOUD_POINTLIGHT_ENABLED");
			}
		}

		/// <summary>
		/// Update shader variables
		/// </summary>
		static TextureParameter	_OC_NoiseTex					= new TextureParameter("_OC_NoiseTex");
		static Vector2Parameter	_OC_NoiseScale					= new Vector2Parameter("_OC_NoiseScale");
		static FloatParameter	_OC_Timescale					= new FloatParameter("_OC_Timescale");
		static TextureParameter	_OC_3DNoiseTex					= new TextureParameter("_OC_3DNoiseTex");
		static Vector4Parameter	_OC_NoiseParams1				= new Vector4Parameter("_OC_NoiseParams1");
		static Vector2Parameter	_OC_NoiseParams2				= new Vector2Parameter("_OC_NoiseParams2");
		static FloatParameter	_OC_Precipitation				= new FloatParameter("_OC_Precipitation");
		static Vector2Parameter	_OC_CloudOcclusionParams		= new Vector2Parameter("_OC_CloudOcclusionParams");
		static Vector4Parameter	_OC_ShapeParams					= new Vector4Parameter("_OC_ShapeParams");
		static FloatParameter	_OC_NoiseErosion				= new FloatParameter("_OC_NoiseErosion");
		static Vector2Parameter	_OC_AlphaEdgeParams				= new Vector2Parameter("_OC_AlphaEdgeParams");

		static FloatParameter	_OC_CloudAltitude				= new FloatParameter("_OC_CloudAltitude");
		static FloatParameter	_OC_CloudPlaneRadius			= new FloatParameter("_OC_CloudPlaneRadius");
		static FloatParameter	_OC_CloudHeight					= new FloatParameter("_OC_CloudHeight");
		static FloatParameter	_OC_CloudHeightInv				= new FloatParameter("_OC_CloudHeightInv");

		static FloatParameter	_OC_NightScattering				= new FloatParameter("_OC_NightScattering");
		static Vector4Parameter	_OC_MieScatteringParams			= new Vector4Parameter("_OC_MieScatteringParams");

		static FloatParameter	_SkySunSize						= new FloatParameter("_SkySunSize");
		static FloatParameter	_SkyMoonSize					= new FloatParameter("_SkyMoonSize");
		static FloatParameter	_SkySunIntensity				= new FloatParameter("_SkySunIntensity");
		static FloatParameter	_SkyMoonIntensity				= new FloatParameter("_SkyMoonIntensity");
		static TextureParameter	_SkyMoonCubemap					= new TextureParameter("_SkyMoonCubemap");
		static TextureParameter	_SkyStarsCubemap				= new TextureParameter("_SkyStarsCubemap");
		static FloatParameter	_SkyStarsIntensity				= new FloatParameter("_SkyStarsIntensity");
		static Vector4Parameter	_SkySolarEclipse				= new Vector4Parameter("_SkySolarEclipse");
		static Vector4Parameter	_SkyLunarEclipse				= new Vector4Parameter("_SkyLunarEclipse");
		static FloatParameter	_LunarEclipseLightingInfluence	= new FloatParameter("_LunarEclipseLightingInfluence");

		static FloatParameter	_OC_GlobalWindMultiplier		= new FloatParameter("_OC_GlobalWindMultiplier");
		static Vector4Parameter	_OC_GlobalWetnessParams			= new Vector4Parameter("_OC_GlobalWetnessParams");
		static Vector4Parameter	_OC_GlobalRainParams			= new Vector4Parameter("_OC_GlobalRainParams");
		static Vector2Parameter	_OC_GlobalRainParams2			= new Vector2Parameter("_OC_GlobalRainParams2");

		static Vector4Parameter	_OC_Cloudiness					= new Vector4Parameter("_OC_Cloudiness");
		static Vector2Parameter	_OC_CloudSharpness				= new Vector2Parameter("_OC_CloudSharpness");
		static Vector2Parameter	_OC_CloudDensity				= new Vector2Parameter("_OC_CloudDensity");
		static Vector2Parameter	_OC_CloudShadowsParams			= new Vector2Parameter("_OC_CloudShadowsParams");

		static FloatParameter	_OC_CloudShadowsSharpen			= new FloatParameter("_OC_CloudShadowsSharpen");
		static TextureParameter	_OC_CloudShadowsEdgeTex			= new TextureParameter("_OC_CloudShadowsEdgeTex");
		static Vector4Parameter	_OC_CloudShadowsEdgeTexParams	= new Vector4Parameter("_OC_CloudShadowsEdgeTexParams");

		static FloatParameter	_OC_ScatteringMaskSoftness		= new FloatParameter("_OC_ScatteringMaskSoftness");
		static FloatParameter	_OC_ScatteringMaskFloor			= new FloatParameter("_OC_ScatteringMaskFloor");
		static Vector4Parameter	_OC_FogParams					= new Vector4Parameter("_OC_FogParams");
		static FloatParameter	_OC_FogBlend					= new FloatParameter("_OC_FogBlend");
		static ColorParameter	_OC_FogColor					= new ColorParameter("_OC_FogColor");
		static FloatParameter	_OC_FogHeight					= new FloatParameter("_OC_FogHeight");
		static Vector2Parameter	_OC_FogFalloffParams			= new Vector2Parameter("_OC_FogFalloffParams");

		static FloatParameter	_OC_AtmosphereExposure			= new FloatParameter("_OC_AtmosphereExposure");
		static FloatParameter	_OC_AtmosphereDensity			= new FloatParameter("_OC_AtmosphereDensity");
		static FloatParameter	_OC_AtmosphereFarClipFade		= new FloatParameter("_OC_AtmosphereFarClipFade");
		
		static Vector3Parameter	_OC_CurrentSunColor				= new Vector3Parameter("_OC_CurrentSunColor");
		static Vector3Parameter	_OC_CurrentMoonColor			= new Vector3Parameter("_OC_CurrentMoonColor");
		static Vector3Parameter	_OC_LightDir					= new Vector3Parameter("_OC_LightDir");
		static FloatParameter	_OC_LightDirYInv				= new FloatParameter("_OC_LightDirYInv");
		static Vector3Parameter	_OC_LightColor					= new Vector3Parameter("_OC_LightColor");
		static Vector3Parameter	_OC_ActualSunDir				= new Vector3Parameter("_OC_ActualSunDir");
		static ColorParameter	_OC_ActualSunColor				= new ColorParameter("_OC_ActualSunColor");
		static Vector3Parameter	_OC_ActualMoonDir				= new Vector3Parameter("_OC_ActualMoonDir");
		static ColorParameter	_OC_ActualMoonColor				= new ColorParameter("_OC_ActualMoonColor");

		ColorParameter			_OC_EarthColor					= new ColorParameter("_OC_EarthColor");

		void UpdateShaderProperties ()
		{
			if (current == null)
				return;

			if (beforeShaderParametersUpdate != null)
				beforeShaderParametersUpdate.Invoke();

			Shader.SetGlobalVector("_OverCloudOriginOffset", currentOriginOffset);

			// Check if the atmosphere needs recomputing
			if (m_LastAtmosphere == null ||
				m_LastAtmosphere.planetScale	!= atmosphere.precomputation.planetScale ||
				m_LastAtmosphere.heightScale	!= atmosphere.precomputation.heightScale ||
				m_LastAtmosphere.mie			!= atmosphere.precomputation.mie ||
				m_LastAtmosphere.rayleigh		!= atmosphere.precomputation.rayleigh ||
				m_LastAtmosphere.ozone			!= atmosphere.precomputation.ozone ||
				m_LastAtmosphere.phase			!= atmosphere.precomputation.phase)
			{
				InitializeAtmosphere();

				if (m_LastAtmosphere == null)
					m_LastAtmosphere = new Atmosphere.Precomputation();
				m_LastAtmosphere.planetScale	= atmosphere.precomputation.planetScale;
				m_LastAtmosphere.heightScale	= atmosphere.precomputation.heightScale;
				m_LastAtmosphere.mie			= atmosphere.precomputation.mie;
				m_LastAtmosphere.rayleigh		= atmosphere.precomputation.rayleigh;
				m_LastAtmosphere.ozone			= atmosphere.precomputation.ozone;
				m_LastAtmosphere.phase			= atmosphere.precomputation.phase;
			}

			// Rarely updated variables (CBUFFER OverCloudStatic)
			{
				// Compositor etc.
				if (volumetricClouds.noiseTexture)
				{
					_OC_NoiseTex.value = volumetricClouds.noiseTexture;
				}
				else
					Debug.LogError("OverCloud noise texture not set.");

				_OC_NoiseScale.value	= new Vector2(Mathf.Pow(volumetricClouds.noiseScale, 4), Mathf.Pow(volumetricClouds.noiseMacroScale, 4)) * 0.001f;
				_OC_Timescale.value		= weather.windTimescale;
				_OC_3DNoiseTex.value			= m_3DNoise;
				_OC_NoiseParams1.value			= new Vector4(volumetricClouds.noiseSettings.noiseTiling_A, volumetricClouds.noiseSettings.noiseIntensity_A, volumetricClouds.noiseSettings.noiseTiling_B, volumetricClouds.noiseSettings.noiseIntensity_B);
				_OC_NoiseParams2.value			= new Vector2(volumetricClouds.noiseSettings.turbulence, volumetricClouds.noiseSettings.riseFactor);
				_OC_Precipitation.value			= m_CurrentPreset.precipitation;
				_OC_CloudOcclusionParams.value	= new Vector2(lighting.cloudAmbientOcclusion.intensity, 1f / lighting.cloudAmbientOcclusion.heightFalloff);
				_OC_ShapeParams.value			= new Vector4(volumetricClouds.noiseSettings.shapeCenter, 1f / volumetricClouds.noiseSettings.shapeCenter, 1f / (1 - volumetricClouds.noiseSettings.shapeCenter), volumetricClouds.noiseSettings.baseDensityIncrease);
				_OC_NoiseErosion.value			= volumetricClouds.noiseSettings.erosion;
				_OC_AlphaEdgeParams.value		= new Vector2(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper);

				// Volumetric cloud plane
				_OC_CloudAltitude.value			= adjustedCloudPlaneAltitude;
				_OC_CloudPlaneRadius.value		= volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier;
				_OC_CloudHeight.value			= current.cloudPlaneHeight;
				_OC_CloudHeightInv.value		= 1f / current.cloudPlaneHeight;

				_OC_CloudShadowsSharpen.value	= lighting.cloudShadows.sharpen;
				_OC_CloudShadowsEdgeTex.value	= lighting.cloudShadows.edgeTexture;
				_OC_CloudShadowsEdgeTexParams.value	= new Vector4(
					lighting.cloudShadows.coverage,
					1f / (volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2 * lighting.cloudShadows.coverage),
					lighting.cloudShadows.edgeTextureIntensity,
					lighting.cloudShadows.coverage / lighting.cloudShadows.edgeTextureScale
				);

				// Lighting
				lighting.cloudLighting.UpdateShaderProperties();

				// Scattering & fog
				_OC_NightScattering.value		= atmosphere.nightScattering;
				// This is set per-camera in mRender instead
				//_OC_MieScatteringParams.value	= new Vector4(mieScatteringIntensity * 0.1f, mieScatteringPhase, mieScatteringFogPhase, Mathf.Pow(1-mieScatteringDistanceFade, 8));

				_SkySunSize.value						= atmosphere.sunSize;
				_SkyMoonSize.value						= atmosphere.moonSize;
				_SkySunIntensity.value					= atmosphere.sunIntensity;
				_SkyMoonIntensity.value					= atmosphere.moonIntensity;
				_SkyMoonCubemap.value					= atmosphere.moonAlbedo;
				_SkyStarsCubemap.value					= atmosphere.starsCubemap;
				_SkyStarsIntensity.value				= atmosphere.starsIntensity;
				_SkySolarEclipse.value					= new Vector4(atmosphere.solarEclipseColor.r, atmosphere.solarEclipseColor.g, atmosphere.solarEclipseColor.b, solarEclipse);
				_SkyLunarEclipse.value					= new Vector4(atmosphere.lunarEclipseColor.r, atmosphere.lunarEclipseColor.g, atmosphere.lunarEclipseColor.b, lunarEclipse);
				_LunarEclipseLightingInfluence.value	= lighting.ambient.lunarEclipseLightingInfluence;

				_OC_EarthColor.value					= atmosphere.earthColor;
			}

			// Compositor etc.
			_OC_GlobalWindMultiplier.value	= m_CurrentPreset.windMultiplier;
			_OC_GlobalWetnessParams.value	= new Vector4(m_CurrentPreset.wetnessRemap, weather.rain.albedoDarken, weather.rain.roughnessDecrease, 0);
			_OC_GlobalRainParams.value		= new Vector4(weather.rain.rippleIntensity, weather.rain.rippleScale, weather.rain.flowIntensity, weather.rain.flowScale);
			_OC_GlobalRainParams2.value		= new Vector2(weather.rain.rippleTimescale * 100, weather.rain.flowTimescale * 2);

			// Volumetric cloud plane
			float cloudiness_a = Mathf.Min(m_CurrentPreset.cloudiness		* 2 	, 1);
			float cloudiness_b = Mathf.Max(m_CurrentPreset.cloudiness		* 2 - 1, 0);
			float cloudiness_c = Mathf.Min(m_CurrentPreset.macroCloudiness  * 2		, 1);
			float cloudiness_d = Mathf.Max(m_CurrentPreset.macroCloudiness  * 2 - 1, 0);
			_OC_Cloudiness.value			= new Vector4(cloudiness_a, cloudiness_b, cloudiness_c, cloudiness_d);
			_OC_CloudSharpness.value		= new Vector2(m_CurrentPreset.sharpness, m_CurrentPreset.macroSharpness);
			_OC_CloudDensity.value			= new Vector2(m_CurrentPreset.opticalDensity, m_CurrentPreset.lightingDensity);
			_OC_CloudShadowsParams.value = new Vector2(m_CurrentPreset.cloudShadowsDensity, m_CurrentPreset.cloudShadowsOpacity);
			
			// Scattering & fog
			_OC_ScatteringMaskSoftness.value	= atmosphere.scatteringMask.softness;
			_OC_ScatteringMaskFloor.value		= atmosphere.scatteringMask.floor;
			_OC_FogParams.value					= new Vector4(Mathf.Pow(m_CurrentPreset.fogDensity, 16), m_CurrentPreset.fogDirectIntensity, m_CurrentPreset.fogAmbientIntensity, m_CurrentPreset.fogShadow);
			_OC_FogBlend.value					= 1f / Mathf.Pow(_OC_FogParams.value.x, m_CurrentPreset.fogBlend);
			_OC_FogColor.value					= m_CurrentPreset.fogAlbedo;
			_OC_FogHeight.value					= m_CurrentPreset.fogHeight * current.cloudPlaneAltitude;
			_OC_FogFalloffParams.value			= new Vector2(m_CurrentPreset.fogFalloff, 1f / m_CurrentPreset.fogFalloff);

			_OC_AtmosphereExposure.value		= atmosphere.exposure * 0.0003f;
			_OC_AtmosphereDensity.value			= atmosphere.density;
			_OC_AtmosphereFarClipFade.value		= atmosphere.farClipFade;

			// There are 3 different light color types:
			// 1. Actual color. This is the physical color of the orbital element in the sky (does not change with time of day)
			// 2. Current color. This is the current color of the orbital element in the sky (does change with time of day)
			// 3. Cloud light color. This is the light used to light the clouds, and is equivalent to one of the current colors

			Vector3 currentSunColor  = Vector3.zero;
			Vector3 currentMoonColor = Vector3.zero;
			if (components.sun)
			{
				currentSunColor = new Vector3(components.sun.color.r, components.sun.color.g, components.sun.color.b) * components.sun.intensity;
				_OC_CurrentSunColor.value = currentSunColor;
			}
			if (components.moon)
			{
				currentMoonColor = new Vector3(components.moon.color.r, components.moon.color.g, components.moon.color.b) * components.moon.intensity;
				_OC_CurrentMoonColor.value = currentMoonColor;
			}

			// Light the clouds based on the current dominant light (prioritize the sun)
			if (components.sun && components.sun.gameObject.activeInHierarchy && components.sun.intensity > 0.001f && (components.sun.color.r + components.sun.color.g + components.sun.color.b) > 0.001f)
			{
				_OC_LightDir.value		= components.sun.transform.forward;
				_OC_LightDirYInv.value	= 1f / components.sun.transform.forward.y;
				_OC_LightColor.value	= currentSunColor;
				dominantLight = components.sun;
				if (m_OverCloudSun)
					dominantOverCloudLight = m_OverCloudSun;
				else
					dominantOverCloudLight = null;
			}
			else if (components.moon && components.moon.gameObject.activeInHierarchy)
			{
				_OC_LightDir.value		= components.moon.transform.forward;
				_OC_LightDirYInv.value	=  1f / components.moon.transform.forward.y;
				_OC_LightColor.value	= currentMoonColor;
				dominantLight = components.moon;
				if (m_OverCloudMoon)
					dominantOverCloudLight = m_OverCloudMoon;
				else
					dominantOverCloudLight = null;
			}
			else
			{
				// Can't light the clouds if no lights are available
				_OC_LightColor.value = Vector3.zero;
				dominantLight = null;
				dominantOverCloudLight = null;
			}

			if (components.sun)
			{
				_OC_ActualSunDir.value		= components.sun.transform.forward;
				_OC_ActualSunColor.value	= atmosphere.actualSunColor;
			}
			else
			{
				_OC_ActualSunColor.value = Color.clear;
			}
			if (components.moon)
			{
				_OC_ActualMoonDir.value = components.moon.transform.forward;
				// Moon intensity is based on how much of the surface is lit by the sun
				if (components.sun)
					_OC_ActualMoonColor.value = new Color(atmosphere.actualMoonColor.r, atmosphere.actualMoonColor.g, atmosphere.actualMoonColor.b, 1-(Vector3.Dot(components.moon.transform.forward, components.sun.transform.forward)*0.5f+0.5f));
				else
					_OC_ActualMoonColor.value = atmosphere.actualMoonColor;
			}
			else
			{
				_OC_ActualMoonColor.value = Color.clear;
			}

			// Set custom floats
			for (int i = 0; i < m_CustomFloats.Length; i++)
			{
				if (m_CustomFloats[i].shaderParameter != "")
				{
					Shader.SetGlobalFloat(m_CustomFloats[i].shaderParameter, m_CurrentPreset.customFloats[i]);
				}
			}

			if (afterShaderParametersUpdate != null)
				afterShaderParametersUpdate.Invoke();
		}

		/// <summary>
		/// Update the weather
		/// </summary>
		/// <param name="camera">Camera to place weather effects around (lightning)</param>
		void UpdateWeather (Camera camera)
		{
			weather.windTime += Time.deltaTime * m_CurrentPreset.windMultiplier * weather.windTimescale * 10;
			Shader.SetGlobalFloat("_OC_GlobalWindTime", weather.windTime);

			// Cache last frame preset so we can determine if the sky changed later
			if (m_LastFramePreset == null)
				m_LastFramePreset = new WeatherPreset(m_CurrentPreset);
			else
				m_LastFramePreset.Lerp(m_LastFramePreset, m_CurrentPreset, 1);

			// Update prev/target
			if (m_TargetPreset == null || m_TargetPreset.name != activePreset)
			{
				FindTargetPreset();
				m_PrevPreset = new WeatherPreset(m_CurrentPreset);
				m_FadeTimer = 0;
			}

			if (m_TargetPreset != null)
			{
				m_FadeTimer = Mathf.Clamp01(m_FadeTimer + Time.deltaTime / (Application.isPlaying ? fadeDuration : editorFadeDuration));
				m_CurrentPreset.Lerp(m_PrevPreset, m_TargetPreset, m_FadeTimer);
			}

			// Sky changed check
			if (
				m_LastFramePreset.cloudiness			!= m_CurrentPreset.cloudiness			||
				m_LastFramePreset.macroCloudiness		!= m_CurrentPreset.macroCloudiness		||
				m_LastFramePreset.sharpness				!= m_CurrentPreset.sharpness			||
				m_LastFramePreset.macroSharpness		!= m_CurrentPreset.macroSharpness		||
				m_LastFramePreset.opticalDensity		!= m_CurrentPreset.opticalDensity		||
				m_LastFramePreset.lightingDensity		!= m_CurrentPreset.lightingDensity		||
				m_LastFramePreset.cloudShadowsDensity	!= m_CurrentPreset.cloudShadowsDensity	||
				m_LastFramePreset.cloudShadowsOpacity	!= m_CurrentPreset.cloudShadowsOpacity	||
				m_LastFramePreset.precipitation			!= m_CurrentPreset.precipitation		||
				m_LastFramePreset.fogDensity			!= m_CurrentPreset.fogDensity			||
				m_LastFramePreset.fogAlbedo				!= m_CurrentPreset.fogAlbedo			||
				m_LastFramePreset.fogDirectIntensity	!= m_CurrentPreset.fogDirectIntensity	||
				m_LastFramePreset.fogAmbientIntensity	!= m_CurrentPreset.fogAmbientIntensity	||
				m_LastFramePreset.fogHeight				!= m_CurrentPreset.fogHeight
				)
				skyChanged = true;

			// Update lightning
			if (weather.lightning.gameObject)
			{
				m_LightningTimer -= Time.deltaTime;

				if (m_LightningTimer <= 0 || (m_LightningRestrike && !weather.lightning.gameObject.activeInHierarchy))
				{
					if ((Random.Range(0, 1f) <= m_CurrentPreset.lightningChance ||
						(m_LightningRestrike && !weather.lightning.gameObject.activeInHierarchy)) &&
						weather.lightning.gameObject && (Application.isPlaying || weather.lightning.enableInEditor))
					{
						// Lightning strike
						Vector3 pos = camera.transform.position;
						pos.y = adjustedCloudPlaneAltitude;
						var dir = Random.insideUnitSphere;
						dir.y = 0;
						dir.Normalize();

						var camFwd = camera.transform.forward;
						camFwd.y = 0;
						camFwd.Normalize();
						dir = Vector3.Lerp(dir, camFwd, weather.lightning.cameraBias);

						pos += dir * Random.Range(weather.lightning.distanceMin, weather.lightning.distanceMax);
						var d = GetDensity2D(pos);

						// Lightning can only appear where there is cloud coverage
						if (d > weather.lightning.minimumDensity)
						{
							weather.lightning.gameObject.transform.position = pos;
							// Look at camera
							weather.lightning.gameObject.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
							// Play lightning by re-enabling object
							weather.lightning.gameObject.SetActive(false);
							weather.lightning.gameObject.SetActive(true);

							m_LightningTimer = Random.Range(weather.lightning.intervalMin, weather.lightning.intervalMax);

							// Restrike chance
							m_LightningRestrike = Random.Range(0f, 1f) <= weather.lightning.restrikeChance * m_CurrentPreset.lightningChance;
						}
					}
					else
					{
						m_LightningTimer = Random.Range(weather.lightning.intervalMin, weather.lightning.intervalMax);
					}
				}
			}

			// Update the rain ripple texture
			m_RainRippleMat.SetFloat("_TimeScale", 40);
			m_RainRippleMat.SetFloat("_Intensity", 1);
			Graphics.Blit(weather.rain.rippleTexture, m_RainRippleRT, m_RainRippleMat);
			Shader.SetGlobalTexture("_OC_RainRippleTex", m_RainRippleRT);
		}

		/// <summary>
		/// Update cloud compositor texture
		/// </summary>
		/// <param name="camera">The camera to position the compositor around</param>
		void UpdateCompositor (Camera camera)
		{
			Vector3 cameraPos = camera.transform.position;
			float span = volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2;
			var cellSpan = span / (float)volumetricClouds.compositorResolution;

			// Need to take origin offset into account when snapping
			cameraPos += currentOriginOffset;

			// Snap position to RT pixels (makes sampling stable when camera moves)
			cameraPos.x = Mathf.Round(cameraPos.x / cellSpan) * cellSpan;
			cameraPos.z = Mathf.Round(cameraPos.z / cellSpan) * cellSpan;

			// Convert back from actual world space to camera world space
			cameraPos -= currentOriginOffset;

			m_WorldExtents = new Rect(new Vector2(cameraPos.x, cameraPos.z), Vector2.one * span);

			m_WorldExtents.center -= m_WorldExtents.size * 0.5f;
		
			Shader.SetGlobalVector("_OC_CloudWorldExtentsMinMax", new Vector4(m_WorldExtents.min.x, m_WorldExtents.min.y, m_WorldExtents.max.x, m_WorldExtents.max.y));
			Shader.SetGlobalVector("_OC_CloudWorldExtents", new Vector4(m_WorldExtents.width, m_WorldExtents.height, 1f / m_WorldExtents.width, 1f / m_WorldExtents.height));
			Shader.SetGlobalVector("_OC_CloudWorldPos", new Vector3(cameraPos.x, 0, cameraPos.z));
			Graphics.Blit(null, m_CompositorRT, m_CompositorMat);

			// Blur compositor
			// Set up RTs + material
			var rt1 = RenderTexture.GetTemporary(m_CompositorRT.width, m_CompositorRT.height, 0, m_CompositorRT.format, RenderTextureReadWrite.Linear);
			Shader.SetGlobalVector("_PixelSize", new Vector2(1f / (float)rt1.width, 1f / (float)rt1.height));
			Shader.SetGlobalFloat("_BlurAmount", volumetricClouds.compositorBlur);
			// Downscale + vertical blur pass
			Graphics.Blit(m_CompositorRT, rt1, m_SeparableBlurMat, 4);
			// Horizontal blur pass
			Graphics.Blit(rt1, m_CompositorRT, m_SeparableBlurMat, 5);

			// Release temporaries
			RenderTexture.ReleaseTemporary(rt1);

			// Bind compositor texture
			Shader.SetGlobalTexture("_OC_CompositorTex", m_CompositorRT);
		}

		/// <summary>
		/// Update the position of the cloud layer
		/// </summary>
		/// <param name="camera">The camera to position the cloud layer around</param>
		void UpdatePosition (Camera camera)
		{
			int cellCount = (int)Mathf.Floor(Mathf.Sqrt(volumetricClouds.particleCount));
			float cellSpan = (volumetricClouds.cloudPlaneRadius * 2) / (float)cellCount;

			var pos = new Vector3(camera.transform.position.x, 0, camera.transform.position.z);
			pos += new Vector3(currentOriginOffset.x, 0, currentOriginOffset.z);

			// Lod
			float lodCellSpan = (volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier * 2) / (float)cellCount;
			var lodPos = pos;

			// Shaders need this to calculate density
			Shader.SetGlobalVector("_OC_CellSpan", new Vector2(cellSpan, lodCellSpan));

			// Snap position
			pos.x = Mathf.Round(pos.x / cellSpan) * cellSpan;
			pos.z = Mathf.Round(pos.z / cellSpan) * cellSpan;
			pos -= new Vector3(currentOriginOffset.x, 0, currentOriginOffset.z);
			pos.y += adjustedCloudPlaneAltitude;
			m_CloudObject.transform.position = pos;// + new Vector3(cellSpan * 0.5f, 0, cellSpan * 0.5f);
			m_CloudObject.transform.rotation = Quaternion.identity;

			// Snap lod position
			lodPos.x = Mathf.Round(lodPos.x / lodCellSpan) * lodCellSpan;
			lodPos.z = Mathf.Round(lodPos.z / lodCellSpan) * lodCellSpan;
			lodPos -= new Vector3(currentOriginOffset.x, 0, currentOriginOffset.z);
			lodPos.y += adjustedCloudPlaneAltitude;
			m_LodObject.transform.position = lodPos;
			m_LodObject.transform.rotation = Quaternion.identity;

			// Check if we need to reinitialize the cloud meshes (settings changed)
			if (!m_Filter.sharedMesh ||
				m_Filter.sharedMesh.vertexCount != cellCount * cellCount * 4 ||
				Mathf.Abs(volumetricClouds.cloudPlaneRadius - m_LastRadius) > Mathf.Epsilon ||
				Mathf.Abs(volumetricClouds.lodRadiusMultiplier - m_LastLodMultiplier) > Mathf.Epsilon
				)
			{
				InitializeMeshes();
			}

			// Cache for next frame check
			m_LastLodMultiplier	= volumetricClouds.lodRadiusMultiplier;
			m_LastRadius		= volumetricClouds.cloudPlaneRadius;

			if (Vector3.Distance(pos, m_LastPos) > Mathf.Epsilon)
			{
				// Mesh moved: update data

				if (m_PropBlock == null)
					m_PropBlock = new MaterialPropertyBlock();

				m_Renderer.GetPropertyBlock(m_PropBlock);

				m_PropBlock.SetVector("_RandomRange", new Vector2(cellSpan, cellSpan));
				m_PropBlock.SetFloat("_NearRadius", 0);
				m_PropBlock.SetFloat("_Radius", volumetricClouds.cloudPlaneRadius);
				m_PropBlock.SetFloat("_RadiusMax", current.cloudPlaneHeight);
				m_PropBlock.SetVector("_ParticleScale", new Vector2(1, 1));
				//m_PropBlock.SetFloat("_ParticleScale",  1);
				m_PropBlock.SetVector("_CloudPosition", m_CloudObject.transform.position);
				m_PropBlock.SetVector("_CloudExtents", Vector3.one * 1f / volumetricClouds.cloudPlaneRadius);

				m_Renderer.SetPropertyBlock(m_PropBlock);
				m_LastPos = pos;
			}

			if (Vector3.Distance(lodPos, m_LastLodPos) > Mathf.Epsilon)
			{
				// Lod moved: update data

				if (m_LodPropBlock == null)
					m_LodPropBlock = new MaterialPropertyBlock();

				m_LodRenderer.GetPropertyBlock(m_LodPropBlock);

				m_LodPropBlock.SetVector("_RandomRange", new Vector2(lodCellSpan, cellSpan));
				m_LodPropBlock.SetFloat("_NearRadius", volumetricClouds.cloudPlaneRadius);
				m_LodPropBlock.SetFloat("_Radius", volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier);
				m_LodPropBlock.SetFloat("_RadiusMax", current.cloudPlaneHeight);
				m_LodPropBlock.SetVector("_ParticleScale", new Vector2(volumetricClouds.lodParticleSize, 1f / volumetricClouds.lodParticleSize));
				//m_LodPropBlock.SetFloat("_ParticleScale",  m_LodSize);
				m_LodPropBlock.SetVector("_CloudPosition", m_CloudObject.transform.position);
				m_LodPropBlock.SetVector("_CloudExtents", Vector3.one * 1f / (volumetricClouds.cloudPlaneRadius * volumetricClouds.lodRadiusMultiplier));

				m_LodRenderer.SetPropertyBlock(m_LodPropBlock);
				m_LastLodPos = lodPos;
			}
		}

		/// <summary>
		/// Calculate azimuthial coordinates based on Right Ascension and Declination
		/// </summary>
		/// <param name="RA">Right Ascension </param>
		/// <param name="Decl">Declination</param>
		void AzimuthialCoordiante (float RA, float Decl, out float phi, out float theta)
		{
			float HA = m_LST - RA;
			// Now it's time to convert our objects HA and Decl to local azimuth and altitude. To do that, we also must know lat, our local latitude.
			// Then we proceed as follows:
			float x = Mathf.Cos(HA) * Mathf.Cos(Decl);
			float y = Mathf.Sin(HA) * Mathf.Cos(Decl);
			float z = Mathf.Sin(Decl);

			float xhor = x * Mathf.Sin(timeOfDay.latitude * Mathf.Deg2Rad) - z * Mathf.Cos(timeOfDay.latitude * Mathf.Deg2Rad);
			float yhor = y;
			float zhor = x * Mathf.Cos(timeOfDay.latitude * Mathf.Deg2Rad) + z * Mathf.Sin(timeOfDay.latitude * Mathf.Deg2Rad);

			float az  = Mathf.Atan2( yhor, xhor ) + Mathf.PI;
			float alt = Mathf.Asin( zhor ); // = Mathf.Atan2( zhor, Mathf.Sqrt(xhor*xhor+yhor*yhor) );

			phi = az;
			theta = (Mathf.PI * 0.5f) - alt;
		}

		/// <summary>
		/// Convert azimuthal coordiantes to cartesian ones
		/// </summary>
		/// <param name="phi"></param>
		/// <param name="theta"></param>
		/// <returns></returns>
		Vector3 CartesianCoordinate (float phi, float theta)
		{
			float cosPhi	= Mathf.Cos( phi );
			float sinPhi	= Mathf.Sin( phi );
			float cosTheta	= Mathf.Cos( theta );
			float sinTheta	= Mathf.Sin( theta );
			
			Vector3 v;
			v.x = sinPhi * sinTheta;
			v.y = cosTheta;
			v.z = cosPhi * sinTheta;
			
			return v;
		}

		/// <summary>
		/// Update time (and date)
		/// </summary>
		void UpdateTime ()
		{
			if (m_LastFrameTimeOfDay == null)
				m_LastFrameTimeOfDay = new TimeOfDay();

			// Cache previous frame time so we can do sky changed check later
			m_LastFrameTimeOfDay.latitude	= timeOfDay.latitude;
			m_LastFrameTimeOfDay.longitude	= timeOfDay.longitude;
			m_LastFrameTimeOfDay.year		= timeOfDay.year;
			m_LastFrameTimeOfDay.month		= timeOfDay.month;
			m_LastFrameTimeOfDay.day		= timeOfDay.day;
			m_LastFrameTimeOfDay.time		= timeOfDay.time;

			bool play = timeOfDay.play && (!Application.isPlaying ? timeOfDay.playInEditor : true);
			if (timeOfDay.useLocalTime)
			{
				var dt = DateTime.Now;
				timeOfDay.year = dt.Year;
				timeOfDay.month = dt.Month;
				timeOfDay.day = dt.Day;
				timeOfDay.time =
					dt.Hour +
					(float)dt.Minute / (24f * 60f) +
					(float)dt.Millisecond / (24f * 60f * 60f * 1000f);
			}
			else if (play)
			{
				timeOfDay.Advance();
			}

			// Sky changed check
			if (
				m_LastFrameTimeOfDay.latitude	!= timeOfDay.latitude	||
				m_LastFrameTimeOfDay.longitude	!= timeOfDay.longitude	||
				m_LastFrameTimeOfDay.year		!= timeOfDay.year		||
				m_LastFrameTimeOfDay.month		!= timeOfDay.month		||
				m_LastFrameTimeOfDay.day		!= timeOfDay.day		||
				m_LastFrameTimeOfDay.time		!= timeOfDay.time
				)
				skyChanged = true;
		}

		/// <summary>
		/// Update orbital elements (sun + moon position/direction)
		/// </summary>
		void UpdateOrbital ()
		{
			if (timeOfDay.enable)
			{
				// http://www.stjarnhimlen.se/comp/ppcomp.html

				// Generate "day number"
				float d = timeOfDay.dayNumber;
				// The obliquity of the ecliptic, i.e. the "tilt" of the Earth's axis of rotation
				float ecl = (23.4393f - 3.563E-7f * d) * Mathf.Deg2Rad;
				if (components.sun)
				{
					// Calculate sun position/direction

					// Orbital elements of the sun (in degrees, so we need to convert them to radians)
					//float N = (0.0f) * Mathf.Deg2Rad;
					//float i = (0.0f) * Mathf.Deg2Rad;
					float w = (282.9404f + 4.70935E-5f * d) * Mathf.Deg2Rad;
					float a = 1.000000f; // (AU)
					float e = 0.016709f - 1.151E-9f * d;
					float M = (356.0470f + 0.9856002585f * d) * Mathf.Deg2Rad;

					// First, compute the eccentric anomaly E from the mean anomaly M and from the eccentricity e
					float E = M + e * Mathf.Sin(M) * ( 1.0f + e * Mathf.Cos(M) );

					// Then compute the Sun's distance r and its true anomaly v from:
					float xv = a * ( Mathf.Cos(E) - e );
					float yv = a * ( Mathf.Sqrt(1.0f - e*e) * Mathf.Sin(E) );
					float v = Mathf.Atan2( yv, xv );
					float r = Mathf.Sqrt( xv*xv + yv*yv );

					// Sun's mean longitude (radians)
					float Ls = v + w;

					// Calculate LST (in degrees, then back)
					float GMST0 = Ls * Mathf.Rad2Deg + 180.0f;
					float GMST = (float)(GMST0 + timeOfDay.time * 15);
					m_LST = (GMST + timeOfDay.longitude) * Mathf.Deg2Rad;

					// Convert lonsun, r to ecliptic rectangular geocentric coordinates xs, ys:
					float xs = r * Mathf.Cos(Ls);
					float ys = r * Mathf.Sin(Ls);
					// (since the Sun always is in the ecliptic plane, zs is of course zero). xs, ys is the Sun's position in a coordinate system in the plane of the ecliptic.
					// To convert this to equatorial, rectangular, geocentric coordinates, compute:
					float xe = xs;
					float ye = ys * Mathf.Cos(ecl);
					float ze = ys * Mathf.Sin(ecl);
					// Finally, compute the Sun's Right Ascension (RA) and Declination (Decl):
					float RA  = Mathf.Atan2( ye, xe );
					float Decl = Mathf.Asin(ze); // = Mathf.Atan2( ze, Mathf.Sqrt(xe*xe+ye*ye) );

					// Azimuthial coordinates
					float phi, theta;
					AzimuthialCoordiante(RA, Decl, out phi, out theta);

					components.sun.transform.forward = CartesianCoordinate(phi, theta) * -1; // Position -> Direction = flip sign
				}

				if (components.moon && timeOfDay.affectsMoon)
				{
					// Calculate moon position/direction

					// Orbital elements of the moon (in degrees, so we need to convert them to radians)
					float N = (125.1228f - 0.0529538083f * d) * Mathf.Deg2Rad;
					float i = (5.1454f) * Mathf.Deg2Rad;
					float w = (318.0634f + 0.1643573223f * d) * Mathf.Deg2Rad;
					float a = 60.2666f; // (Earth radii)
					float e = 0.054900f;
					float M = (115.3654f + 13.0649929509f * d) * Mathf.Deg2Rad;

					// First, compute the eccentric anomaly E from the mean anomaly M and from the eccentricity e
					float E = M + e * Mathf.Sin(M) * ( 1.0f + e * Mathf.Cos(M) );

					// Then compute the Sun's distance r and its true anomaly v from:
					float xv = a * ( Mathf.Cos(E) - e );
					float yv = a * ( Mathf.Sqrt(1.0f - e*e) * Mathf.Sin(E) );
					float v = Mathf.Atan2( yv, xv );
					float r = Mathf.Sqrt( xv*xv + yv*yv );

					float xh = r * (Mathf.Cos(N) * Mathf.Cos(v+w) - Mathf.Sin(N) * Mathf.Sin(v+w) * Mathf.Cos(i));
					float yh = r * (Mathf.Sin(N) * Mathf.Cos(v+w) + Mathf.Cos(N) * Mathf.Sin(v+w) * Mathf.Cos(i));
					float zh = r * (Mathf.Sin(v+w) * Mathf.Sin(i));

					// Equatorial, rectangular, geocentric coordinates
					float xe = xh;
					float ye = yh * Mathf.Cos(ecl) - zh * Mathf.Sin(ecl);
					float ze = yh * Mathf.Sin(ecl) + zh * Mathf.Cos(ecl);
					// Moon's Right Ascension (RA) and Declination (Decl)
					float RA  = Mathf.Atan2( ye, xe );
					float Decl = Mathf.Atan2( ze, Mathf.Sqrt(xe*xe+ye*ye) );

					// Azimuthial coordinates
					float phi, theta;
					AzimuthialCoordiante(RA, Decl, out phi, out theta);

					components.moon.transform.forward = CartesianCoordinate(phi, theta) * -1; // Position -> Direction = flip sign
				}

				
			}
		}
		#endregion

		#region Setters

		/// <summary>
		/// Set the current target weather preset
		/// </summary>
		/// <param name="preset">Name of the preset to set</param>
		public static void SetWeatherPreset (string preset)
		{
			SetWeatherPreset(preset, instance.fadeDuration);
		}

		/// <summary>
		///  Set the current target weather preset, and specify the fade duration
		/// </summary>
		/// <param name="preset">Name of the preset to use</param>
		/// <param name="fadeDuration">Fade duration (in seconds)</param>
		public static void SetWeatherPreset (string preset, float fadeDuration)
		{
			instance.fadeDuration = fadeDuration;
			if (instance)
				instance.activePreset = preset;
		}

		#endregion

		#region Utilities

		int t0;
		int t1;
		int t2;
		int t3;
		int t4;
		int t5;

		Vector3[] vertices;
		int[] triangles;
		float[] distances;

		void QuicksortTriangles(int left, int right)
		{
			int i = left;
			int j = right;
			float pivot = distances[(i + j) / 2];

			while (i <= j)
			{
				while (distances[i] < pivot)
				{
					i++;
				}

				while (distances[j] > pivot)
				{
					j--;
				}

				if (i <= j)
				{
					// Swap distances
					float tmp = distances[i];
					distances[i] = distances[j];
					distances[j] = tmp;

					// Swap quads
					t0 = triangles[i * 6 + 0];
					t1 = triangles[i * 6 + 1];
					t2 = triangles[i * 6 + 2];
					t3 = triangles[i * 6 + 3];
					t4 = triangles[i * 6 + 4];
					t5 = triangles[i * 6 + 5];
					triangles[i * 6 + 0] = triangles[j * 6 + 0];
					triangles[i * 6 + 1] = triangles[j * 6 + 1];
					triangles[i * 6 + 2] = triangles[j * 6 + 2];
					triangles[i * 6 + 3] = triangles[j * 6 + 3];
					triangles[i * 6 + 4] = triangles[j * 6 + 4];
					triangles[i * 6 + 5] = triangles[j * 6 + 5];
					triangles[j * 6 + 0] = t0;
					triangles[j * 6 + 1] = t1;
					triangles[j * 6 + 2] = t2;
					triangles[j * 6 + 3] = t3;
					triangles[j * 6 + 4] = t4;
					triangles[j * 6 + 5] = t5;

					i++;
					j--;
				}
			}

			// Recursive calls
			if (left < j)
			{
				QuicksortTriangles(left, j);
			}

			if (i < right)
			{
				QuicksortTriangles(i, right);
			}
		}

		/// <summary>
		/// Sort the triangles of a mesh back-to-front around (0, 0, 0)
		/// </summary>
		/// <param name="mesh"></param>
		/// <returns>Sorted mesh</returns>
		public Mesh SortTriangles(Mesh mesh)
		{
			// Transform camera position to object space
			//camPos = transform.InverseTransformPoint(camPos);

			var camPos = Vector3.zero;

			//mf.sharedMesh.MarkDynamic();
			//var mesh = m_Filter.sharedMesh;

			// Update stored arrays
			//if (vertices == null)
				vertices = mesh.vertices;
			//if (triangles == null)
				triangles = mesh.triangles;
			//if (distances == null)
				distances = new float[mesh.vertices.Length / 4];

			//var triangles = mesh.triangles;

			// Cache quad distances
			int u = 0;
			for (int i = 0; i < triangles.Length; i += 6)
			{
				// Average position of quad
				distances[u] = -Vector3.Distance(camPos,
						(vertices[triangles[i + 0]] +
						 vertices[triangles[i + 1]] +
						 vertices[triangles[i + 2]] +
						 vertices[triangles[i + 3]] +
						 vertices[triangles[i + 4]] +
						 vertices[triangles[i + 5]]) * 0.16666f);
				u++;
			}

			QuicksortTriangles(0, distances.Length - 1);
			mesh.triangles = triangles;

			return mesh;
		}

		/// <summary>
		/// Smoothstep implementation
		/// </summary>
		/// <param name="edge0">Lower edge</param>
		/// <param name="edge1">Upper edge</param>
		/// <param name="x">Value to filter</param>
		/// <returns>Filtered result</returns>
		static float smoothstep(float edge0, float edge1, float x)
		{
			float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
			return t * t * (3 - 2 * t);
		}
		#endregion

		#region Density Probing
		/// <summary>
		/// CPU variant of density function
		/// </summary>
		/// <param name="worldPos">Sample position in world space</param>
		/// <returns>Density at sample position</returns>
		static float GetDensity2D (Vector3 worldPos)
		{
			worldPos += currentOriginOffset;

			// Local density
			// float2 uv = (worldPos.xz + float2(1, 0) * _OC_GlobalWindTime) * _OC_NoiseScale.x;
			Vector2 uv = (new Vector2(worldPos.x, worldPos.z) + new Vector2(1, 0) * weather.windTime) * _OC_NoiseScale.value.x;

			// float density = float density = tex2D(_OC_NoiseTex, uv).r;
			float density = volumetricClouds.noiseTexture.GetPixelBilinear(uv.x, uv.y).r;

			// density = max(density - (1 - _OC_Cloudiness.x), 0);
			density = Mathf.Max(density - (1 - _OC_Cloudiness.value.x), 0);

			// density = lerp(_OC_Cloudiness.y, 1, density);
			density = Mathf.Lerp(_OC_Cloudiness.value.y, 1, density);

			// density = smoothstep(_OC_CloudSharpness.x * 0.499, 1 - _OC_CloudSharpness.x * 0.499, density);
			density = smoothstep(_OC_CloudSharpness.value.x * 0.499f, 1 - _OC_CloudSharpness.value.x * 0.499f, density);

			// Macro density
			// uv = (worldPos.xz + float2(1, 0) * _OC_GlobalWindTime) * _OC_NoiseScale.y;
			uv = (new Vector2(worldPos.x, worldPos.z) + new Vector2(1f, 0) * weather.windTime) * _OC_NoiseScale.value.y;

			// float macroDensity = tex2D(_OC_NoiseTex, uv).g;
			float macroDensity = volumetricClouds.noiseTexture.GetPixelBilinear(uv.x, uv.y).g;

			// macroDensity = max(macroDensity - (1 - _OC_Cloudiness.z), 0);
			macroDensity = Mathf.Max(macroDensity - (1 - _OC_Cloudiness.value.z), 0);

			// macroDensity = lerp(_OC_Cloudiness.w, 1, macroDensity);
			macroDensity = Mathf.Lerp(_OC_Cloudiness.value.w, 1, macroDensity);

			// macroDensity = smoothstep(_OC_CloudSharpness.y * 0.499, 1 - _OC_CloudSharpness.y * 0.499, macroDensity);
			macroDensity = smoothstep(_OC_CloudSharpness.value.y * 0.499f, 1 - _OC_CloudSharpness.value.y * 0.499f, macroDensity);

			// density *= macroDensity;
			density *= macroDensity;

			// Adjust for particle culling
			density = smoothstep(0.1f, 0.5f, density);

			// Multiply with optical density, otherwise the reported density will heavily mismatch the appearance of the clouds
			density *= Mathf.Min(current.opticalDensity, 1);

			return density;
		}

		/// <summary>
		/// Get the volumetric cloud density of the cloud volume at a point.
		/// </summary>
		/// <param name="worldPos">The point to sample.</param>
		/// <param name="density2D">The 2D density at the point.</param>
		/// <returns>The density at the point.</returns>
		static float GetDensity3D (Vector3 worldPos, out float density2D)
		{
			density2D = smoothstep(volumetricClouds.noiseSettings.alphaEdgeLower, volumetricClouds.noiseSettings.alphaEdgeUpper, GetDensity2D(worldPos));
			float density3D = 0;
			if (density2D > Mathf.Epsilon && IsInsideCloudVolume(worldPos))
			{
				float height = worldPos.y - (_OC_CloudAltitude.value - _OC_CloudHeight.value);
				height *= _OC_CloudHeightInv.value * 0.5f;

				if (height < _OC_ShapeParams.value.x)
				{
					height *= _OC_ShapeParams.value.y;
				}
				else
				{
					height = 1 - (height - _OC_ShapeParams.value.x) * _OC_ShapeParams.value.z * 1.5f;
				}

				density3D = density2D * height;
			}

			return density3D;
		}

		/// <summary>
		/// Get the volumetric cloud density of the cloud volume at a point.
		/// </summary>
		/// <param name="worldPos">The point to sample.</param>
		/// <returns>The density at the point.</returns>
		static float GetDensity3D (Vector3 worldPos)
		{
			float foo;
			return GetDensity3D(worldPos, out foo);
		}

		/// <summary>
		/// CPU variant of radius function
		/// </summary>
		/// <param name="density">Input density</param>
		/// <returns>Radius based on density</returns>
		static float Radius (float density)
		{
			return current.cloudPlaneHeight * density;
		}

		/// <summary>
		/// Sample the cloud density at the specified sample position
		/// </summary>
		/// <param name="worldPos">Sample position in world space</param>
		/// <returns>Cloud density at sample position</returns>
		public static CloudDensity GetDensity (Vector3 worldPos)
		{
			var cloudDensity = new CloudDensity();

			if (!instance)
				return cloudDensity;

			float density2D;
			float density3D = GetDensity3D(worldPos, out density2D);

			cloudDensity.density	= density3D;
			cloudDensity.coverage	= density2D;
			cloudDensity.rain		= density2D * (worldPos.y < current.cloudPlaneAltitude ? 1 : density3D) * instance.m_CurrentPreset.precipitation;

			return cloudDensity;
		}

		/// <summary>
		/// Check if a position if above the entire volumetric cloud volume.
		/// </summary>
		/// <param name="position">The position to check against.</param>
		/// <returns>True if above the volume, false otherwise.</returns>
		public static bool IsAboveCloudVolume (Vector3 position)
		{
			return position.y > (adjustedCloudPlaneAltitude + current.cloudPlaneHeight);
		}

		/// <summary>
		/// Check if a position if below the entire volumetric cloud volume.
		/// </summary>
		/// <param name="position">The position to check against.</param>
		/// <returns>True if below the volume, false otherwise.</returns>
		public static bool IsBelowCloudVolume (Vector3 position)
		{
			return position.y < (adjustedCloudPlaneAltitude - current.cloudPlaneHeight);
		}

		/// <summary>
		/// Check if a position if inside the volumetric cloud volume.
		/// </summary>
		/// <param name="position">The position to check against.</param>
		/// <returns>True if inside the volume, false otherwise.</returns>
		public static bool IsInsideCloudVolume (Vector3 position)
		{
			return Mathf.Abs(position.y - adjustedCloudPlaneAltitude) < current.cloudPlaneHeight;
		}

		/// <summary>
		/// Calculate the cloud visibility between two points.
		/// </summary>
		/// <param name="p0">The first point.</param>
		/// <param name="p1">The second point.</param>
		/// <param name="sampleCount">The number of samples to use. More samples will result in a more accurate result, but will perform worse.</param>
		/// <returns>The cloud visibility between two points.</returns>
		public static float CloudVisibility (Vector3 p0, Vector3 p1, int sampleCount = 32)
		{
			// Adjust the volume size to better represent the visual volume
			float upper = adjustedCloudPlaneAltitude + current.cloudPlaneHeight * 0.5f;
			float lower = adjustedCloudPlaneAltitude - current.cloudPlaneHeight * 0.5f;

			// No need to calculate anything if both points are on the same side + outside of the volume
			if (p0.y < lower && p1.y < lower ||
				p0.y > upper && p1.y > upper)
				return 0;

			// Project points onto volume to maximize sample usage
			if (p0.y > upper || p0.y < lower)
			{
				var dir = (p1 - p0).normalized;
				if (p0.y > upper)
				{
					// Project onto upper bound
					p0 = p0 + dir * ((p0.y - upper) / Mathf.Abs(dir.y));
				}
				else
				{
					// Project onto lower bound
					p0 = p0 - dir * ((p0.y - lower) / Mathf.Abs(dir.y));
				}
			}
			if (p1.y > upper || p1.y < lower)
			{
				var dir = (p0 - p1).normalized;
				if (p1.y > upper)
				{
					// Project onto upper bound
					p1 = p1 + dir * ((p1.y - upper) / Mathf.Abs(dir.y));
				}
				else
				{
					// Project onto lower bound
					p1 = p1 - dir * ((p1.y - lower) / Mathf.Abs(dir.y));
				}
			}

			float scaleValue = Vector3.Distance(p0, p1) / (current.cloudPlaneHeight * 0.5f) / (float)sampleCount;

			float alpha = 0;

			for (int i = 0; i < sampleCount; i++)
			{
				var t = Vector3.Lerp(p0, p1, (float)i / (float)(sampleCount-1));
				float a = GetDensity3D(t) * _OC_CloudDensity.value.x;

				alpha += smoothstep(_OC_AlphaEdgeParams.value.x, _OC_AlphaEdgeParams.value.y, a);
			}

			alpha *= scaleValue;

			return Mathf.Clamp01(alpha);
		}
		#endregion

		#region Custom Floats
		public int customFloatsCount { get { return m_CustomFloats.Length; } }

		/// <summary>
		/// Get the current value of a custom float parameter.
		/// </summary>
		/// <param name="name">The name of the custom float parameter.</param>
		/// <returns>The current value of the custom float parameter if found, otherwise 0.</returns>
		public static float GetCustomFloat (string name)
		{
			return current.GetCustomFloat(name);
		}

		/// <summary>
		/// Get the index of a custom float from its name.
		/// </summary>
		/// <param name="name">The name of the custom float.</param>
		/// <returns>The index of the custom float if found, otherwise -1.</returns>
		public int GetCustomFloatIndex (string name)
		{
			for (int i = 0; i < m_CustomFloats.Length; i++)
			{
				if (m_CustomFloats[i].name == name)
					return i;
			}

			return -1;
		}

		public string GetCustomFloatName (int index)
		{
			return m_CustomFloats[index].name;
		}

		public void SetCustomFloatName (int index, string name)
		{
			m_CustomFloats[index].name = name;
		}

		public string GetCustomFloatShaderParameter (int index)
		{
			return m_CustomFloats[index].shaderParameter;
		}

		public void SetCustomFloatShaderParameter (int index, string shaderParameter)
		{
			m_CustomFloats[index].shaderParameter = shaderParameter;
		}

		/// <summary>
		/// Add a new custom float.
		/// </summary>
		public void AddCustomFloat ()
		{
			var customFloat = new CustomFloat();
			customFloat.name = "MyCustomFloat";
			customFloat.shaderParameter = "_MyCustomFloat";

			var tmp = new List<CustomFloat>(m_CustomFloats);
			tmp.Add(customFloat);
			m_CustomFloats = tmp.ToArray();

			m_CurrentPreset.AddCustomFloat();
			m_PrevPreset.AddCustomFloat();
			m_LastFramePreset.AddCustomFloat();

			// Add to all presets
			for (int i = 0; i < m_Presets.Length; i++)
				m_Presets[i].AddCustomFloat();

			// Update selection index
			drawerSelectedCustomFloat = m_CustomFloats.Length-1;

			// Need to mark scene as dirty to not lose changes
			#if UNITY_EDITOR
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			#endif
		}

		/// <summary>
		/// Delete the currently selected custom float.
		/// </summary>
		public void DeleteCustomFloat ()
		{
			if (drawerSelectedCustomFloat < 0 || drawerSelectedCustomFloat > m_CustomFloats.Length)
				return;

			var tmp = new List<CustomFloat>(m_CustomFloats);
			tmp.RemoveAt(drawerSelectedCustomFloat);
			m_CustomFloats = tmp.ToArray();

			m_CurrentPreset.DeleteCustomFloat(drawerSelectedCustomFloat);
			m_PrevPreset.DeleteCustomFloat(drawerSelectedCustomFloat);
			m_LastFramePreset.DeleteCustomFloat(drawerSelectedCustomFloat);	

			// Remove from all presets
			for (int i = 0; i < m_Presets.Length; i++)
				m_Presets[i].DeleteCustomFloat(drawerSelectedCustomFloat);

			// Update selection index
			if (m_CustomFloats.Length < 1)
				drawerSelectedCustomFloat = -1;
			else
			{
				drawerSelectedCustomFloat = Mathf.Max(drawerSelectedCustomFloat-1, 0);
			}

			// Need to mark scene as dirty to not lose changes
			#if UNITY_EDITOR
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			#endif
		}
		#endregion

		#region 2D Cloud Planes
		/// <summary>
		/// Copy the currently selected 2D cloud plane.
		/// </summary>
		public void AddCloudPlane ()
		{
			CloudPlane cpy = null;
			if (drawerSelectedCloudPlane > -1 && drawerSelectedCloudPlane < m_CloudPlanes.Length)
				cpy = m_CloudPlanes[drawerSelectedCloudPlane];
			var tmp = new List<CloudPlane>(m_CloudPlanes);

			if (cpy != null)
				tmp.Add(new CloudPlane(cpy));
			else
				tmp.Add(new CloudPlane("2D Cloud Plane"));

			m_CloudPlanes = tmp.ToArray();
			drawerSelectedCloudPlane = m_CloudPlanes.Length-1;
		}

		/// <summary>
		/// Delete the currently selected 2D cloud plane.
		/// </summary>
		public void DeleteCloudPlane ()
		{
			var tmp = new List<CloudPlane>(m_CloudPlanes);
			tmp.RemoveAt(drawerSelectedCloudPlane);
			m_CloudPlanes = tmp.ToArray();
			drawerSelectedCloudPlane = m_CloudPlanes.Length-1;
		}
		#endregion

		#region Misc
		/// <summary>
		/// Force a sky changed flush next frame.
		/// </summary>
		public static void ForceSkyChanged ()
		{
			skyChanged = true;
		}

		/// <summary>
		/// Add a new weather preset.
		/// </summary>
		public void AddWeatherPreset ()
		{
			var tmp = new List<WeatherPreset>(m_Presets);
			var preset = new WeatherPreset("New Weather Preset");
			preset.customFloats = new float[m_CustomFloats.Length];
			for (int i = 0; i < m_CustomFloats.Length; i++)
			{
				preset.customFloats[i] = 0;
			}
			tmp.Add(preset);
			m_Presets = tmp.ToArray();
		}
		#endregion

		#region 3D Noise
		/// <summary>
		/// Load cached noise textures, or generate new ones if necessary (generating is limited to the editor).
		/// </summary>
		/// <param name="forceRegenerate">Whether to force skip loading cached noise or not.</param>
		public void InitializeNoise (bool forceRegenerate = false)
		{
			CloudNoiseGen.perlin = volumetricClouds.noiseGeneration.perlin;
			CloudNoiseGen.worley = volumetricClouds.noiseGeneration.worley;
			if (CloudNoiseGen.InitializeNoise(ref m_3DNoise, "OverCloud", (int)volumetricClouds.noiseGeneration.resolution, forceRegenerate ? CloudNoiseGen.Mode.ForceGenerate : CloudNoiseGen.Mode.LoadAvailableElseGenerate))
				Shader.SetGlobalTexture("_OC_3DNoiseTex", m_3DNoise);
			else
				Debug.LogError("Fatal: Failed to load/initialize 3D noise texture.");
		}

		/// <summary>
		/// Updates the 3D noise preview slice displayed in the inspector. Only works in the editor.
		/// </summary>
		public void UpdateNoisePreview ()
		{
			#if UNITY_EDITOR
			if (m_3DNoiseSlice == null || m_3DNoiseSlice.Length != 3)
				m_3DNoiseSlice = new RenderTexture[3];
			CloudNoiseGen.perlin = volumetricClouds.noiseGeneration.perlin;
			CloudNoiseGen.worley = volumetricClouds.noiseGeneration.worley;
			for (int i = 0; i < 3; i++)
			{
				if ((int)volumetricClouds.noiseGeneration.resolution <= 0)
					continue;

				if (m_3DNoiseSlice[i] == null || m_3DNoiseSlice[i].width != (int)volumetricClouds.noiseGeneration.resolution)
					m_3DNoiseSlice[i] = new RenderTexture((int)volumetricClouds.noiseGeneration.resolution, (int)volumetricClouds.noiseGeneration.resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				CloudNoiseGen.NoiseMode mode = CloudNoiseGen.NoiseMode.Mix;
				switch (i)
				{
					case 0:
						mode = CloudNoiseGen.NoiseMode.Mix;
					break;
					case 1:
						mode = CloudNoiseGen.NoiseMode.PerlinOnly;
					break;
					case 2:
						mode = CloudNoiseGen.NoiseMode.WorleyOnly;
					break;
				}
				CloudNoiseGen.GetSlice(ref m_3DNoiseSlice[i], 0, mode);
			}
			#endif
		}
		#endregion
	}
}