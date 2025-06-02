using Fusion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public enum PlayerAction
{
    DropShield,
    RepickupArmor
}

public class WeaponItem : NetworkBehaviour
{
    [SerializeField] private WeaponSpawnData weponsData;
    [SerializeField] private MeleeSoundController meleeSoundController;
    [SerializeField] private Vector3 dropRotation;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private Vector3 impactSize;
    public GameObject muzzleFlash;
    public Transform impactPoint;

    [Header("DAMAGE")]
    [SerializeField] private float head;
    [SerializeField] private float body;
    [SerializeField] private float thigh;
    [SerializeField] private float shin;
    [SerializeField] private float foot;
    [SerializeField] private float arm;
    [SerializeField] private float forearm;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public string WeaponID { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public string WeaponName { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int Ammo { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public PlayerNetworkLoader TempPlayer { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public PlayerItemCheckerController ItemCheckerController { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject Back { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject Hand { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool IsPickedUp { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool IsHand { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public Vector3 DropPosition { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int Hit { get; set; }

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
        ItemCheckerController = tempPlayer.GetComponent<PlayerItemCheckerController>();
        Back = back;
        Hand = hand;
        Ammo = ammo;
        IsPickedUp = true;
        IsHand = isHand;
    }

    public override void Render()
    {
        SetWeaponParent();

        if (!IsPickedUp)
        {
            transform.parent = null;
            transform.position = new Vector3(DropPosition.x, DropPosition.y + 0.01f, DropPosition.z);
            transform.rotation = Quaternion.Euler(dropRotation);
        }

        if (!HasStateAuthority)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(Hit):
                        meleeSoundController.PlayHit();
                        break;
                }
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    public void RPC_RepickupPrimaryWeapon(NetworkObject tempPlayer, NetworkObject back, NetworkObject hand, bool isHand = false)
    {
        PlayerInventory playerInventory = tempPlayer.GetComponent<PlayerInventory>();

        if (playerInventory.PrimaryWeapon != null) playerInventory.PrimaryWeapon.DropPrimaryWeapon();

        Object.AssignInputAuthority(tempPlayer.InputAuthority);

        TempPlayer = tempPlayer.GetComponent<PlayerNetworkLoader>();
        ItemCheckerController = tempPlayer.GetComponent<PlayerItemCheckerController>();
        playerInventory.PrimaryWeapon = this;
        playerInventory.PrimaryWeaponSFX = GetComponent<MeleeSoundController>();

        if (isHand)
            playerInventory.WeaponIndex = 2;

        Back = back;
        Hand = hand;
        IsPickedUp = true;
        IsHand = isHand;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    public void RPC_RepickupSecondaryWeapon(NetworkObject tempPlayer, NetworkObject back, NetworkObject hand, bool isHand = false)
    {
        PlayerInventory playerInventory = tempPlayer.GetComponent<PlayerInventory>();

        if (playerInventory.ArrowHolder != null)
        {
            if (playerInventory.SecondaryWeapon != null)
            {
                playerInventory.SecondaryWeapon.DropSecondaryWithAmmoCaseWeapon();
            }
        }
        else
        {
            if (playerInventory.SecondaryWeapon != null)
            {
                playerInventory.SecondaryWeapon.DropSecondaryWeapon();
            }
        }

        Object.AssignInputAuthority(tempPlayer.InputAuthority);

        TempPlayer = tempPlayer.GetComponent<PlayerNetworkLoader>();
        ItemCheckerController = tempPlayer.GetComponent<PlayerItemCheckerController>();
        playerInventory.SecondaryWeapon = this;
        playerInventory.SecondaryWeaponSFX = GetComponent<GunSoundController>();

        if (isHand)
            playerInventory.WeaponIndex = 3;

        Back = back;
        Hand = hand;
        IsPickedUp = true;
        IsHand = isHand;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    public void RPC_RepickupSecondaryWithAmmoCaseWeapon(NetworkObject tempPlayer, NetworkObject back, NetworkObject hand, bool isHand = false)
    {
        PlayerInventory playerInventory = tempPlayer.GetComponent<PlayerInventory>();

        if (playerInventory.SecondaryWeapon != null) playerInventory.SecondaryWeapon.DropSecondaryWithAmmoCaseWeapon();

        Object.AssignInputAuthority(tempPlayer.InputAuthority);

        Runner.Spawn(weponsData.GetItemObject("arrowcontainer"), playerInventory.arrowHandle.transform.position, Quaternion.identity, tempPlayer.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {
            playerInventory.ArrowHolder = obj;
            obj.GetComponent<AmmoContainerController>().Initialize(playerInventory.arrowHandle);
        });

        TempPlayer = tempPlayer.GetComponent<PlayerNetworkLoader>();
        ItemCheckerController = tempPlayer.GetComponent<PlayerItemCheckerController>();
        playerInventory.SecondaryWeapon = this;
        playerInventory.SecondaryWeaponSFX = GetComponent<GunSoundController>();

        if (isHand)
            playerInventory.WeaponIndex = 3;

        Back = back;
        Hand = hand;
        IsPickedUp = true;
        IsHand = isHand;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    public void RPC_RepickupArmor(NetworkObject tempPlayer, NetworkObject back, NetworkObject hand)
    {
        PlayerInventory playerInventory = tempPlayer.GetComponent<PlayerInventory>();

        if (playerInventory.Shield != null) playerInventory.Shield.DropShield();

        Object.AssignInputAuthority(tempPlayer.InputAuthority);

        TempPlayer = tempPlayer.GetComponent<PlayerNetworkLoader>();
        ItemCheckerController = tempPlayer.GetComponent<PlayerItemCheckerController>();
        playerInventory.Shield = this;

        Back = back;
        Hand = hand;
        IsPickedUp = true;
        IsHand = true;
    }

    public void DropPrimaryWeapon()
    {
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;

        DropPosition = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y + 0.01f, TempPlayer.transform.position.z);
        transform.position = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y + 0.01f, TempPlayer.transform.position.z);
        TempPlayer.GetComponent<PlayerInventory>().PrimaryWeapon = null;
        TempPlayer = null;
        ItemCheckerController = null;

        transform.rotation = Quaternion.Euler(dropRotation);
        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }

    public void DropSecondaryWeapon()
    {
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;

        DropPosition = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y + 0.01f, TempPlayer.transform.position.z);
        transform.position = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y + 0.01f, TempPlayer.transform.position.z);
        TempPlayer.GetComponent<PlayerInventory>().SecondaryWeapon = null;
        TempPlayer = null;
        ItemCheckerController = null;

        transform.rotation = Quaternion.Euler(dropRotation);
        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }

    public void DropSecondaryWithAmmoCaseWeapon()
    {
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;

        DropPosition = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y + 0.01f, TempPlayer.transform.position.z);
        transform.position = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y + 0.01f, TempPlayer.transform.position.z);
        TempPlayer.GetComponent<PlayerInventory>().SecondaryWeapon = null;

        Runner.Despawn(TempPlayer.GetComponent<PlayerInventory>().ArrowHolder);

        TempPlayer.GetComponent<PlayerInventory>().ArrowHolder = null;
        TempPlayer = null;
        ItemCheckerController = null;

        transform.rotation = Quaternion.Euler(dropRotation);
        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }

    public void DropShield()
    {
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;

        transform.position = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y, TempPlayer.transform.position.z);
        DropPosition = new Vector3(TempPlayer.transform.position.x, TempPlayer.transform.position.y, TempPlayer.transform.position.z);
        TempPlayer.GetComponent<PlayerInventory>().Shield = null;
        TempPlayer = null;
        ItemCheckerController = null;

        transform.rotation = Quaternion.Euler(dropRotation);
        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_DropPrimaryWeaponOnMoveBattle()
    {
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(dropRotation);
        DropPosition = Vector3.zero;
        TempPlayer.GetComponent<PlayerInventory>().WeaponIndex = 1;
        TempPlayer.GetComponent<PlayerInventory>().PrimaryWeapon = null;
        TempPlayer = null;
        ItemCheckerController = null;
        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_DropSecondaryWeaponOnMoveBattle()
    {
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(dropRotation);
        DropPosition = Vector3.zero;
        TempPlayer.GetComponent<PlayerInventory>().WeaponIndex = 1;
        TempPlayer.GetComponent<PlayerInventory>().SecondaryWeapon = null;

        if (TempPlayer.GetComponent<PlayerInventory>().ArrowHolder != null) Runner.Despawn(TempPlayer.GetComponent<PlayerInventory>().ArrowHolder);

        TempPlayer = null;
        ItemCheckerController = null;
        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_DropShieldOnMoveBattle()
    {
        Back = null;
        Hand = null;
        IsHand = false;
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(dropRotation);
        DropPosition = Vector3.zero;
        TempPlayer.GetComponent<PlayerInventory>().Shield = null;
        TempPlayer = null;
        ItemCheckerController = null;
        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }

    public void PerformFirstAttack(Action action = null)
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
                action?.Invoke();

                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => head,
                    "Body" => body,
                    "Thigh" => thigh,
                    "Shin" => shin,
                    "Foot" => foot,
                    "Arm" => arm,
                    "Forearm" => forearm,
                    _ => 0f
                };

                Debug.Log($"First Weapon Damage by {TempPlayer.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                // Process the hit
                HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesFirst.Add(hitObject);

                Hit++;
            }
        }
    }

    public void PerformSecondAttack(Action action = null)
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
                action?.Invoke();

                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => head,
                    "Body" => body,
                    "Thigh" => thigh,
                    "Shin" => shin,
                    "Foot" => foot,
                    "Arm" => arm,
                    "Forearm" => forearm,
                    //"Head" => 40f,
                    //"Body" => 35f,
                    //"Thigh" => 30f,
                    //"Shin" => 25f,
                    //"Foot" => 20f,
                    //"Arm" => 30f,
                    //"Forearm" => 25f,
                    _ => 0f
                };

                Debug.Log($"Second Fist Damage by {TempPlayer.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                // Process the hit
                HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesSecond.Add(hitObject);

                Hit++;
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
}