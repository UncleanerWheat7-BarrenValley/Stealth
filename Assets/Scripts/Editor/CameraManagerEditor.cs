using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraFOV))]
public class CameraManagerEditor : Editor
{
    private void OnSceneGUI()
    {
        drawFOV();
    }
    private void drawFOV()
    {
        CameraFOV fov = (CameraFOV)target;

        Vector3 pos = fov.transform.position;

        Handles.color = new Color(1, 0, 0, 0.1f);
        Handles.DrawSolidArc(pos, Vector3.up, fov.DirFromAngle(-fov.fovValue / 2), fov.fovValue, fov.depthOfViewValue);       

        
        
    }
}
