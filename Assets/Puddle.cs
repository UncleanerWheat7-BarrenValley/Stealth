using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puddle : MonoBehaviour
{

    public delegate void PuddleSound(Vector3 location);
    public static event PuddleSound puddleSound;
    public void WaterAnnounce() 
    {
        puddleSound(transform.position);
    }
}
