using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    public delegate void FootRemoval(GameObject foot);
    public static event FootRemoval footRemoval;
    private void OnDestroy()
    {
        footRemoval(this.gameObject);
    }
}
