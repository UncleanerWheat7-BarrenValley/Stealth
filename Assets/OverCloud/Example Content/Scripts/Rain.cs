using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC.ExampleContent
{
	// Small script which feeds a particle system with data based on an OverCloudProbe.
	// It also rotates the particle system along the velocity of the object.
	[RequireComponent(typeof(ParticleSystem)), RequireComponent(typeof(AudioSource))]
	public class Rain : MonoBehaviour
	{
		[SerializeField]
		OverCloudProbe	m_CloudProbe = null;
		[SerializeField]
		float			m_VelocityRotationSpeed = 10;
		[SerializeField]
		float			m_RotationRelax = 10;

		ParticleSystem	m_ParticleSystem;
		ParticleSystem.EmissionModule	m_Emission;
		AudioSource		m_AudioSource;
		Vector3			m_LastPos;

		private void Start()
		{
			m_ParticleSystem	= GetComponent<ParticleSystem>();
			m_Emission			= m_ParticleSystem.emission;
			m_AudioSource		= GetComponent<AudioSource>();
		}

		void OnEnable ()
		{
			m_LastPos = transform.position;
		}
	
		void Update ()
		{
			// Get rain value from cloud probe
			var rain = m_CloudProbe.rain;

			// Update particle emission
			if (rain > Mathf.Epsilon)
				m_ParticleSystem.Play();
			m_Emission.rateOverTime = rain * 10000;

			// Update rain sound
			m_AudioSource.volume = rain;

			// Calculate object velocity
			var velocity = (transform.position - m_LastPos) / Time.deltaTime;
			// Bias towards "neutral" rotation
			velocity += Vector3.up * m_RotationRelax;
			// Update the rotation of the object
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(-velocity.normalized), m_VelocityRotationSpeed * Time.deltaTime);

			// Store position for velocity calculation next frame
			m_LastPos = transform.position;
		}
	}
}