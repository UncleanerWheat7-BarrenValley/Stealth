using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClick : MonoBehaviour
{
    public void ClickedButton(int input) 
    {
        if (input == 0) 
        {
            print("You clicked options");
        }
        else if (input == 1)
        {
            SceneManager.LoadScene(0);
        }
        else if (input == 2)
        {
            Application.Quit();
        }
    }   
}
