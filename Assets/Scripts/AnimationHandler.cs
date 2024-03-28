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
    int attack;
    int gunActive;
    // Start is called before the first frame update
    void Start()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        animator = GetComponent<Animator>();
        animMovementSpeed = Animator.StringToHash("MovementSpeed");
        animWallMovementSpeed = Animator.StringToHash("WallMovementSpeed");
        attack = Animator.StringToHash("Attack");
        gunActive = Animator.StringToHash("GunActive");
    }
   
    private void Update()
    {       
        animator.SetFloat(animMovementSpeed, playerInput.moveAmount);
        animator.SetFloat(animWallMovementSpeed, playerInput.horizontalInput);

        if (playerInput.attack)
        {
            Fire1(playerInput.gunFlag);
        }
    }

    public void Fire1(bool gun)
    {
        animator.SetBool(gunActive, gun);
        animator.SetBool(attack, true);
        playerInput.attack = false;
    }

    public void EndAttack() //animation event
    {
        animator.SetBool(attack, false);
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
