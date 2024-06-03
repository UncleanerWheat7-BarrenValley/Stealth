using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Destroy;
using static FootPrint;

public class FootprintManager : MonoBehaviour
{
    public bool useFootprint;
    public List<Vector3> allFootprintList = new List<Vector3>();
    public List<GameObject> enemyList = new List<GameObject>();

    public static bool useFootprints;

    private void OnEnable()
    {
        useFootprints = useFootprint;
        if (useFootprints)
        {
            footPlacement += AddFootprintToList;
            footRemoval += DestroyOneFootprint;
        }
    }

    private void OnDisable()
    {
        if (useFootprints)
        {
            footPlacement -= AddFootprintToList;
            footRemoval -= DestroyOneFootprint;
        }
    }

    private void Start()
    {
        if (useFootprints)
        {
            StartCoroutine("FootprintCheck", 2);
        }

    }

    IEnumerator FootprintCheck(int waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            bool distanceInRange;
            bool angleRange;

            foreach (GameObject enemy in enemyList)
            {
                if (enemy.GetComponent<Enemy>().myState != Enemy.MyState.idle) continue;

                foreach (Vector3 footPlacement in allFootprintList)
                {
                    Vector3 directionToFoot = enemy.transform.position - footPlacement;
                    angleRange = Vector3.Angle(enemy.transform.forward, directionToFoot) > 120;
                    distanceInRange = Vector3.Distance(footPlacement, enemy.transform.position) < 2;

                    if (enemy.GetComponent<BreadCrumb>().follow)
                    {
                        enemy.GetComponent<BreadCrumb>().AddFootprint(footPlacement);
                    }
                    else if (angleRange && distanceInRange)
                    {
                        enemy.GetComponent<BreadCrumb>().AddFootprint(footPlacement);
                        enemy.GetComponent<BreadCrumb>().StartToTrail(footPlacement);
                    }
                }
            }
        }
    }

    public void AddFootprintToList(Vector3 footprint)
    {
        allFootprintList.Add(footprint);
    }

    public void DestroyOneFootprint(Vector3 destroyedFootprint)
    {
        allFootprintList.Remove(destroyedFootprint);
        foreach (GameObject enemy in enemyList)
        {
            enemy.GetComponent<BreadCrumb>().ClearFootprintIfExists(destroyedFootprint);
        }
    }
}
