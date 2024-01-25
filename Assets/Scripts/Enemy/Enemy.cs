using UnityEngine;
using static StateMachine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{

    StateMachine stateMachine = new StateMachine();
    public Light light;
    private NavMeshAgent navMeshAgent;
    public Transform playerTransform;
    public float currentMoveSpeed;
    private Vector3 point;
    private float topSpeed;

    public MyState myState;
    public enum MyState
    {
        idle, caution, alert
    }


    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        topSpeed = navMeshAgent.speed;
        UpdateCurrentState();
    }

    private void Update()
    {

        currentMoveSpeed = navMeshAgent.velocity.magnitude / topSpeed;
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
        point = hit.position;

    }

    public void MoveToRandom()
    {
        GetComponent<NavMeshAgent>().destination = point;
        navMeshAgent.speed = topSpeed / 2;
    }

    public bool closeEnough()
    {
        if (Vector3.Distance(navMeshAgent.transform.position, point) < 1)
        {
            return true;
        }
        else 
        {
            return false; 
        }
    }

    public void MoveToPlayer()
    {
        navMeshAgent.destination = playerTransform.position;
    }
}
