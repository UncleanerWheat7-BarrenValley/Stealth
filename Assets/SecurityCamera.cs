using BrunetonsImprovedAtmosphere;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityCamera : MonoBehaviour
{   
    public float speed;
    public float rotationLimit;
    private Quaternion leftRotation;   // Left rotation limit
    private Quaternion rightRotation;
    private float rotationTime = 0f;   // Time to interpolate
    public float cameraPauseTime;

    private void Start()
    {
        leftRotation = transform.rotation * Quaternion.Euler(0, -rotationLimit / 2, 0);
        rightRotation = transform.rotation * Quaternion.Euler(0, rotationLimit / 2, 0);
        speed = rotationLimit / 5;

        StartCoroutine(RotateCameraObj());
    }

    IEnumerator RotateCameraObj() 
    {
        while (true)
        {
            

            yield return RotateToTarget(rightRotation);
            yield return new WaitForSeconds(cameraPauseTime);
            yield return RotateToTarget(leftRotation);
            yield return new WaitForSeconds(cameraPauseTime);
        }
    }


    IEnumerator RotateToTarget(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed * Time.deltaTime);
            yield return null;
        }
    }

}
