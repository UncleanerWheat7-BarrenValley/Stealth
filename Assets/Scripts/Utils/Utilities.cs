using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Utilities
{
    public static void PauseGame()
    {
        Time.timeScale = 0;
    }

    public static void UnpauseGame()
    {
        Time.timeScale = 1;
    }
}
