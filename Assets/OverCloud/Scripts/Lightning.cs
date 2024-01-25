using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OC.ExampleContent
{
	// An example of how to implement lightning effects
	[ExecuteInEditMode]
	public class Lightning : MonoBehaviour
	{
		[SerializeField]
		[Range(0, 1)]
		float			m_Phase;
		[SerializeField]
		float			m_PlaySpeed		= 1;
		[SerializeField]
		bool			m_PlayInEditor	= true;
		[SerializeField]
		LayerMask		m_ImpactLayers	= 0;

		[Header("Components")]
		[SerializeField]
		Transform		m_RendererPivot	= null;
		[SerializeField]
		MeshRenderer	m_Renderer		= null;
		[SerializeField]
		Material		m_Material		= null;
		[SerializeField]
		Light			m_Light			= null;
		[SerializeField]
		AudioSource		m_SoundEffect	= null;
		[SerializeField]
		ParticleSystem	m_ImpactSparks	= null;

		[Header("Point Light Settings")]
		[SerializeField]
		Gradient		m_PhaseColor	= null;

		private void OnValidate()
		{
			if (!m_Light || !m_Material)
				return;

			UpdateComponents();
		}

		private void OnEnable()
		{
			#if UNITY_EDITOR
				EditorApplication.update += EditorUpdate;
			#endif

			RestartLightning();
		}

		private void OnDisable()
		{
			#if UNITY_EDITOR
				EditorApplication.update -= EditorUpdate;
			#endif
		}

		void EditorUpdate ()
		{
			if (Application.isPlaying || !m_PlayInEditor)
				return;

			UpdateLightning();
		}

		private void Update()
		{
			if (!Application.isPlaying)
				return;

			UpdateLightning();
		}

		void RestartLightning ()
		{
			// Only move forwards if the game is playing or we have enabled playing in editor
			if (!Application.isPlaying && !m_PlayInEditor)
				return;

			// The bolt has to hit something
			RaycastHit hit;
			if (!Physics.Raycast(new Ray(transform.position, Vector3.down), out hit, 99999, m_ImpactLayers))
				return;

			// Set the scale of the bolt based on the height
			float height = transform.position.y - hit.point.y;
			m_RendererPivot.localScale = Vector3.one * height;

			m_Material.SetInt("_TexIndex", Random.Range(0, 4));

			m_Phase = 0;
			if (Application.isPlaying)
			{
				if (m_SoundEffect)
				{
					// Play sound effect
					m_SoundEffect.Play();
				}
				if (m_ImpactSparks)
				{
					// Play impact sparks effect
					m_ImpactSparks.transform.position = hit.point;
					m_ImpactSparks.Play();
				}
			}
		}

		void UpdateLightning ()
		{
			m_Phase = Mathf.Min(m_Phase + Time.deltaTime * m_PlaySpeed, 1);

			UpdateComponents();
		}

		void UpdateComponents ()
		{
			bool shouldEnable  = m_Phase < 1;
			m_Renderer.enabled = shouldEnable;
			m_Light.enabled    = shouldEnable;

			if (shouldEnable)
			{
				m_Light.color = m_PhaseColor.Evaluate(m_Phase);
				m_Material.SetFloat("_Phase", m_Phase);
			}

			// Need to wait for particle system and audio source to finish
			if (m_SoundEffect)
				shouldEnable = shouldEnable || m_SoundEffect.isPlaying;
			if (m_ImpactSparks)
				shouldEnable = shouldEnable || m_ImpactSparks.isPlaying;

			if (!shouldEnable)
			{
				// Disable the GameObject so OverCloud knows the lightning effect is ready to use again
				gameObject.SetActive(false);
			}
		}
	}
}