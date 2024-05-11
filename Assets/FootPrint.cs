using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootPrint : MonoBehaviour
{
    [SerializeField]
    GameObject footPrint;

    public delegate void FootPlacement(GameObject location);
    public static event FootPlacement footPlacement;

    public void PlaceFootPrint() 
    {
        GameObject FP = Instantiate(footPrint, transform.position, Quaternion.Euler(transform.parent.eulerAngles));
        footPlacement(FP);
        Destroy(FP, 10);
    }
}
