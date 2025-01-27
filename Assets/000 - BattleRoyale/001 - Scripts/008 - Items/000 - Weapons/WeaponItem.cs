using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class WeaponItem : NetworkBehaviour
{
    [SerializeField] private Vector3 dropRotation;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private Vector3 impactSize;
    [SerializeField] private Transform impactPoint;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public string WeaponID { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public string WeaponName { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int Ammo { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public PlayerNetworkLoader TempPlayer { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject Back { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject Hand { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool IsPickedUp { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool IsHand { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public Vector3 DropPosition { get; set; }

    //  ===========================

    [Networked, Capacity(10)] public NetworkLinkedList<NetworkObject> hitEnemiesFirst { get; } = new NetworkLinkedList<NetworkObject>();
    [Networked, Capacity(10)] public NetworkLinkedList<NetworkObject> hitEnemiesSecond { get; } = new NetworkLinkedList<NetworkObject>();
    private readonly List<LagCompensatedHit> hitsFirst = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> hitsSecond = new List<LagCompensatedHit>();

    private ChangeDetector _changeDetector;

    //  ===========================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public void InitializeItem(string name, string id, NetworkObject tempPlayer, NetworkObject back, NetworkObject hand, int ammo, bool isHand = false)
    {
        WeaponID = id;
        WeaponName = name;
        TempPlayer = tempPlayer.GetComponent<PlayerNetworkLoader>();
        Back = back;
        Hand = hand;
        Ammo = ammo;
        IsPickedUp = true;
        IsHand = isHand;
    }

    public override void Render()
    {
        SetWeaponParent();

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(DropPosition):

                    Debug.Log($"Drop Weapon, Pos: {DropPosition}");

                    transform.parent = null;
                    transform.position = new Vector3(DropPosition.x, DropPosition.y + 0.1f, DropPosition.z);
                    transform.rotation = Quaternion.Euler(dropRotation);

                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        SetWeaponParent();
    }

    private void SetWeaponParent()
    {
        if (!IsPickedUp) return;

        if (Back == null || Hand == null) return;

        if (!IsHand)
            transform.parent = Back.transform;
        else
            transform.parent = Hand.transform;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RepickupWeapon(NetworkObject tempPlayer, NetworkObject back, NetworkObject hand, int ammo, bool isHand = false)
    {
        Object.AssignInputAuthority(tempPlayer.InputAuthority);

        PlayerInventory playerInventory = tempPlayer.GetComponent<PlayerInventory>();

        TempPlayer = tempPlayer.GetComponent<PlayerNetworkLoader>();
        playerInventory.PrimaryWeapon = this;

        if (playerInventory.WeaponIndex == 1)
            playerInventory.WeaponIndex = 2;

        Back = back;
        Hand = hand;
        Ammo = ammo;
        IsPickedUp = true;
        IsHand = isHand;
    }

    public void DropWeapon()
    {
        IsPickedUp = false;
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;
        transform.position = DropPosition;
        transform.rotation = Quaternion.Euler(dropRotation);
        DropPosition = TempPlayer.transform.position;
        TempPlayer = null;
        Object.RemoveInputAuthority();
    }

    public void PerformFirstAttack()
    {
        if (TempPlayer == null) return;

        int hitCount = Runner.LagCompensation.OverlapBox(
            impactPoint.transform.position,
            impactSize,
            impactPoint.transform.rotation,
            Object.InputAuthority,
            hitsFirst,
            enemyLayerMask,
            HitOptions.IgnoreInputAuthority
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hitsFirst[i].Hitbox;
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null)
            {
                continue;
            }

            // Avoid duplicate hits
            if (!hitEnemiesFirst.Contains(hitObject))
            {
                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 30f,
                    "Body" => 25f,
                    "Thigh" => 20f,
                    "Shin" => 15f,
                    "Foot" => 10f,
                    "Arm" => 20f,
                    "Forearm" => 15f,
                    _ => 0f
                };

                Debug.Log($"First Weapon Damage by {TempPlayer.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                // Process the hit
                HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesFirst.Add(hitObject);
            }
        }
    }

    public void PerformSecondAttack()
    {
        if (TempPlayer == null) return;

        int hitCount = Runner.LagCompensation.OverlapBox(
            impactPoint.transform.position,
            impactSize,
            impactPoint.transform.rotation,
            Object.InputAuthority,
            hitsSecond,
            enemyLayerMask,
            HitOptions.IgnoreInputAuthority
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hitsSecond[i].Hitbox;

            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null)
            {
                continue;
            }

            // Avoid duplicate hits
            if (!hitEnemiesSecond.Contains(hitObject))
            {
                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 30f,
                    "Body" => 25f,
                    "Thigh" => 20f,
                    "Shin" => 15f,
                    "Foot" => 10f,
                    "Arm" => 20f,
                    "Forearm" => 15f,
                    _ => 0f
                };

                Debug.Log($"Second Fist Damage by {TempPlayer.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                // Process the hit
                HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesSecond.Add(hitObject);
            }
        }
    }

    private void HandleHit(NetworkObject enemy, float damage)
    {
        // Implement your logic for handling a hit here, e.g., apply damage

        PlayerHealth health = enemy.GetComponent<PlayerHealth>();

        health.ReduceHealth(damage, TempPlayer.Username, TempPlayer.Object);
    }

    public void ResetFirstAttack()
    {
        hitEnemiesFirst.Clear();
    }

    public void ResetSecondAttack()
    {
        hitEnemiesSecond.Clear();
    }

    private void OnDrawGizmos()
    {
        if (impactPoint == null) return;

        // Adjust for world scale
        Vector3 worldImpactSize = Vector3.Scale(impactSize, impactPoint.lossyScale);

        // Draw the Gizmo with world-adjusted scale
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(impactPoint.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, worldImpactSize * 2);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }
}