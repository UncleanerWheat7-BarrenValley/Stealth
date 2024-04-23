using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponWheelButtonController : MonoBehaviour
{
    [SerializeField]
    WeaponWheelController weaponWheelController;
    public int ID;
    private Animator anim;
    public string itemName;
    public TextMeshProUGUI itemText;
    public Image selectedItem;
    public Sprite icon;

    private void Start()
    {
        anim = GetComponent<Animator>();
        weaponWheelController.UpdateSelected(0);
    }

    public void Selected()
    {
        weaponWheelController.weaponID = ID;
        weaponWheelController.UpdateSelected(ID);        
    }

    public void Deselected()
    {
        weaponWheelController.weaponID = ID;
    }

    public void HoverEnter() 
    {
        anim.SetBool("Hover", true);
        itemText.text = itemName;
    }

    public void HoverExit()
    {
        anim.SetBool("Hover", false);
        itemText.text = "";
    }
}
