using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponWheelButtonController : MonoBehaviour
{
    [SerializeField]
    weaponWheelController weaponWheelController;
    public int ID;
    private Animator anim;
    public string itemName;
    public TextMeshProUGUI itemText;
    public Image selectedItem;
    public Sprite icon;

    public delegate void SelectedWeapon();
    public static event SelectedWeapon selectedWeapon;    

    private void Start()
    {
        anim = GetComponent<Animator>();
        weaponWheelController.UpdateSelectedUI(0);
    }

    public void Selected()
    {
        PlayerController.weaponID = ID;
        weaponWheelController.UpdateSelectedUI(ID);
        selectedWeapon();
    }

    public void Deselected()
    {
        PlayerController.weaponID = 0;
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
