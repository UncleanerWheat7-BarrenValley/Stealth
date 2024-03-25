using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class FOV : MonoBehaviour
{
    public float fovValue;
    public float depthOfViewValue;
    float angleValue;
    public LayerMask playerMask;
    bool playerInFOV = false;

    [Range(0, 100)] public float alertLevel;
    public void Awake()
    {
        alertLevel = 0;

        Vector3 directionToTarget = transform.position - new Vector3(transform.position.x + fovValue / 2, transform.position.y, transform.position.z + depthOfViewValue);
        angleValue = Vector3.SignedAngle(-transform.forward, directionToTarget, Vector3.up);
    }

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

    public Vector3 DirFromAngle(float angleInDegrees)
    {
        angleInDegrees += transform.eulerAngles.y;
        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),
            0,
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)
            );
    }

    void FindTarget()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, fovValue, playerMask);
        Transform target = targetsInViewRadius[0].transform;
        Vector3 dirToTarget = (target.position - transform.position).normalized;

        bool distanceInRange = Vector3.Distance(transform.position, target.position) < depthOfViewValue;
        bool radiusInRange = Vector3.Angle(transform.forward, dirToTarget) < fovValue / 2;

        if (!distanceInRange || !radiusInRange) 
        {
            playerInFOV = false;
            AlertCooldown();
            return;
        }

        if (Physics.Linecast(transform.position, target.position, out RaycastHit hitInfo))
        {
            print(hitInfo.transform.name);
            if (hitInfo.transform.tag == "Player")
            {
                playerInFOV = true;
                if (alertLevel < 100)
                {
                    alertLevel += 50 / Vector3.Distance(transform.position, target.position);
                }
            }
        }
    }

    private async void AlertCooldown()
    {
        while (alertLevel > 0)
        {
            if (playerInFOV)
            {
                break;
            }

            alertLevel = Mathf.MoveTowards(alertLevel, 0, 5 * Time.deltaTime);

            await Task.Yield();
        }
    }
}
