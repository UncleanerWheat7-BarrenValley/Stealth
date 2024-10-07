using RPGCharacterAnims.Lookups;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
    public PlayerController playerController;
    [SerializeField]
    GameObject weaponTemp;
    List<GameObject> weapons;
    List<GameObject> weaponsToShowNext = new List<GameObject>();
    List<GameObject> weaponsToShowPrev = new List<GameObject>();
    public int currentGun = 0;
    int totalWeaponsDisplayed = 5;

    float timer = 5;



    private void OnEnable()
    {
        Utilities.PauseGame();
    }

    private void OnDisable()
    {
        playerController.ActivateGun(currentGun > 0 ? true : false);
        Utilities.UnpauseGame();
    }


    // Start is called before the first frame update
    void Start()
    {
        weapons = playerController.weapons;
        if (weapons.Count < 5)
        {
            totalWeaponsDisplayed = weapons.Count;
        }

        AddGunsToLists();
        DrawGunUI();
    }

    private void AddGunsToLists()
    {
        foreach (GameObject weapon in weapons)
        {
            if (weaponsToShowNext.Count < totalWeaponsDisplayed)
            {
                weaponsToShowNext.Add(weapon);
                weaponsToShowPrev.Add(weapon);
            }
            else
                break;
        }
        weaponsToShowPrev.Reverse();
    }

    private void DrawGunUI()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        weaponsToShowNext.Clear();
        weaponsToShowPrev.Clear();

        int j = 0;

        for (int i = 0; i < totalWeaponsDisplayed; i++)
        {
            j = currentGun + i;

            if (j > weapons.Count - 1)
            {
                j -= weapons.Count;
            }
            weaponsToShowNext.Add(weapons[j]);
        }

        int k = 0;

        for (int i = 0; i < totalWeaponsDisplayed; i++)
        {
            k = currentGun - i;
            if (k < 0)
            {
                k += weapons.Count;
            }
            weaponsToShowPrev.Add(weapons[k]);
        }

        for (int i = 0; i < totalWeaponsDisplayed; i++)
        {
            GameObject UIWeaponNext = Instantiate(weaponTemp, transform.position + Vector3.up * (150 * i), Quaternion.identity, transform);
            UIWeaponNext.GetComponent<Image>().sprite = weaponsToShowNext[i].GetComponent<Image>().sprite;
        }

        for (int i = 0; i < totalWeaponsDisplayed; i++)
        {
            GameObject UIWeaponPrev = Instantiate(weaponTemp, transform.position + Vector3.left * (150 * i), Quaternion.identity, transform);
            UIWeaponPrev.GetComponent<Image>().sprite = weaponsToShowPrev[i].GetComponent<Image>().sprite;
        }
    }

    public void ChangeSelectedGun(int gunSelection)
    {
        if (gunSelection > 0)
        {
            currentGun++;
            if (currentGun > weapons.Count - 1)
            {
                currentGun = 0;
            }
        }
        if (gunSelection < 0)
        {
            currentGun--;
            if (currentGun < 0)
            {
                currentGun = weapons.Count - 1;
            }
        }
        DrawGunUI();
    }
}
