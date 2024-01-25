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
	/// The OverCloudReflectionProbeUpdater can be used to automatically update reflection probes whenever the sky changes.
	/// </summary>
	#if UNITY_EDITOR
	[InitializeOnLoad]
	#endif
	[ExecuteInEditMode, RequireComponent(typeof(ReflectionProbe))]
	public class OverCloudReflectionProbeUpdater : MonoBehaviour
	{
		[System.Serializable]
		public enum UpdateMode
		{
			OnSkyChanged,
			OnEnable,
			Realtime
		}

		ReflectionProbe _reflectionProbe;
		public ReflectionProbe reflectionProbe
		{
			get
			{
				if (!_reflectionProbe)
					_reflectionProbe = GetComponent<ReflectionProbe>();
				return _reflectionProbe;
			}
		}

		[Tooltip("If and when the reflection probe should be updated. If set to ScriptOnly, the reflection probe will not render unless RenderProbe is manually called.")]
		public UpdateMode						updateMode = UpdateMode.OnSkyChanged;

		[Tooltip("OverCloudReflectionProbeUpdater will set the reflection probe timeSlicingMode to this value.")]
		public ReflectionProbeTimeSlicingMode	timeSlicing = ReflectionProbeTimeSlicingMode.IndividualFaces;

		void OnEnable ()
		{
			reflectionProbe.mode = ReflectionProbeMode.Realtime;
			reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
			reflectionProbe.timeSlicingMode = timeSlicing;
			
			reflectionProbe.RenderProbe();
		}

		private void OnValidate()
		{
			reflectionProbe.timeSlicingMode = timeSlicing;
		}

		void Update () 
		{
			if (reflectionProbe)
			{
				if (updateMode == UpdateMode.Realtime || (updateMode == UpdateMode.OnSkyChanged && OverCloud.skyChanged))
					reflectionProbe.RenderProbe();
			}
		}
	}
}