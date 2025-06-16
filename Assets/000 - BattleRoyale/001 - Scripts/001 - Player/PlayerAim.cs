using Cinemachine;
using Fusion;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.XR;

public class PlayerAim : NetworkBehaviour
{

    //  =====================

    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("RIG AIM")]
    [SerializeField] private CinemachineVirtualCamera aimVcam;
    [SerializeField] private Rig hipsRig;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly] [field: SerializeField] [Networked] public NetworkBool IsAim { get; private set; }

    //  ====================

    [Networked] public NetworkButtons ButtonsPrevious { get; set; }

    //  ====================

    //public override void Render()
    //{
    //    if (!HasStateAuthority) 
    //    {
    //        HipsWeightNetwork();
    //    }
    //}

    //public override void FixedUpdateNetwork()
    //{
    //    NetworkAim();

    //    if (HasStateAuthority)
    //        HipsWeightNetwork();
    //}

    //private void NetworkAim()
    //{
    //    if (GetInput<MyInput>(out var input) == false) return;

    //    if (input.Buttons.WasPressed(ButtonsPrevious, InputButton.Aim))
    //        IsAim = !IsAim;

    //    PlayerAimCamera();

    //    ButtonsPrevious = input.Buttons;
    //}

    //private void PlayerAimCamera()
    //{
    //    if (!HasInputAuthority) return;

    //    if (IsAim)
    //        aimVcam.gameObject.SetActive(true);
    //    else
    //        aimVcam.gameObject.SetActive(false);
    //}

    //private void HipsWeightNetwork()
    //{
    //    if (playerController.IsProne)
    //    {
    //        hipsRig.weight = 0f;
    //        return;
    //    }

    //    if (IsAim)
    //        hipsRig.weight = 1f;
    //    else
    //    {
    //        if (playerInventory.WeaponIndex != 3)
    //        {
    //            hipsRig.weight = 0f;
    //            return;
    //        }

    //        hipsRig.weight = 1f;
    //    }
    //}
}
