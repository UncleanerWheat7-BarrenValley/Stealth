using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepSystem : MonoBehaviour
{
    [SerializeField]
    AudioSource audioSource;
    [SerializeField]
    AudioClip Concrete, Water;

    RaycastHit hit;
    [SerializeField]
    Transform rayStart;
    float range = 0.5f;
    public LayerMask LayerMask;

    private void Update()
    {
        Debug.DrawRay(rayStart.position, rayStart.transform.up * range * -1, Color.red);
    }

    public void Footstep(float volume)
    {
        if (Physics.Raycast(rayStart.position, rayStart.transform.up * -1, out hit, range, LayerMask))
        {
            audioSource.volume = volume * 0.1f;
            if (hit.collider.CompareTag("Concrete"))
            {
                PlayFootstepSound(Concrete);
            }
            else if (hit.collider.CompareTag("Water"))
            {
                PlayFootstepSound(Water);
                ToggleFootprint(true);
                StartCoroutine(FootprintTimer());
            }
            else if (hit.collider.CompareTag("Snow"))
            {
                ToggleFootprint(true);
            }
        }
    }

    private void PlayFootstepSound(AudioClip audio)
    {
        audioSource.pitch = UnityEngine.Random.Range(0.8f, 1);
        audioSource.PlayOneShot(audio);
    }

    void ToggleFootprint(bool active)
    {
        FootprintManager.useFootprints = active;
    }

    IEnumerator FootprintTimer() 
    {
        yield return new WaitForSeconds(10);

        ToggleFootprint(false);
    }
}
