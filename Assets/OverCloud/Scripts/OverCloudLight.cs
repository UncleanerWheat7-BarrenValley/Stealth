///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OC
{
	/// <summary>
	/// The OverCloudLight component is a utility class used to drive the color and intensity of the sun and moon directional lights.
	/// Alternatively, it can be used to light the volumetric clouds with a point light (only one can be active at any given time).
	/// </summary>
	[ExecuteInEditMode, RequireComponent(typeof(Light))]
	public class OverCloudLight : MonoBehaviour
	{
		public static List<OverCloudLight>	lights { get; private set; }
		public bool  hasActiveLight { get { return light.enabled && light.intensity > Mathf.Epsilon && light.color != Color.black; } }
		public float pointRadius	{ get { return light.range; } }
		public Color pointColor		{ get { return light.color * light.intensity; } }
		Light _light;
		new public Light light
		{
			get
			{
				if (!_light)
					_light = GetComponent<Light>();
				return _light;
			}
		}

		[System.Serializable]
		public enum Type
		{
			Point,
			Sun,
			Moon
		}

		public Type		type { get { return m_Type; } }

		[Tooltip("The light type (sun or moon).")]
		[SerializeField]
		Type			m_Type			= Type.Point;
		[Tooltip("A gradient which describes the color of the light over time. Sort of. In actuality, it uses the elevation of the light source as the input for the gradient evaluation. This means that the color value at the location 0% will be used when the light is facing straight upwards, 50% will be used when the light is exactly on the horizon and 100% will be used when the light is pointing straight downwards.")]
		[SerializeField]
		Gradient		m_ColorOverTime	= null;

		[Tooltip("A multiplier to apply on top of the evaluated color.")]
		public float	multiplier = 1;

		CommandBuffer	m_CascadeBuffer;
		CommandBuffer	m_ShadowsBuffer;
		Material		m_ShadowMaterial;
		bool			m_BufferInitialized;
		bool			m_ShadowBufferInitialized;

		private void OnEnable ()
		{
			if (lights == null)
				lights = new List<OverCloudLight>();
			lights.Add(this);
		}

		private void OnDisable ()
		{
			lights.Remove(this);

			ClearBuffers();
		}

		void InitializeBuffers ()
		{
			if (m_BufferInitialized)
				return;

			// Shadow map copy
			if (m_CascadeBuffer == null)
			{
				m_CascadeBuffer = new CommandBuffer();
				m_CascadeBuffer.name = "CascadeShadowCopy";
				m_CascadeBuffer.SetGlobalTexture("_CascadeShadowMapTexture", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));
				m_CascadeBuffer.SetGlobalFloat("_CascadeShadowMapPresent", 1f);
			}

			light.AddCommandBuffer(LightEvent.AfterShadowMap, m_CascadeBuffer);

			m_BufferInitialized = true;
		}

		void InitializeShadowBuffer ()
		{
			if (m_ShadowBufferInitialized)
				return;

			// Cloud shadows injection
			if (m_ShadowsBuffer == null)
			{
				m_ShadowsBuffer = new CommandBuffer();
				m_ShadowsBuffer.name = "CloudShadows";

				if (!m_ShadowMaterial)
					m_ShadowMaterial = new Material(Shader.Find("Hidden/OverCloud/Atmosphere"));

				//m_ShadowsBuffer.SetRenderTarget(BuiltinRenderTextureType.CurrentActive);
				m_ShadowsBuffer.SetGlobalVector("_LightShadowData", new Vector4(light.shadowStrength, 0, 0, light.shadowNearPlane));
				m_ShadowsBuffer.DrawMesh(OverCloud.quad, Matrix4x4.identity, m_ShadowMaterial, 0, 5);
			}

			light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, m_ShadowsBuffer);

			m_ShadowBufferInitialized = true;
		}

		void ClearBuffers ()
		{
			if (!m_BufferInitialized)
				return;

			if (m_CascadeBuffer != null)
			{
				light.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CascadeBuffer);
				m_CascadeBuffer = null;
			}

			m_BufferInitialized = false;
		}

		void ClearShadowBuffer ()
		{
			if (!m_ShadowBufferInitialized)
				return;

			if (m_ShadowsBuffer != null)
			{
				light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, m_ShadowsBuffer);
				m_ShadowsBuffer = null;
			}

			m_ShadowBufferInitialized = false;
		}

		// OverCloud needs to call this function every time a camera renders
		public void UpdateBuffers ()
		{
			if (!m_BufferInitialized)
			{
				InitializeBuffers();
			}

			if (!m_ShadowBufferInitialized)
			{
				InitializeShadowBuffer();
			}
		}

		void Update ()
		{
			if (OverCloud.instance)
			{
				if (m_Type != Type.Point)
				{
					// Evaluate light color
					var elevation = Vector3.Dot(light.transform.forward, Vector3.down)*0.5f+0.5f;
					switch (m_Type)
					{
						default:
						case Type.Sun:
							light.color = Color.Lerp(m_ColorOverTime.Evaluate(elevation), OverCloud.atmosphere.solarEclipseColor, OverCloud.solarEclipse) * multiplier;
						break;
						case Type.Moon:
							light.color = Color.Lerp(m_ColorOverTime.Evaluate(elevation), OverCloud.atmosphere.lunarEclipseColor, OverCloud.lunarEclipse) * multiplier * OverCloud.moonFade;
						break;
					}

					// Only allow one active directional light at a time
					light.enabled = (light == OverCloud.dominantLight) && !(light.color.r == 0 && light.color.g == 0 && light.color.b == 0);

					// Check if we need to initialize or clear buffers for this lightp
					if (light.enabled && !m_BufferInitialized)
						InitializeBuffers();
					else if (!light.enabled && m_BufferInitialized)
						ClearBuffers();

					if (light.enabled && OverCloud.lighting.cloudShadows.mode == CloudShadowsMode.Injected && !m_ShadowBufferInitialized)
						InitializeShadowBuffer();
					else if ((!light.enabled || OverCloud.lighting.cloudShadows.mode != CloudShadowsMode.Injected) && m_ShadowBufferInitialized)
						ClearShadowBuffer();
				}
			}
			else if (m_BufferInitialized)
			{
				// No OverCloud instance found. Clear buffers if they exist
				ClearBuffers();
				ClearShadowBuffer();
			}
		}
	}
}