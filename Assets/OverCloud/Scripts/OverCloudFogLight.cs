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
	[ExecuteInEditMode]
	public class OverCloudFogLight : MonoBehaviour
	{
		public static List<OverCloudFogLight> fogLights;

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

		[SerializeField] [Tooltip("The sphere mesh used to render the fog light. Don't change this!")]
		public Mesh				m_Mesh;
		[SerializeField] [Tooltip("The material used to render the fog light. Don't change this!")]
		public Material			m_Material;
		[Range(0, 1)] [Tooltip("Intensity multiplier for the effect.")]
		public float			intensity = 1;
		[Range(0, 1)] [Tooltip("Minimum fog density used for the effect. Can be used to force the effect to show up even when there is no fog.")]
		public float			minimumDensity = 0;
		[Range(0, 1)] [Tooltip("Controls the falloff of the effect from the camera.")]
		public float			attenuationFactor = 1;

		[Range(4, 128)]
		public int				raymarchSteps = 16;

		MaterialPropertyBlock	m_Prop;

		private void OnEnable ()
		{
			if (fogLights == null)
				fogLights = new List<OverCloudFogLight>();
			fogLights.Add(this);
		}

		private void OnDisable ()
		{
			fogLights.Remove(this);
		}

		void UpdateProperties ()
		{
			m_Prop.SetColor("_Color", light.color * light.intensity);
			m_Prop.SetVector("_Center", transform.position);
			m_Prop.SetVector("_Params", new Vector4(light.range, 1f / light.range, intensity, attenuationFactor));
			m_Prop.SetVector("_Params2", new Vector4(Mathf.Pow(minimumDensity, 16), 0, 0, 0));
			if (light.type == LightType.Spot)
			{
				var fwd = light.transform.forward;
				m_Prop.SetVector("_SpotParams", light.transform.forward);
				float dot = 1 - Mathf.Cos(light.spotAngle * Mathf.Deg2Rad * 0.5f);
				m_Prop.SetVector("_SpotParams2", new Vector3(1 - dot, 1 / dot, 0));
			}
			m_Prop.SetVector("_RaymarchSteps", new Vector2(raymarchSteps, 1f / (float)raymarchSteps));
		}

		public void BufferRender (CommandBuffer buffer)
		{
			if (!light)
				return;

			if (m_Prop == null)
				m_Prop = new MaterialPropertyBlock();

			int pass = -1;
			if (light.type == LightType.Point)
				pass = 0;
			else if (light.type == LightType.Spot)
				pass = 1;

			if (pass < 0)
				return;

			if (m_Mesh && m_Material)
			{
				UpdateProperties();
				
				buffer.DrawMesh(m_Mesh, Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * light.range), m_Material, 0, pass, m_Prop);
			}
		}
	}
}
