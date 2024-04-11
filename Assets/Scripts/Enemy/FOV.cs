using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class FOV : MonoBehaviour
{
    [SerializeField]
    EnemyManager enemyManager;

    public float fovValue;
    public float depthOfViewValue;
    public LayerMask playerMask;
    bool playerInFOV = false;    

    [Range(0, 100)] public float alertLevel;
    public void Awake()
    {
        alertLevel = 0;        
    }

    void Start()
    {
        StartCoroutine("FindTargetWithDelay", 0.2f);
    }

    IEnumerator FindTargetWithDelay(float delay)
    {
        while (enemyManager.Health > 0)
        {
            yield return new WaitForSeconds(delay);
            FindTarget();
        }

        yield break;
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

    public bool PlayerInFOV()
    {
        return playerInFOV;
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
            AlertCooldown(2);
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
            else 
            {
                playerInFOV = false;
                AlertCooldown(1);
            }
        }
    }

    private async void AlertCooldown(int multipier)
    {
        while (alertLevel > 0)
        {
            if (playerInFOV)
            {
                break;
            }

            alertLevel = Mathf.MoveTowards(alertLevel, 0, multipier * Time.deltaTime);

            await Task.Yield();
        }
    }
}
