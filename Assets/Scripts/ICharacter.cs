using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacter
{
    float health { get; set; }
    void Damage(float damage);
    float moveSpeed { get; set; }
}
