using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationHandler : MonoBehaviour
{
    Animator animator;
    public Enemy enemyScript;
    int animMoveSpeed;
    int animFire;
    int animDeath;

    private void Start()
    {
        animator = GetComponent<Animator>();
        animMoveSpeed = Animator.StringToHash("Speed");
        animFire = Animator.StringToHash("Fire");
        animDeath = Animator.StringToHash("Death");
    }

    private void Update()
    {
        animator.SetFloat(animMoveSpeed, enemyScript.currentMoveSpeed);
    }

    public void PlayFire(bool fire) 
    {
        animator.SetBool(animFire, fire);
    }

    public void PlayDeath()
    {
        animator.SetTrigger(animDeath);
    }

    public void DisableSelf() 
    {
        animator.enabled = false;
        GetComponent<CapsuleCollider>().enabled = false;
        this.enabled = false;
    }
}
