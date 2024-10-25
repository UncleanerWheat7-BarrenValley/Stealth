using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField]
    GameObject PausePanel;
    public void OpenPauseMenu() 
    {
        PausePanel.SetActive(true);
    }
}
