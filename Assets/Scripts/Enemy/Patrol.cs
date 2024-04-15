using UnityEngine;

public class Patrol : MonoBehaviour
{
    public Transform[] patrolTransforms;
    public int currentWaypoint = 0;
    public int totalWaypointNumber = 0;
    public float waypointWaitTime = 1;

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
