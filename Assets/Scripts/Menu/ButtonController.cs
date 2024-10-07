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

        if (selectedNumber > 2)
        {
            selectedNumber = 0;
        }
        if (selectedNumber < 0)
        {
            selectedNumber = 2;
        }

        foreach (var button in buttons)
        {
            button.GetComponent<RectTransform>().localPosition += new Vector3(0, 128 * numberChange, 0);

            if (button.GetComponent<RectTransform>().localPosition.y > 128) 
            {
                button.GetComponent<RectTransform>().localPosition = new Vector3(0, -128, 0);
            }
            if (button.GetComponent<RectTransform>().localPosition.y < -128)
            {
                button.GetComponent<RectTransform>().localPosition = new Vector3(0, 128, 0);
            }

            button.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 60);
        }

        buttons[selectedNumber].GetComponent<RectTransform>().sizeDelta = new Vector2(480, 90);
    }

    public void SelectButton() 
    {
        buttons[selectedNumber].GetComponent<ButtonClick>().ClickedButton(selectedNumber);
    }
}
