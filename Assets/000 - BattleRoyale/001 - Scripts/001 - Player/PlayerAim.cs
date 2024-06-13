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

    [Header("RIG AIM")]
    [SerializeField] private Rig hipsRig;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly] [field: SerializeField] public CinemachineVirtualCamera AimVCam { get; set; }
    [field: MyBox.ReadOnly] [field: SerializeField] [Networked] public NetworkBool IsAim { get; private set; }

    //  ====================

    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    [Networked] public NetworkButtons Released { get; set; }
    [Networked] public NetworkButtons Pressed { get; set; }

    //  ====================

    public override void FixedUpdateNetwork()
    {
        NetworkAim();
    }

    private void NetworkAim()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        if (input.Buttons.WasPressed(ButtonsPrevious, InputButton.Aim))
            IsAim = !IsAim;

        PlayerAimCamera();
        HipsWeightNetwork();

        ButtonsPrevious = input.Buttons;
    }

    private void PlayerAimCamera()
    {
        if (!HasInputAuthority) return;

        if (AimVCam == null) return;

        if (IsAim)
            AimVCam.gameObject.SetActive(true);
        else
            AimVCam.gameObject.SetActive(false);
    }

    private void HipsWeightNetwork()
    {
        if (playerController.IsProne)
        {
            hipsRig.weight = 0f;
            return;
        }

        if (IsAim)
            hipsRig.weight = 1f;
        else
            hipsRig.weight = 0f;
    }
}
