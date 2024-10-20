using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BreadCrumb : MonoBehaviour
{
    [SerializeField]
    NavMeshAgent agent;
    [SerializeField]
    Enemy enemyScript;

    public bool follow = false;
    Vector3 destinationPos;    

    [SerializeField]
    public List<Vector3> followFootprintList = new List<Vector3>();
        
    public void AddFootprint(Vector3 footprint)
    {
        followFootprintList.Add(footprint);
    }

    public void StartToTrail(Vector3 footprint)
    {
        destinationPos = followFootprintList.Count > 0 ? followFootprintList[0] : Vector3.zero;
        follow = true;
        StartCoroutine("FollowBreadcrumbs", 1);
        enemyScript.SetState(Enemy.MyState.follow);
        enemyScript.navMeshAgent.destination = destinationPos;
    }

    IEnumerator FollowBreadcrumbs(float delay)
    {
        while (follow && followFootprintList.Count > 0)
        {
            destinationPos = followFootprintList.Count > 0 ? followFootprintList[0] : Vector3.zero;
            if (enemyScript.alertLevel > 20)
            {
                follow = false;
            }

            print(Vector3.Distance(transform.position, destinationPos));

            if (Vector3.Distance(transform.position, destinationPos) < 0.5f)
            {
                followFootprintList.Remove(followFootprintList[0]);

                enemyScript.navMeshAgent.destination = destinationPos;
                yield return new WaitForSeconds(delay);
            }

            if (followFootprintList.Count == 0)
            {
                follow = false;
                enemyScript.SetState(Enemy.MyState.idle);
                yield break;
            }


            yield return new WaitForSeconds(delay);
        }

        follow = false;
        enemyScript.SetState(Enemy.MyState.idle);

        followFootprintList.Clear();
        yield break;
    }

    public void ClearFootprintIfExists(Vector3 footprint)
    {
        if (followFootprintList.Contains(footprint))
        {
            follow = false;
            enemyScript.SetState(Enemy.MyState.idle);
        }
    }
}
