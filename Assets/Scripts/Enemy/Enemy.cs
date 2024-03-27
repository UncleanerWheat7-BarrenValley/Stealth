using UnityEngine;
using static StateMachine;
using UnityEngine.AI;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    private StateMachine stateMachine = new StateMachine();
    [SerializeField]
    private Patrol patrol;
    [SerializeField]
    private EnemyAnimationHandler enemyAnimationHandler;
    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private FOV fov;

    private Vector3 randomPoint;
    private float topSpeed;

    public Light stateLight;
    public Transform playerTransform;
    public float currentMoveSpeed;

    public MyState myState;
    public enum MyState
    {
        idle, caution, alert, dead
    }

    void Start()
    {
        topSpeed = navMeshAgent.speed;
        SetState(myState);
    }

    private void Update()
    {
        if (stateMachine == null) 
        {
            return;
        }

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

    public void SetState(MyState newState) 
    {
        myState = newState;
        UpdateCurrentState();
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
        else if (myState == MyState.dead) 
        {
            stateMachine.ChangeState(new DeadState(this.gameObject));
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
                    transform.rotation = Quaternion.Slerp(transform.rotation, waypoint.rotation, t * 0.1f);
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
        return Vector3.Distance(navMeshAgent.transform.position, destination.position) < 0.1 ? true : false;
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

    internal void Dead()
    {
        DisableSelf();
        enemyAnimationHandler.PlayDeath();        
        stateMachine = null;
    }

    private void DisableSelf()
    {
        fov.enabled = false;
        navMeshAgent.enabled = false;
        patrol.enabled = false;        
        this.enabled = false;
    }
}
