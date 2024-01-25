using System;
using UnityEngine;

public class StateMachine
{
    public IStates currentState;
   
    public void ChangeState(IStates newState)
    {
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
        public IdleState(GameObject owner) 
        {
            this.owner = owner;
        }

        public void EnterState()
        {
            Debug.Log("Enter Idle state");
            this.owner.GetComponent<Enemy>().light.color = new Color(0, 0, 1, 0);
        }

        public void ExecuteState()
        {
            Debug.Log("Execute Idle state");
        }

        public void ExitState()
        {
            Debug.Log("Exit Idle state");
        }
    }

    public class CautionState : IStates
    {
        GameObject owner;
        public CautionState(GameObject owner)
        {
            this.owner = owner;
        }
        public void EnterState()
        {
            Debug.Log("Enter Caution state");
            this.owner.GetComponent<Enemy>().light.color = new Color(1, 0, 1, 0);
            this.owner.GetComponent<Enemy>().SelectRandomPoint();
            this.owner.GetComponent<Enemy>().MoveToRandom();
        }

        public void ExecuteState()
        {
            if (this.owner.GetComponent<Enemy>().closeEnough()) 
            {
                this.owner.GetComponent<Enemy>().UpdateCurrentState();
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
        public AlertState(GameObject owner)
        {
            this.owner = owner;            
        }
        public void EnterState()
        {
            Debug.Log("Enter Alert state");
            this.owner.GetComponent<Enemy>().light.color = new Color(1,0,0,0);
        }

        public void ExecuteState()
        {
            Debug.Log("Execute Alert state");
            this.owner.GetComponent<Enemy>().MoveToPlayer();
        }

        public void ExitState()
        {
            Debug.Log("Exit Alert state");
        }
    }
}
