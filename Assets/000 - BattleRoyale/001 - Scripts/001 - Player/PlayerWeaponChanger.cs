using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponChanger : NetworkBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerController controller;
    [SerializeField] private Animator playerAnimator;

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

        playerAnimator.SetLayerWeight(inventory.TempLastIndex, 0f);

        playerAnimator.SetTrigger("switchweapon");
    }

    private void ChangeToPrimaryWeapon()
    {
        if (inventory.PrimaryWeapon == null) return;

        if (inventory.WeaponIndex == inventory.PrimaryWeapon.AnimatorID) return;

        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchPrimary)) return;

        inventory.WeaponPrimaryChange();

        playerAnimator.SetLayerWeight(inventory.TempLastIndex, 0f);

        playerAnimator.SetTrigger("switchweapon");
    }

    private void ChangeToSecondaryWeapon()
    {
        if (inventory.WeaponIndex == 5) return;

        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchSecondary)) return;

        inventory.WeaponSecondaryChange();

        playerAnimator.SetLayerWeight(inventory.TempLastIndex, 0f);

        playerAnimator.SetTrigger("switchweapon");
    }
}
