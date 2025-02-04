using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponChanger : NetworkBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerController controller;

    //  ======================

    [Networked] public NetworkButtons PreviousButtons { get; set; }

    MyInput controllerInput;

    //  ======================

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        ChangeToPunchWeapon();
        ChangeToPrimaryWeapon();
        ChangeToSecondaryWeapon();

        PreviousButtons = input.Buttons;
    }

    private void ChangeToPunchWeapon()
    {
        if (inventory.WeaponIndex == 1) return;

        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchHands)) return;

        inventory.WeaponHandChange();
    }

    private void ChangeToPrimaryWeapon()
    {
        if (inventory.WeaponIndex == 2) return;

        if (inventory.PrimaryWeapon == null) return;

        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchPrimary)) return;

        inventory.WeaponPrimaryChange();
    }

    private void ChangeToSecondaryWeapon()
    {
        if (inventory.WeaponIndex == 3) return;

        if (inventory.SecondaryWeapon == null) return;

        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchSecondary)) return;

        inventory.WeaponSecondaryChange();
    }
}
