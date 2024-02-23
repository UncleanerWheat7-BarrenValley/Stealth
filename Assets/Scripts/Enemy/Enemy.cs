using UnityEngine;
using static StateMachine;
using UnityEngine.AI;
using Unity.VisualScripting;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{

    private StateMachine stateMachine = new StateMachine();
    public Light light;
    private NavMeshAgent navMeshAgent;
    public Transform playerTransform;
    public float currentMoveSpeed;
    private Vector3 point;
    private float topSpeed;
    private FOV fov;

    public MyState myState;
    public enum MyState
    {
        idle, caution, alert
    }


    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
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
        point = hit.position;

    }

    public void MoveToRandom()
    {
        GetComponent<NavMeshAgent>().destination = point;

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

    internal void ChangeLightColour(Color colourValue)
    {
        light.color = colourValue;
    }

    internal void UpdateMoveSpeed(float multiplier)
    {
        navMeshAgent.speed = topSpeed * multiplier;
    }
}
