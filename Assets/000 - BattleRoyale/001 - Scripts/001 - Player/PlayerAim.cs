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

    [Header("RIG AIM")]
    [SerializeField] private Transform aimTF;
    [SerializeField] private LayerMask aimLayerMask;
    [SerializeField] private Rig hipsRig;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly] [field: SerializeField] public CinemachineVirtualCamera AimVCam { get; set; }

    //  ====================

    [Networked] public NetworkButtons ButtonsPrevious { get; set; }

    //  ====================

    public override void FixedUpdateNetwork()
    {
        PlayerAimCamera();
        RigTargetAim();
    }

    private void PlayerAimCamera()
    {
        if (!HasInputAuthority) return;

        if (GetInput<MyInput>(out var input) == false) return;

        var pressed = input.Buttons.GetPressed(ButtonsPrevious);
        var release = input.Buttons.GetReleased(ButtonsPrevious);

        ButtonsPrevious = input.Buttons;

        if (pressed.IsSet(InputButton.Aim))
        {
            AimVCam.gameObject.SetActive(true);
            hipsRig.weight = 1f;
        }
        if (release.IsSet(InputButton.Aim))
        {
            AimVCam.gameObject.SetActive(false);
            hipsRig.weight = 0f;
        }
    }

    private void RigTargetAim()
    {
        if (!HasInputAuthority) return;

        Vector2 screenCenterPoint = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = GameManager.Instance.Camera.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimLayerMask))
            aimTF.position = raycastHit.point;
    }
}
