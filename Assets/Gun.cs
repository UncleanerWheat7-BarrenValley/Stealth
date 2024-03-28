using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Gun : MonoBehaviour
{
    [SerializeField]
    Transform bulletStartPoint;
    public void FireGun()
    {
        Debug.DrawRay(bulletStartPoint.position, transform.forward * 100, UnityEngine.Color.green,10000);
        if (Physics.Raycast(bulletStartPoint.position, transform.forward, out RaycastHit hitInfo, 100))
        {
            if (hitInfo.transform.tag == "Enemy")
            {
                Debug.LogWarning("ItHit");
                Debug.LogWarning(hitInfo.transform.GetComponent<EnemyManager>().Health);
                hitInfo.transform.GetComponent<EnemyManager>().Damage(3);
                Debug.LogWarning(hitInfo.transform.GetComponent<EnemyManager>().Health);
                print("yay");
            }
        }
        else { print("fail"); }
    }
}
