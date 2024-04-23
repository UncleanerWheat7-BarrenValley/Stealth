using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;

public class WeaponWheelController : MonoBehaviour
{
    public Animator anim;
    private bool weaponWheelSelected = false;
    public Image selectedItem;
    public Sprite[] weaponImages;
    [SerializeField]
    PlayerController playerController;

    public int weaponID;

    private void Start()
    {
        UpdateSelected(0);
    }

    public void OpenWeaponWheel()
    {
        weaponWheelSelected = !weaponWheelSelected;
        anim.SetBool("OpenWeaponWheel", weaponWheelSelected);
    }

    public void UpdateSelected(int ID)
    {
        switch (ID)
        {
            case 0:
                weaponID = ID;
                selectedItem.sprite = weaponImages[0];
                playerController.SelectedWeapon();
                break;
            case 1:
                selectedItem.sprite = weaponImages[1];
                playerController.SelectedWeapon();
                break;
        }
    }
}
