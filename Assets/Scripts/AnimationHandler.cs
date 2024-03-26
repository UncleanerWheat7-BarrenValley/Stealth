using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    Animator animator;
    public PlayerInput playerInput;
    int animMovementSpeed;
    int animWallMovementSpeed;
    int punch;
    // Start is called before the first frame update
    void Start()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        animator = GetComponent<Animator>();
        animMovementSpeed = Animator.StringToHash("MovementSpeed");
        animWallMovementSpeed = Animator.StringToHash("WallMovementSpeed");
        punch = Animator.StringToHash("Punch");

    }
   
    private void Update()
    {       
        animator.SetFloat(animMovementSpeed, playerInput.moveAmount);
        animator.SetFloat(animWallMovementSpeed, playerInput.horizontalInput);

        if (playerInput.punch)
        {
            Fire1();
        }
    }

    public void Fire1()
    {
        animator.SetBool(punch, true);
        playerInput.punch = false;
    }

    public void EndPunch() //animation event
    {
        animator.SetBool(punch, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy")
        {
            Debug.LogWarning("ItHit");
            Debug.LogWarning(other.GetComponent<EnemyManager>().Health);
            other.GetComponent<EnemyManager>().Damage(1);
            Debug.LogWarning(other.GetComponent<EnemyManager>().Health);
        }
    }

}
