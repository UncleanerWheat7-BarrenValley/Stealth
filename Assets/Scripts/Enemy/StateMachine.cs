using UnityEngine;
using UnityEngine.AI;

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

            if (patrol.patrolTransforms.Length > 0) 
            {
                enemyScript.StartPatrol();
            }
        }

        public void ExecuteState()
        {
            
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
            enemyScript.SelectRandomPoint();
            enemyScript.MoveToRandom();
            enemyScript.UpdateMoveSpeed(0.5f);
        }

        public void ExecuteState()
        {
            
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
        public AlertState(GameObject owner)
        {
            this.owner = owner;            
            enemyScript = owner.GetComponent<Enemy>();
        }
        public void EnterState()
        {
            enemyScript.ChangeLightColour(new Color(1,0,0,0));
            enemyScript.UpdateMoveSpeed(1);
        }

        public void ExecuteState()
        {
            enemyScript.MoveToPlayer();
        }

        public void ExitState()
        {
            Debug.Log("Exit Alert state");
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
}
