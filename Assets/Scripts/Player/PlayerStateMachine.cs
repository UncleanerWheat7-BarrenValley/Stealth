using UnityEngine;

public class PlayerStateMachine
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

    public class NormalState : IStates
    {
        GameObject owner;
        public NormalState(GameObject owner)
        {
            this.owner = owner;
            owner.GetComponentInParent<PlayerController>().movementSpeed = 5;
        }

        public void EnterState()
        {
            Debug.Log("Enter Idle state");
            owner.GetComponentInChildren<Animator>().SetBool("WallHug", false);
        }

        public void ExecuteState()
        {
        }

        public void ExitState()
        {
            Debug.Log("Exit Idle state");
        }
    }

    public class WallState : IStates
    {
        GameObject owner;
        PlayerController playerController;
        Animator animator;
        public WallState(GameObject owner)
        {
            this.owner = owner;
            playerController = owner.GetComponentInParent<PlayerController>();
            animator = owner.GetComponentInChildren<Animator>();            
        }

        public void EnterState()
        {
            Debug.Log("Enter Wall state");
            playerController.movementSpeed = 1;
            animator.SetBool("WallHug", true);
        }

        public void ExecuteState()
        {
            Debug.DrawRay(owner.transform.localPosition, owner.transform.forward * 0.75f, Color.red);
        }

        public void ExitState()
        {
            Debug.Log("Exit Idle state");
        }
    }

    public class CrouchState : IStates
    {
        GameObject owner;
        PlayerController playerController;
        public CrouchState(GameObject owner)
        {
            this.owner = owner;
            playerController = owner.GetComponentInParent<PlayerController>();
        }

        public void EnterState()
        {
            Debug.Log("Enter Idle state");
            playerController.movementSpeed = 2;
        }

        public void ExecuteState()
        {
        }

        public void ExitState()
        {
            Debug.Log("Exit Idle state");
        }
    }
}
