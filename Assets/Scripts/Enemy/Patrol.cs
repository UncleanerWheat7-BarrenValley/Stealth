using UnityEngine;
using UnityEngine.AI;

public class Patrol : MonoBehaviour
{
    public Transform[] patrolTransforms;
    public int currentWaypoint = 0;
    public int totalWaypointNumber = 0;
    public float waypointWaitTime;

    void Start()
    {
        totalWaypointNumber = patrolTransforms.Length - 1;
    }

    public void UpdateWayPoint()
    {
        if (currentWaypoint < totalWaypointNumber)
        {
            currentWaypoint++;
        }
        else
        {
            currentWaypoint = 0;
        }        
    }
}
