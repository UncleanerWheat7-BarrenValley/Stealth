using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Gun : MonoBehaviour
{
    [SerializeField]
    Transform bulletStartPoint;



    [SerializeField]
    string tagToHit;

    public void FireGun()
    {
        Debug.DrawRay(bulletStartPoint.position, bulletStartPoint.forward * 100, UnityEngine.Color.green, 5);
        if (Physics.Raycast(bulletStartPoint.position, bulletStartPoint.forward, out RaycastHit hitInfo, 100))
        {
            if (hitInfo.transform.tag == tagToHit)
            {
                if (tagToHit == "Enemy")
                {
                    Debug.LogWarning(hitInfo.transform.GetComponent<EnemyManager>().Health);
                    hitInfo.transform.GetComponent<EnemyManager>().Damage(3);
                    Debug.LogWarning(hitInfo.transform.GetComponent<EnemyManager>().Health);
                }
                else 
                {
                    Debug.LogWarning(hitInfo.transform.GetComponent<PlayerManager>().Health);
                    hitInfo.transform.GetComponent<PlayerManager>().Damage(3);
                    Debug.LogWarning(hitInfo.transform.GetComponent<PlayerManager>().Health);
                }
            }
        }
        else { print("fail"); }
    }
}
