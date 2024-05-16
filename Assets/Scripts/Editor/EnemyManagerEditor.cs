using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FOV))]
public class EnemyManagerEditor : Editor
{
    private void OnSceneGUI()
    {
        drawFOV();
    }

    private void drawFOV()
    {
        FOV fov = (FOV)target;

        Vector3 pos = fov.transform.position + Vector3.up * 1.5f;

        Handles.color = new Color(1, 1, 1, 0.1f);
        Handles.DrawSolidArc(pos, Vector3.up, fov.DirFromAngle(-fov.fovValue / 2), fov.fovValue, fov.depthOfViewValue);        

        Handles.color = new Color(1, 0, 1, 0.1f);
        Handles.DrawSolidDisc(pos, Vector3.up, fov.fosValue);

        Handles.color = Color.red;
        fov.fovValue = Handles.ScaleValueHandle(
            fov.fovValue,
            pos + fov.transform.forward * 1,
            fov.transform.rotation * Quaternion.Euler(0, 180, 1),
            3,
            Handles.ConeHandleCap,
            1
            );

        Handles.color = Color.blue;
        fov.depthOfViewValue = Handles.ScaleValueHandle(
            fov.depthOfViewValue,
            pos + fov.transform.forward * 2,
            fov.transform.rotation,
            10,
            Handles.ArrowHandleCap,
            1
            );
    }
}
