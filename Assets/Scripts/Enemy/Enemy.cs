using UnityEngine;
using static StateMachine;
using UnityEngine.AI;
using System.Collections;
using System.Threading.Tasks;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    private StateMachine stateMachine = new StateMachine();
    [SerializeField]
    private Patrol patrol;
    [SerializeField]
    private EnemyAnimationHandler enemyAnimationHandler;
    [SerializeField]
    public NavMeshAgent navMeshAgent;
    [SerializeField]
    public FOV fov;
    [SerializeField]
    private EnemyManager enemyManager;
    [SerializeField]
    private Gun gun;

    [Range(0, 100)] public float alertLevel;



    private Vector3 randomPoint;
    private float topSpeed;
    private Vector3 playerShadowTransformPosition;

    public Transform playerTransform;
    public Light stateLight;
    public float currentMoveSpeed;

    public MyState myState;
    public enum MyState
    {
        idle,
        caution,
        calming,
        alert,
        dead,
        fire
    }

    private void OnEnable()
    {
        PlayerManager.playerDied += playerDied;
        Gun.shootSound += ShootSound;
        Laser.laserPlayerDetected += Alarm;
    }

    private void OnDisable()
    {
        PlayerManager.playerDied -= playerDied;
        Gun.shootSound -= ShootSound;
    }

    void Start()
    {
        topSpeed = navMeshAgent.speed;
        SetState(myState);
        playerShadowTransformPosition = playerTransform.position;
    }

    private void Update()
    {
        print(stateMachine.currentState);
        currentMoveSpeed = navMeshAgent.velocity.magnitude / topSpeed;//for animation speed
        stateMachine.Update();
    }

    public void SetState(MyState newState)
    {
        myState = newState;
        UpdateCurrentState();
    }

    private void UpdateCurrentState()
    {
        if (myState == MyState.idle)
        {
            stateMachine.ChangeState(new IdleState(this.gameObject));
        }
        else if (myState == MyState.caution)
        {
            stateMachine.ChangeState(new CautionState(this.gameObject));
        }
        else if (myState == MyState.calming)
        {
            stateMachine.ChangeState(new CalmingState(this.gameObject));
        }
        else if (myState == MyState.alert)
        {
            stateMachine.ChangeState(new AlertState(this.gameObject));
        }
        else if (myState == MyState.dead)
        {
            stateMachine.ChangeState(new DeadState(this.gameObject));
        }
        else if (myState == MyState.fire)
        {
            stateMachine.ChangeState(new FireState(this.gameObject));
        }
    }

    public void SelectRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 10;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, 10, 1);
        randomPoint = hit.position;
        navMeshAgent.SetDestination(randomPoint);
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
            if (Vector3.Distance(navMeshAgent.transform.position, waypoint.position) < 0.1 ? true : false)
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

    public void Investigate()
    {        
        GetComponent<NavMeshAgent>().destination = playerShadowTransformPosition;        
    }   

    public void UpdatePlayerShadow()
    {
        playerShadowTransformPosition = playerTransform.position;
    }

    public void MoveToPlayer()
    {
        navMeshAgent.destination = playerShadowTransformPosition;

        if (Vector3.Distance(playerShadowTransformPosition, transform.position) < 3 && fov.PlayerInFOV())
        {
            SetState(MyState.fire);
        }
    }

    public void FireGun(bool fire)
    {
        enemyAnimationHandler.PlayFire(fire);
    }

    public void AimAtPlayer()
    {
        if (Vector3.Distance(playerShadowTransformPosition, transform.position) < 5)
        {
            Quaternion rotation = Quaternion.LookRotation(playerShadowTransformPosition - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f);
        }
        else
        {
            FireGun(false);
            SetState(MyState.alert);
        }
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

    void playerDied()
    {
        alertLevel = 0;
        SetState(MyState.idle);
    }

    void ShootSound(Vector3 gunshotLocation)
    {
        if (Vector3.Distance(transform.position, gunshotLocation) < fov.fosValue)
        {
            print("I heard that");
            alertLevel += 100 / Vector3.Distance(transform.position, gunshotLocation);
            SetState(MyState.caution);
            Investigate();
        }
    }

    void Alarm() 
    {
        alertLevel = 100;
        SetState(MyState.alert);
    }

    public async void AlertCooldown(float multipier)
    {
        while (alertLevel > 0)
        {
            if (fov.PlayerInFOV())
            {
                break;
            }

            alertLevel = Mathf.MoveTowards(alertLevel, 0, multipier * Time.deltaTime);

            await Task.Yield();
        }
    }
}
