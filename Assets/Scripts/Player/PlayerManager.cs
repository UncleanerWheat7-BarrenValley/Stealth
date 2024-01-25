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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
