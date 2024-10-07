using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuControls : MonoBehaviour
{
    Controls controls;
    Vector2 movementInput;
    public ButtonController buttonController;
    float timer = 0.5f;
    float tempTimer;


    private void OnEnable()
    {
        tempTimer = timer;

        if (controls == null)
        {
            controls = new Controls();
            controls.MenuActions.Move.performed += inputActions => MoveSelected(-(int)inputActions.ReadValue<Vector2>().y);// movementInput = inputActions.ReadValue<Vector2>();
            controls.MenuActions.Select.performed += i => HandleSelect();
        }

        controls.Enable();
    }

    private void MoveSelected(int input)
    {
        buttonController.ChangeSelected(input);
    }

    private void HandleSelect()
    {
        buttonController.SelectButton();
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}
