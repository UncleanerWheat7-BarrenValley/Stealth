using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField]
    Transform bulletStartPoint;
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip gunFire;
    [SerializeField] 
    bool inaccurate;

    float randX = 0, randY = 0;
    public float randXEdge, randYEdge;


    [SerializeField]
    string tagToHit;
    [SerializeField]
    private TrailRenderer bulletTrail;
    
    public delegate void ShootSound(Vector3 location);
    public static event ShootSound shootSound;

    public void FireGun()
    {
        audioSource.GetComponent<AudioSource>().clip = gunFire;
        audioSource.Play();
        shootSound(transform.position);

        if (inaccurate)
        {
            randX = Random.Range(-randXEdge, randXEdge);
            randY = Random.Range(-randYEdge, 0);
        }

        Vector3 direction = bulletStartPoint.forward + bulletStartPoint.right * randX + bulletStartPoint.up * randY;
        Debug.DrawRay(bulletStartPoint.position, direction * 100, Color.green, 5);

        if (Physics.Raycast(bulletStartPoint.position, bulletStartPoint.forward + new Vector3(randX, randY, 0), out RaycastHit hitInfo, 100))
        {
            TrailRenderer trail = Instantiate(bulletTrail, bulletStartPoint.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, hitInfo));

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
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 startPos = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPos, hit.point, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }

        Destroy(trail.gameObject, trail.time);
    }
}
