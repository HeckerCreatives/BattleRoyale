using Fusion;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoContainerController : NetworkBehaviour
{
    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] private NetworkObject ContainerParent { get; set; }

    public void Initialize(NetworkObject parent)
    {
        ContainerParent = parent;
    }

    public override void Render()
    {
        SetAmmoParent();
    }

    public override void FixedUpdateNetwork()
    {
        SetAmmoParent();
    }

    private void SetAmmoParent()
    {
        if (ContainerParent == null) return;

        transform.parent = ContainerParent.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
