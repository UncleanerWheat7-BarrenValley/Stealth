using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;

public class weaponWheelController : MonoBehaviour
{
    public Animator anim;
    private bool weaponWheelSelected = false;
    public Image selectedItem;
    public Sprite[] weaponImages;
    [SerializeField]
    PlayerController playerController;

    public void OpenWeaponWheel()
    {
        weaponWheelSelected = !weaponWheelSelected;
        anim.SetBool("OpenWeaponWheel", weaponWheelSelected);
    }

    public void UpdateSelectedUI(int weaponID)
    {
        switch (weaponID)
        {
            case 0:
                selectedItem.sprite = weaponImages[0];
                break;
            case 1:
                selectedItem.sprite = weaponImages[1];
                break;
        }

        OpenWeaponWheel();
    }
}
