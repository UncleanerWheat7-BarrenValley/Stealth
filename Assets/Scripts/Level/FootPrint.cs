using UnityEngine;

public class FootPrint : MonoBehaviour
{
    [SerializeField]
    GameObject footPrint;    

    public delegate void FootPlacement(Vector3 location);
    public static event FootPlacement footPlacement;

    public void PlaceFootPrint()
    {
        if (FootprintManager.useFootprints)
        {
            GameObject FP = Instantiate(footPrint, transform.position, Quaternion.Euler(transform.parent.eulerAngles));
            footPlacement(FP.transform.position);
        }
    }
}
