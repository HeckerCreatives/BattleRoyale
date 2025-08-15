using Cinemachine;
using Fusion;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.XR;

public class PlayerAim : NetworkBehaviour
{
    [Header("RIG AIM")]
    [SerializeField] private CinemachineVirtualCamera aimVcam;
    [SerializeField] private Rig hipsRig;
    [SerializeField] private RigBuilder hipsRigBuilder;
    [SerializeField] private MultiAimConstraint hipsConstraint;

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

    public void HipsWeight(float value)
    {
        //RigBuilderSetter(value == 1);
        hipsRig.weight = value;
        hipsConstraint.weight = value;
        SetSourceWeight(0, value);
    }

    //public void RigBuilderSetter(bool setter)
    //{
    //    hipsConstraint.enabled = setter;

    //    //if (setter)
    //    //    hipsRigBuilder.Build();
    //}

    public void EnableRigConstraints()
    {
        foreach (var constraint in hipsRig.GetComponentsInChildren<IRigConstraint>())
        {
            (constraint as MonoBehaviour).enabled = true;
        }
    }

    public void DisableRigConstraints()
    {
        foreach (var constraint in hipsRig.GetComponentsInChildren<IRigConstraint>())
        {
            (constraint as MonoBehaviour).enabled = false;
        }
    }

    public void HipsOffset(float x, float y, float z)
    {
        var data = hipsConstraint.data;
        data.offset = new Vector3(x, y, z); // Set the full offset vector
        hipsConstraint.data = data;         // Reassign to apply changes
    }

    void SetSourceWeight(int index, float newWeight)
    {
        var data = hipsConstraint.data;
        var sources = data.sourceObjects;

        if (index < 0 || index >= sources.Count)
        {
            Debug.LogWarning("Invalid source index");
            return;
        }

        sources.SetWeight(index, newWeight);       // update weight
        data.sourceObjects = sources;              // reassign to data
        hipsConstraint.data = data;
    }
}
