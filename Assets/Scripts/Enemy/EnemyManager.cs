using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour, ICharacter
{
    [SerializeField]
    Enemy enemy;
    [SerializeField]
    FOV fov;
    public int Health { get; set; } = 5;
    public float moveSpeed { get; set; } = 5;

    public void Damage(int damage)
    {
        Health -= damage;
        if(Health <= 0) 
        {
            enemy.SetState(Enemy.MyState.dead);
            return;
        }
        fov.alertLevel = 100;
    }
}
