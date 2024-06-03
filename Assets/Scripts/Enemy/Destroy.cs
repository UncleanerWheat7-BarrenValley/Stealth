using UnityEngine;

public class Destroy : MonoBehaviour
{
    public delegate void FootRemoval(Vector3 foot);
    public static event FootRemoval footRemoval;

    private void Start()
    {
        Invoke("EndFootprint", 20);
    }

    private void EndFootprint()
    {
        footRemoval(this.gameObject.transform.position);
        Destroy(gameObject);
    }
}
