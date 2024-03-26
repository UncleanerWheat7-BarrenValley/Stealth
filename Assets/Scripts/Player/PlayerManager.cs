using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour, ICharacter
{
    public float moveSpeed { get; set; } = 5;
    public int Health { get; set; } = 5;

    public void Damage(int damage)
    {
        Health--;
    }
}
