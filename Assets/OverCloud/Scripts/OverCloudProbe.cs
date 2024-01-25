///////////////////////////////////////////////////////////
// Copyright (c) 2019 Felix Westin. All rights reserved. //
// For additional details, see LICENSE in root directory //
///////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OC
{
	/// <summary>
	/// The OverCloudProbe component can be used to sample the cloud density and/or coverage at a specific position.
	/// </summary>
	[ExecuteInEditMode]
	public class OverCloudProbe : MonoBehaviour
	{
		public float	density	{ get; private set; }
		public float	rain	{ get; private set; }

		[Tooltip("How fast the probe should fade to the new sampled value.")]
		public float	interpolationSpeed = 1;

		[Tooltip("Draw a gizmo which visualizes the density at the probe position.")]
		[SerializeField]
		bool			m_Debug = false;

		private void Update()
		{
			var cloudDensity = OverCloud.GetDensity(transform.position);
			density = Mathf.Lerp(density, cloudDensity.density, interpolationSpeed * Time.deltaTime);
			rain	= Mathf.Lerp(rain, cloudDensity.rain, interpolationSpeed * Time.deltaTime);
		}

		private void OnDrawGizmos ()
		{
			if (m_Debug)
			{
				float density = OverCloud.GetDensity(transform.position).density;
				Gizmos.color = Color.Lerp(Color.red, Color.green, density);
				Gizmos.DrawSphere(transform.position, Mathf.Lerp(10, 100, density));
			}
		}
	}
}
