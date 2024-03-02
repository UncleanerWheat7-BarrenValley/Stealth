using UnityEngine;
using static StateMachine;
using UnityEngine.AI;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.UIElements;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    private StateMachine stateMachine = new StateMachine();
    private Patrol patrol;
    private NavMeshAgent navMeshAgent;
    private Vector3 randomPoint;
    private FOV fov;
    private float topSpeed;

    public Light stateLight;
    public Transform playerTransform;
    public float currentMoveSpeed;

    public MyState myState;
    public enum MyState
    {
        idle, caution, alert
    }

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        patrol = GetComponent<Patrol>();
        fov = GetComponent<FOV>();
        topSpeed = navMeshAgent.speed;
        UpdateCurrentState();
    }

    private void Update()
    {
        if (fov.alertLevel < 50)
        {
            if (stateMachine.currentState is not IdleState)
            {
                print(stateMachine.currentState);
                stateMachine.ChangeState(new IdleState(this.gameObject));
            }
        }
        else if (fov.alertLevel > 50 && fov.alertLevel < 75)
        {
            if (stateMachine.currentState is not CautionState)
            {
                print(stateMachine.currentState);
                stateMachine.ChangeState(new CautionState(this.gameObject));
            }
        }
        else if (fov.alertLevel >= 75)
        {
            if (stateMachine.currentState is not AlertState)
            {
                stateMachine.ChangeState(new AlertState(this.gameObject));
            }
        }
        currentMoveSpeed = navMeshAgent.velocity.magnitude / topSpeed;//for animation speed
        stateMachine.Update();
    }

    public void UpdateCurrentState()
    {
        if (myState == MyState.idle)
        {
            stateMachine.ChangeState(new IdleState(this.gameObject));
        }
        else if (myState == MyState.caution)
        {
            stateMachine.ChangeState(new CautionState(this.gameObject));
        }
        else if (myState == MyState.alert)
        {
            stateMachine.ChangeState(new AlertState(this.gameObject));
        }
    }

    public void SelectRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 10;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, 10, 1);
        randomPoint = hit.position;
    }

    public void StartPatrol()
    {
        StartCoroutine("Patrol");
    }

    IEnumerator Patrol()
    {
        Transform waypoint;
        while (stateMachine.currentState is IdleState)
        {
            waypoint = patrol.patrolTransforms[patrol.currentWaypoint];
            if (closeEnough(waypoint))
            {
                float waitTime = patrol.waypointWaitTime;

                float t = 0;
                while (t < 1)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,waypoint.rotation,t * 0.1f);
                    t += Time.deltaTime;
                    yield return null;
                }

                yield return new WaitForSeconds(waitTime);
                patrol.UpdateWayPoint();
            }
            GetComponent<NavMeshAgent>().destination = waypoint.position;
            yield return null;
        }
        yield break;
    }

    public void MoveToRandom()
    {
        GetComponent<NavMeshAgent>().destination = randomPoint;
    }

    public bool closeEnough(Transform destination)
    {
        return Vector3.Distance(navMeshAgent.transform.position, destination.position) < 0.1? true : false;
    }

    public void MoveToPlayer()
    {
        navMeshAgent.destination = playerTransform.position;
    }

    internal void ChangeLightColour(Color colourValue)
    {
        stateLight.color = colourValue;
    }

    internal void UpdateMoveSpeed(float multiplier)
    {
        navMeshAgent.speed = topSpeed * multiplier;
    }
}
