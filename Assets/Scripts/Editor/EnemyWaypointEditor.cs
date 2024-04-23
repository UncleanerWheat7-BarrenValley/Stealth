using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using log4net.Util;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;
using static UnityEditor.PlayerSettings;

[CustomEditor(typeof(Patrol))]
public class EnemyWaypointEditor : Editor
{
    private void OnSceneGUI()
    {
        Patrol patrol = (Patrol)target;
        UnityEngine.Transform[] transforms = target.GetComponent<Patrol>().patrolTransforms;

        if (transforms.Length < 1)
        {
            return;
        }

        Vector3 dir;
        Vector3 pos;

        Handles.color = Color.red;
        for (int i = 0; i < target.GetComponent<Patrol>().patrolTransforms.Length - 1; i++)
        {
            dir = (transforms[i].position - transforms[i + 1].position).normalized;
            pos = (transforms[i].position + transforms[i + 1].position) / 2;

            Handles.DrawDottedLine(transforms[i].position, transforms[i + 1].position, 10);
            Handles.DrawSolidArc(pos, Vector3.up, dir, 20, 1);
        }
        
        Handles.DrawDottedLine(transforms[transforms.Length - 1].position, transforms[0].position, 10);
        Handles.DrawSolidArc((transforms[transforms.Length - 1].position + transforms[0].position) / 2, 
            Vector3.up, 
            (transforms[transforms.Length - 1].position - transforms[0].position).normalized, 
            20, 
            1);

    }
}
