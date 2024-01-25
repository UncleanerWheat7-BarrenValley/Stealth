using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OC
{
	[CustomEditor(typeof(OverCloudReflectionProbe)), CanEditMultipleObjects]
	public class OverCloudReflectionProbeEditor : Editor
	{
		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			OverCloudReflectionProbe probe = (OverCloudReflectionProbe)target;

			DrawDefaultInspector();

			if (GUILayout.Button("Save Cubemap"))
			{
				probe.SaveCubemap();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}