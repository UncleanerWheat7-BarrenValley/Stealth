using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem;
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
        Debug.DrawRay(LaserStart.position, transform.forward, Color.green, 5);
        Debug.DrawRay(transform.position, transform.up, Color.blue, 5);
        Debug.DrawRay(LaserStart.position, transform.right, Color.red, 5);

        if (Physics.Raycast(transform.position, transform.up, out RaycastHit hitInfo))
        {
            LaserRender.SetPosition(1, hitInfo.point);
            if (hitInfo.transform.tag == "Player")
            {
                laserPlayerDetected();
                print("Laser Hit player");
            }
            else
            {
                print("Laser Hit " + hitInfo.transform.name);
            }
        }
    }
}
