using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAim : MonoBehaviour
{
    [SerializeField] private GameplayController controller;
    [SerializeField] private GameObject aimVCam;

    [Header("RIG AIM")]
    [SerializeField] private Transform aimTF;
    [SerializeField] private LayerMask aimLayerMask;
    [SerializeField] private Rig hipsRig;

    private void Update()
    {
        PlayerAimCamera();
        RigTargetAim();
        ActivateHipsRig();
    }

    private void PlayerAimCamera()
    {
        aimVCam.SetActive(controller.Aim);
    }

    private void RigTargetAim()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = GameManager.Instance.Camera.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimLayerMask))
            aimTF.position = raycastHit.point;
    }

    private void ActivateHipsRig()
    {
        hipsRig.weight = controller.Aim ? 1f : 0f;
    }
}
