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
    private List<GameObject> enemyList = new List<GameObject>();

    public static bool useFootprints = false;
    bool footstepsCheckRunning;

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

    private void Start()
    {
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in allEnemies)
        {
            enemyList.Add(enemy);
        }
    }

    IEnumerator FootprintCheck(int waitTime)
    {
        footstepsCheckRunning = true;

        while (footstepsCheckRunning)
        {
            print("Searching for feet");
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
        if (useFootprint)
            allFootprintList.Add(footprint);

        if (footstepsCheckRunning) return;

        StartCoroutine("FootprintCheck", 2);        
    }

    public void DestroyOneFootprint(Vector3 destroyedFootprint)
    {
        allFootprintList.Remove(destroyedFootprint);
        foreach (GameObject enemy in enemyList)
        {
            enemy.GetComponent<BreadCrumb>().ClearFootprintIfExists(destroyedFootprint);
        }

        if (allFootprintList.Count < 1) 
        {
            footstepsCheckRunning = false;
        }
    }
}
