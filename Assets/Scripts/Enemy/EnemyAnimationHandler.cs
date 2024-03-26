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
}