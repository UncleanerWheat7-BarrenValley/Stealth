using System;
using UnityEngine;

public class StateMachine
{
    public IStates currentState;

    public void ChangeState(IStates newState)
    {
        Debug.Log("Changing State");
        if (currentState != null)
        {
            currentState.ExitState();
        }

        currentState = newState;
        currentState.EnterState();
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.ExecuteState();
        }
    }

    public class IdleState : IStates
    {
        GameObject owner;
        Enemy enemyScript;
        Patrol patrol;
        public IdleState(GameObject owner)
        {
            this.owner = owner;
            enemyScript = owner.GetComponent<Enemy>();
            patrol = owner.GetComponent<Patrol>();
        }

        public void EnterState()
        {
            enemyScript.UpdateMoveSpeed(0.3f);
            enemyScript.ChangeLightColour(new Color(0, 0, 1, 0));


        }

        public void ExecuteState()
        {
            if (patrol.patrolTransforms.Length > 0)
            {
                enemyScript.StartPatrol();
            }

            if (enemyScript.alertLevel > 10)
            {
                enemyScript.SetState(Enemy.MyState.caution);
            }
        }

        public void ExitState()
        {
            Debug.Log("Exit Idle state");
        }
    }

    public class CautionState : IStates
    {
        GameObject owner;
        Enemy enemyScript;
        public CautionState(GameObject owner)
        {
            this.owner = owner;
            enemyScript = owner.GetComponent<Enemy>();
        }
        public void EnterState()
        {
            enemyScript.ChangeLightColour(new Color(1, 0, 1, 0));
            enemyScript.UpdatePlayerShadow();
            enemyScript.Investigate();
            enemyScript.UpdateMoveSpeed(0.5f);
        }

        public void ExecuteState()
        {
            if (Vector3.Distance(owner.transform.position, enemyScript.navMeshAgent.destination) < 0.1)
            {
                enemyScript.SetState(Enemy.MyState.calming);
            }

            if (enemyScript.alertLevel > 70)
            {
                enemyScript.SetState(Enemy.MyState.alert);
            }

            if (enemyScript.alertLevel < 10)
            {
                enemyScript.SetState(Enemy.MyState.idle);
            }
        }

        public void ExitState()
        {
            Debug.Log("Exit Caution state");
        }
    }

    public class CalmingState : IStates
    {
        GameObject owner;
        Enemy enemyScript;
        public CalmingState(GameObject owner)
        {
            this.owner = owner;
            enemyScript = owner.GetComponent<Enemy>();
        }
        public void EnterState()
        {
            enemyScript.AlertCooldown(1);
        }

        public void ExecuteState()
        {
            if (Vector3.Distance(owner.transform.position, enemyScript.navMeshAgent.destination) < 0.1)
            {
                enemyScript.SelectRandomPoint();
            }

            if (enemyScript.alertLevel > 70)
            {
                enemyScript.SetState(Enemy.MyState.alert);
            }

            if (enemyScript.alertLevel < 10)
            {
                enemyScript.SetState(Enemy.MyState.idle);
            }
        }

        public void ExitState()
        {
            Debug.Log("Exit Caution state");
        }
    }

    public class AlertState : IStates
    {
        GameObject owner;
        Enemy enemyScript;

        float timer = 5;
        float temptTimer;
        public AlertState(GameObject owner)
        {
            this.owner = owner;
            enemyScript = owner.GetComponent<Enemy>();
        }
        public void EnterState()
        {
            enemyScript.ChangeLightColour(new Color(1, 0, 0, 0));
            enemyScript.UpdateMoveSpeed(1);
        }

        public void ExecuteState()
        {
            enemyScript.UpdatePlayerShadow();
            enemyScript.MoveToPlayer();

            if (!enemyScript.fov.PlayerInFOV())
            {
                if (timer > 0)
                {
                    timer -= Time.deltaTime;
                }
                else
                {

                    enemyScript.alertLevel = 65;
                    enemyScript.SetState(Enemy.MyState.calming);
                }
            }
        }

        public void ExitState()
        {
            Debug.Log("Exit Alert state");
        }
    }

    public class FireState : IStates
    {
        GameObject owner;
        Enemy enemyScript;
        float timer = 1f;
        public FireState(GameObject owner)
        {
            this.owner = owner;
            enemyScript = owner.GetComponent<Enemy>();
        }
        public void EnterState()
        {
            Debug.Log("Fire State");
            enemyScript.ChangeLightColour(new Color(0.5f, 0.5f, 0, 0));
            enemyScript.UpdateMoveSpeed(0);
            enemyScript.FireGun(true);
        }

        public void ExecuteState()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }
            else
            {
                enemyScript.UpdatePlayerShadow();
                enemyScript.AimAtPlayer();
                timer = 0.5f;
            }
        }

        public void ExitState()
        {
            enemyScript.FireGun(false);
            Debug.Log("Exit Fire state");
        }
    }

    public class DeadState : IStates
    {
        GameObject owner;
        Enemy enemyScript;

        public DeadState(GameObject owner)
        {
            this.owner = owner;
            enemyScript = owner.GetComponent<Enemy>();

        }
        public void EnterState()
        {
            enemyScript.ChangeLightColour(new Color(0, 1, 0, 0));

            enemyScript.Dead();

        }

        public void ExecuteState()
        {

        }

        public void ExitState()
        {
        }
    }

    public class FollowState : IStates
    {
        GameObject owner;
        Enemy enemyScript;

        public FollowState(GameObject owner)
        {
            this.owner = owner;
            enemyScript = owner.GetComponent<Enemy>();

        }
        public void EnterState()
        {
            enemyScript.ChangeLightColour(new Color(1, 0.25f, 0, 0));
            enemyScript.SetBreadCrumbGoal();
            enemyScript.UpdateMoveSpeed(0.3f);
            Debug.Log("I Follow");
        }

        public void ExecuteState()
        {            
            if (Vector3.Distance(owner.transform.position, enemyScript.navMeshAgent.destination) < 0.1f)
            {                
                enemyScript.SetBreadCrumbGoal();
            }
        }

        public void ExitState()
        {
        }
    }
}
