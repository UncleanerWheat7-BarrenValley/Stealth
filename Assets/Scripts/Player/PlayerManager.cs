using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour, ICharacter
{
    private int health = 5;

    public float moveSpeed { get; set; } = 5;
    float ICharacter.health { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public void Damage(float damage)
    {
        health--;
    }
}
