using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSlide : MonoBehaviour
{
    [SerializeField]
    Vector3 goalPos;

    // Update is called once per frame
    void Update()
    {
        if (transform.localPosition == goalPos) return;

        Vector3.MoveTowards(transform.localPosition, goalPos, Time.deltaTime * 0.1f);
    }

    public void SetGoal(Vector3 goalVect) 
    {
        goalPos = goalVect;
    }
}
