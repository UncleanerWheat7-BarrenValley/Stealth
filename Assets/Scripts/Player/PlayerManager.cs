using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour, ICharacter
{
    [SerializeField]
    PlayerController playerController;

    public float moveSpeed { get; set; } = 5;
    public int Health { get; set; } = 5;

    public void Damage(int damage)
    {
        Health--;
        if (Health <= 0)
        {
            playerController.SetState(PlayerController.MyState.dead);
            return;
        }
    }
}
