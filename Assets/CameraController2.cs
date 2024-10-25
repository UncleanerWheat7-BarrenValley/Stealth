using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController2 : MonoBehaviour
{
    PlayerInput playerInput;

    Transform playerTarget;
    Transform target;

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    private Vector3 cameraNaturalPos = new Vector3(0, 4, -4);
    private Vector3 cameraNaturalRot = new Vector3(32, 0, 0);
    private Vector3 cameraOverlookPos = new Vector3(0, 6, -1);
    private Vector3 cameraOverlookRot = new Vector3(64, 0, 0);
    Vector3 lineCastStartPos;

    bool fixedCam = false;

    float r;
    void Start()
    {
        playerTarget = GameObject.Find("Player").transform;
        ResetCameraTarget();
    }


    void FixedUpdate()
    {
        lineCastStartPos = (target.position + Vector3.up * 0.5f) + cameraNaturalPos;

        if (fixedCam)
        {
            //transform.LookAt(playerTarget.position);
        }
        else
        {
            if (Physics.Linecast(lineCastStartPos, target.position + Vector3.up * 1, out RaycastHit hitInfo))
            {
                print(hitInfo.transform.name);
                Debug.DrawLine(lineCastStartPos, target.position + Vector3.up * 0.5f, Color.blue);
                if (hitInfo.transform.tag == "Player")
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position + cameraNaturalPos, 20 * Time.fixedDeltaTime);
                    //transform.position = target.position + cameraNaturalPos;
                    float Angle = Mathf.SmoothDampAngle(transform.eulerAngles.x, cameraNaturalRot.x, ref r, 0.1f);
                    transform.rotation = Quaternion.Euler(Angle, 0, 0);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position + cameraOverlookPos, 20 * Time.fixedDeltaTime);
                    //transform.position = target.position + cameraOverlookPos;
                    float Angle = Mathf.SmoothDampAngle(transform.eulerAngles.x, cameraOverlookRot.x, ref r, 0.1f);
                    transform.rotation = Quaternion.Euler(Angle, 0, 0);
                }
            }
        }
    }

    public void SetCameraTarget(Vector3 newCameraPos)
    {
        transform.position = newCameraPos;
        fixedCam = true;
    }
    public void ResetCameraTarget()
    {
        fixedCam = false;
        target = playerTarget;
        transform.position = target.position + cameraNaturalPos;
    }
}
