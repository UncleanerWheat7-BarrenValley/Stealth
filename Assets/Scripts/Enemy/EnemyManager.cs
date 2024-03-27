using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour, ICharacter
{
    public int Health { get; set; } = 5;
    public float moveSpeed { get; set; } = 5;

    public void Damage(int damage)
    {
        Health -= damage;
        if(Health <= 0) 
        {
            GetComponent<Enemy>().SetState(Enemy.MyState.dead);
            return;
        }
        GetComponent<FOV>().alertLevel = 100;
    }
}
