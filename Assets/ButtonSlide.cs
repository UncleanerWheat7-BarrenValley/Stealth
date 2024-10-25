using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSlide : MonoBehaviour
{    
    public Vector3 goalPos;

    private void Start()
    {
        goalPos = transform.localPosition;
    }

    public void SetGoal(Vector3 goalVect) 
    {
        goalPos = goalVect;        
    }
}
