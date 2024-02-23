using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    Animator animator;
    public PlayerInput playerInput;
    int animMovementSpeed;
    // Start is called before the first frame update
    void Start()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        animator = GetComponent<Animator>();
        animMovementSpeed = Animator.StringToHash("MovementSpeed");
        
    }

    private void Update()
    {       
        animator.SetFloat(animMovementSpeed, playerInput.moveAmount);
    }

}
