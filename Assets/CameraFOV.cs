using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFOV : MonoBehaviour
{
    [SerializeField]
    Transform eyePosition;

    public float fovValue;
    public float depthOfViewValue;
    public LayerMask playerMask;
    bool playerInFOV = false;
    CapsuleCollider playerCapsuleCollider;

    void Start()
    {
        StartCoroutine("FindTargetWithDelay", 0.2f);
    }

    IEnumerator FindTargetWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindTarget();
        }
    }

    void FindTarget()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, fovValue, playerMask);

        if (targetsInViewRadius.Length <= 0) return;

        Transform target = targetsInViewRadius[0].transform;

        if (playerCapsuleCollider == null)
        {
            playerCapsuleCollider = target.GetComponent<CapsuleCollider>();
        }


        Vector3 dirToTarget = (target.position - transform.position).normalized;

        bool distanceInRange = Vector3.Distance(transform.position, target.position) < depthOfViewValue;
        bool radiusInRange = Vector3.Angle(transform.forward, dirToTarget) < fovValue / 2;

        if (!distanceInRange || !radiusInRange)
        {
            playerInFOV = false;
            return;
        }

        Debug.DrawLine(eyePosition.position, target.position + Vector3.up * playerCapsuleCollider.height * 0.8f, Color.red, 1);

        if (Physics.Linecast(eyePosition.position, target.position + Vector3.up * playerCapsuleCollider.height * 0.8f, out RaycastHit hitInfo))
        {
            if (hitInfo.transform.tag == "Player")
            {
                playerInFOV = true;
                print("player Spotted");
            }
            else
            {
                playerInFOV = false;
            }
        }
    }

     public Vector3 DirFromAngle(float angleInDegrees)
    {
        angleInDegrees += transform.eulerAngles.y;
        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),
            0,
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)
            );
    }
}
