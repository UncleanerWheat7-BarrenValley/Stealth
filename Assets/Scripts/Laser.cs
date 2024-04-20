using UnityEngine;
using Color = UnityEngine.Color;

public class Laser : MonoBehaviour
{
    [SerializeField]
    LineRenderer LaserRender;
    [SerializeField]
    Transform LaserStart;
    [SerializeField]
    Vector3 endPoint;

    public delegate void LaserPlayerDetected();
    public static event LaserPlayerDetected laserPlayerDetected;

    // Start is called before the first frame update
    void Start()
    {   
        LaserRender.SetPosition(0, LaserStart.position);        
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(transform.position, transform.up, out RaycastHit hitInfo))
        {
            LaserRender.SetPosition(1, hitInfo.point);
            if (hitInfo.transform.tag == "Player")
            {
                laserPlayerDetected();
            }
        }
    }
}
