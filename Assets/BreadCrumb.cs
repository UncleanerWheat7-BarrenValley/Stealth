using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Destroy;
using static FootPrint;

public class BreadCrumb : MonoBehaviour
{
    [SerializeField]
    NavMeshAgent agent;
    [SerializeField]
    Enemy enemyScript;

    public List<GameObject> allFootprintList = new List<GameObject>();
    public List<GameObject> followFootprintList = new List<GameObject>();

    bool followB = false;

    private void OnEnable()
    {
        footPlacement += AddFootprintToList;
        footRemoval += DestroyOneFootprint;
    }

    private void OnDisable()
    {
        footPlacement -= AddFootprintToList;
        footRemoval -= DestroyOneFootprint;
    }


    void Start()
    {
        StartCoroutine("CheckBreadcrumbs", 1);
    }

    IEnumerator CheckBreadcrumbs(float delay)
    {
        while (!followB)
        {
            yield return new WaitForSeconds(delay);
            CheckForBreadcrumbs();
        }
        yield break;
    }

    private void CheckForBreadcrumbs()
    {
        foreach (var footprint in allFootprintList)
        {
            Vector3 dirToTarget = (footprint.transform.position - transform.position).normalized;
            var a = Vector3.Distance(transform.position, footprint.transform.position);
            bool distanceInRange = Vector3.Distance(transform.position, footprint.transform.position) < 20;
            bool radiusInRange = Vector3.Angle(transform.forward, dirToTarget) < 45 / 2;

            if (distanceInRange)
            {
                followB = true;
                foreach (var eachFootprint in allFootprintList)
                {
                   
                }
                enemyScript.SetState(Enemy.MyState.follow);
            }
        }
    }

    private void Update()
    {
        print(followB);
    }

    public void AddFootprintToList(GameObject footprint)
    {
        allFootprintList.Add(footprint);
        if (followB)
        {
            followFootprintList.Add(footprint);
        }
    }

    public void UpdateBreadCrumb()
    {
        if (followFootprintList.Count > 1)
        {
            followFootprintList.Remove(followFootprintList[0]);
            agent.destination = followFootprintList[0].transform.position;
        }
        else
        {
            DestroyBreadCrumbs();
            enemyScript.SetState(Enemy.MyState.idle);
        }
    }

    public void DestroyOneFootprint(GameObject destroyedFootprint)
    {
        allFootprintList.Remove(destroyedFootprint);

        if (followFootprintList.Count > 0)
        {
            if (destroyedFootprint == followFootprintList[0])
            {
                followB = false;
            }
        }

        followFootprintList.Remove(destroyedFootprint);

    }

    private void DestroyBreadCrumbs()
    {
        allFootprintList.Clear();
        followFootprintList.Clear(); ;
    }
}
