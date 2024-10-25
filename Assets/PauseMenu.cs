using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{

    private void Start()
    {
        Utilities.PauseGameplay();
    }
    public void ContinueGame() 
    {
        gameObject.SetActive(false);
        Utilities.UnpauseGame();
    }
    public void QuitGame() 
    {
        SceneManager.LoadScene(0);
    }
}
