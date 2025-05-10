using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public GameObject PlayerObj;
    Transform playerSpawnTransform;

    public delegate void PlayerSpawnDelegate(Transform player);
    public static event PlayerSpawnDelegate OnPlayerSpawn;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform transform in transform)
        {
            if (transform.name.Contains("PlayerSpawn"))
            {
                playerSpawnTransform = transform;
            }
        }
        GameObject thePlayer = Instantiate(PlayerObj, playerSpawnTransform.position, Quaternion.identity);

        OnPlayerSpawn(thePlayer.transform);
    }
}
