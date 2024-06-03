using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitCrystal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            GetComponent<Animator>().SetTrigger("Hit");
            StartCoroutine(Wait());
        }
    }

    private IEnumerator Wait()
    {
        yield return new WaitForSeconds(2);
        if (SceneUtility.GetScenePathByBuildIndex(SceneManager.GetActiveScene().buildIndex + 1).Length > 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else 
        {
            SceneManager.LoadScene("SampleScene");
        }
    }
}
