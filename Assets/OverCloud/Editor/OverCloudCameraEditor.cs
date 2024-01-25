using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OC
{
	[CustomEditor(typeof(OverCloudCamera)), CanEditMultipleObjects]
	public class OverCloudCameraEditor : Editor
	{
		public override void OnInspectorGUI ()
		{
			var ocCamera = (OverCloudCamera)target;

			if (ocCamera.camera && OverCloud.instance && ocCamera.camera.farClipPlane < OverCloud.volumetricClouds.cloudPlaneRadius)
			{
				EditorGUILayout.HelpBox("The camera far clip plane is lower than the volumetric cloud plane radius. You might not be able to see the volumetric clouds in the game view. \n" +
					"To fix this, increase the camera far clip plane.", MessageType.Warning);
			}

			DrawDefaultInspector();
		}
	}
}