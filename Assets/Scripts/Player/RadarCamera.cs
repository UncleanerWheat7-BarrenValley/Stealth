using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarCamera : MonoBehaviour
{
    [SerializeField]
    GameObject player;
    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, new Vector3(player.transform.position.x, 10, player.transform.position.z), 0.1f);        
    }
}
