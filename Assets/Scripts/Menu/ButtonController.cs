using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            button.GetComponent<RectTransform>().localPosition += new Vector3(0, 128 * numberChange, 0);

            Vector3 buttonGoalPos = Vector3.zero;

            if (button.GetComponent<RectTransform>().localPosition.y > 128)
            {
                //buttonGoalPos = new Vector3(0, -256, 0);
                button.GetComponent<RectTransform>().localPosition = new Vector3(0, -256, 0);
            }
            if (button.GetComponent<RectTransform>().localPosition.y < -256)
            {
                //buttonGoalPos = new Vector3(0, 128, 0);
                button.GetComponent<RectTransform>().localPosition = new Vector3(0, 128, 0);
            }

            var currentPos = button.GetComponent<RectTransform>().position;

            //button.GetComponent<RectTransform>().position = Vector3.MoveTowards(currentPos, buttonGoalPos, Time.deltaTime * 0.1f);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 60);
        }

        buttons[selectedNumber].GetComponent<RectTransform>().sizeDelta = new Vector2(480, 90);
    }

    public void SelectButton()
    {
        buttons[selectedNumber].GetComponent<ButtonClick>().ClickedButton(selectedNumber);
    }
}
