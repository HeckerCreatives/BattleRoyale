using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion.Sockets;

public class AOIManager : NetworkBehaviour, IInterestEnter, IInterestExit
{
    [SerializeField]
    private float aoiRadius = 60f; // match this with AddPlayerAreaOfInterest

    [Space]
    [SerializeField] private GameObject playerVisualObjects;


    private void OnDrawGizmosSelected()
    {
        // Green transparent wire sphere
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, aoiRadius);
    }

    public override void FixedUpdateNetwork()
    {
        Runner.AddPlayerAreaOfInterest(Object.InputAuthority, transform.position, aoiRadius);
    }

    public void InterestEnter(PlayerRef player)
    {
        if (player == Runner.LocalPlayer) playerVisualObjects.SetActive(true);
    }

    public void InterestExit(PlayerRef player)
    {
        if (player == Runner.LocalPlayer) playerVisualObjects.SetActive(false);
    }
}
