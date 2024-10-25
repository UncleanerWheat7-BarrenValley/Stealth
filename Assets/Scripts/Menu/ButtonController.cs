using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    [SerializeField]
    List<Button> buttons = new List<Button>();
    [SerializeField]
    int selectedNumber, topNumber, BottomNumber;
    // Start is called before the first frame update
    void Start()
    {
        selectedNumber = 1;
    }

    Vector3 a = Vector3.zero;

    private void Update()
    {
        foreach (var button in buttons)
        {
            if (button.transform.localPosition == button.GetComponent<ButtonSlide>().goalPos)
            {
                print("Finished");
                continue;
            }

            button.GetComponent<RectTransform>().localPosition = Vector3.MoveTowards
                (button.GetComponent<RectTransform>().localPosition, button.GetComponent<ButtonSlide>().goalPos, 1);
        }
    }

    public void ChangeSelected(int numberChange)
    {
        selectedNumber += numberChange;

        if (selectedNumber > 3)
        {
            selectedNumber = 0;
        }
        if (selectedNumber < 0)
        {
            selectedNumber = 3;
        }

        foreach (var button in buttons)
        {
            Vector3 buttonGoalPos = new Vector3(0, button.GetComponent<ButtonSlide>().goalPos.y + (128 * numberChange), 1);

            if (buttonGoalPos.y > 128)
            {
                buttonGoalPos = new Vector3(0, -256, 0);
            }
            if (buttonGoalPos.y < -256)
            {
                buttonGoalPos = new Vector3(0, 128, 0);
            }
            
            button.GetComponent<ButtonSlide>().SetGoal(buttonGoalPos);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 60);
        }

        buttons[selectedNumber].GetComponent<RectTransform>().sizeDelta = new Vector2(480, 90);
    }

    public void SelectButton()
    {
        buttons[selectedNumber].GetComponent<ButtonClick>().ClickedButton(selectedNumber);
    }
}
