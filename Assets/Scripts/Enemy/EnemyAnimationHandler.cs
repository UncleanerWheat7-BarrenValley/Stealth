using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationHandler : MonoBehaviour
{
    Animator animator;
    public Enemy enemyScript;
    int animMoveSpeed;

    private void Start()
    {
        animator = GetComponent<Animator>();
        animMoveSpeed = Animator.StringToHash("Speed");
    }

    private void Update()
    {
        animator.SetFloat("Speed", enemyScript.currentMoveSpeed);
    }

    public void PlayFire(bool fire) 
    {
        animator.SetBool("Fire", fire);
    }

    public void PlayDeath()
    {
        animator.SetTrigger("Death");
    }

    public void DisableSelf() 
    {
        animator.enabled = false;
        GetComponent<CapsuleCollider>().enabled = false;
        this.enabled = false;
    }
}
