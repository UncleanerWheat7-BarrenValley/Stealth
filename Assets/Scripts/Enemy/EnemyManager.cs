using UnityEngine;

public class EnemyManager : MonoBehaviour, ICharacter
{
    [SerializeField]
    Enemy enemy;
    [SerializeField]
    Enemy enemyScript;
    public int Health { get; set; } = 5;
    public float moveSpeed { get; set; } = 5;

    public void Damage(int damage)
    {
        if(enemy.myState == Enemy.MyState.dead) return;

        Health -= damage;
        if(Health <= 0) 
        {
            
            enemy.SetState(Enemy.MyState.dead);
            return;
        }
        enemyScript.alertLevel = 100;
    }
}
