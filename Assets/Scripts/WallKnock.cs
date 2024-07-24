using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallKnock : MonoBehaviour
{
    public delegate void KnockSound(Vector3 location);
    public static event KnockSound knockSound;

    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip knockSoundClip;

    public void wallKnockSound() 
    {
        audioSource.GetComponent<AudioSource>().clip = knockSoundClip;
        audioSource.Play();
        knockSound(transform.position);
    }
}
