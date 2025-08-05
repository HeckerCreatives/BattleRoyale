using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;

public class PrimaryWeaponItem : NetworkBehaviour, IPickupItem
{
    public string WeaponID
    {
        get => weaponID;
    }

    public string WeaponName
    {
        get => weaponName;
    }

    //  ======================

    [SerializeField] private string weaponID;
    [SerializeField] private string weaponName;

    [Space]
    [SerializeField] private Transform impactPoint;
    [SerializeField] private Vector3 impactSize;
    [SerializeField] private LayerMask enemyLayerMask;

    [Space]
    [SerializeField] private Vector3 backRotation;
    [SerializeField] private Vector3 handRotation;
    [SerializeField] private Vector3 dropRotation;

    [field: Header("DAMAGE")]
    [Networked][field: SerializeField] public float Head { get; set; }
    [Networked][field: SerializeField] public float Body { get; set; }
    [Networked][field: SerializeField] public float Thigh { get; set; }
    [Networked][field: SerializeField] public float Shin { get; set; }
    [Networked][field: SerializeField] public float Foot { get; set; }
    [Networked][field: SerializeField] public float Arm { get; set; }
    [Networked][field: SerializeField] public float Forearm { get; set; }

    [field: Header("NETWORK")]
    [Networked][field: SerializeField] public int Supplies { get; set; }
    [Networked][field: SerializeField] public bool IsPickedUp { get; set; }
    [Networked][field: SerializeField] public bool IsEquipped { get; set; }
    [Networked][field: SerializeField] public NetworkObject CurrentPlayer { get; set; }
    [Networked][field: SerializeField] public PlayerOwnObjectEnabler PlayerCore { get; set; }
    [Networked][field: SerializeField] public Botdata BotData { get; set; }
    [Networked][field: SerializeField] public Vector3 Position { get; set; }
    [Networked][field: SerializeField] public NetworkObject Back { get; set; }
    [Networked][field: SerializeField] public NetworkObject Hand { get; set; }

    private readonly HashSet<NetworkObject> hitEnemies = new();
    private readonly List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    public override void Render()
    {
        if (IsPickedUp)
        {
            if (IsEquipped)
            {
                transform.parent = Hand.transform;
                transform.localRotation = Quaternion.Euler(handRotation);
            }
            else
            {
                transform.parent = Back.transform;
                transform.localRotation = Quaternion.Euler(backRotation);
            }

            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.parent = null;
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsPickedUp)
        {
            if (IsEquipped)
            {
                transform.parent = Hand.transform;
                transform.localRotation = Quaternion.Euler(handRotation);
            }
            else
            {
                transform.parent = Back.transform;
                transform.localRotation = Quaternion.Euler(backRotation);
            }

            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.parent = null;
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
        }
    }

    public void InitializeItem(NetworkObject player, NetworkObject backParent, NetworkObject handParent, bool isBot = false)
    {
        BotInventory tempBotinventory = null;
        PlayerInventoryV2 tempPlayerinventory = null;

        if (isBot)
        {
            tempBotinventory = player.GetComponent<BotInventory>();

            if (tempBotinventory.PrimaryWeapon != null) tempBotinventory.PrimaryWeapon.DropWeapon();
        }
        else
        {
            tempPlayerinventory = player.GetComponent<PlayerInventoryV2>();

            if (tempPlayerinventory.PrimaryWeapon != null) tempPlayerinventory.PrimaryWeapon.DropWeapon();
        }

        Object.AssignInputAuthority(player.InputAuthority);

        CurrentPlayer = player;

        if (isBot)
            tempBotinventory.PrimaryWeapon = this;
        else
            tempPlayerinventory.PrimaryWeapon = this;

        Position = backParent.transform.position;
        Back = backParent;
        Hand = handParent;

        transform.parent = CurrentPlayer.transform;

        IsPickedUp = true;

        if (isBot) IsEquipped = true;
        else
        {
            if (tempPlayerinventory.WeaponIndex == 2) IsEquipped = true;
            else IsEquipped = false;
        }

        Supplies = 1;

        if (isBot)
        {
            Botdata tempbotdata = player.GetComponent<Botdata>();
            BotData = tempbotdata;
        }
        else
        {
            PlayerOwnObjectEnabler tempcore = player.GetComponent<PlayerOwnObjectEnabler>();
            PlayerCore = tempcore;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupPrimaryWeapon(NetworkObject player, NetworkObject backParent, NetworkObject handParent)
    {
        InitializeItem(player, backParent, handParent);
    }

    public void DropWeapon()
    {
        IsPickedUp = false;
        transform.parent = null;

        Position = CurrentPlayer.transform.position + new Vector3(0f, 0.1f, 0f);

        if (BotData != null)
            CurrentPlayer.GetComponent<BotInventory>().PrimaryWeapon = null;
        else
            CurrentPlayer.GetComponent<PlayerInventoryV2>().PrimaryWeapon = null;

        IsEquipped = false;

        CurrentPlayer = null;
        Back = null;
        Hand = null;
        PlayerCore = null;
        BotData = null;
        Object.RemoveInputAuthority();
    }

    public void DamagePlayer(bool isFinalHit = false, bool isBot = false)
    {
        if (!IsEquipped) return;

        int hitCount = Runner.LagCompensation.OverlapBox(
            impactPoint.transform.position,
            impactSize,
            impactPoint.transform.rotation,
            Object.InputAuthority,
            hits,
            enemyLayerMask,
            HitOptions.IgnoreInputAuthority
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hits[i].Hitbox;
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null) continue;

            if (hitObject == CurrentPlayer) continue;

            if (hitObject.tag == "Bot")
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();

                if (tempdata.IsStagger) return;
                if (tempdata.IsGettingUp) return;
                if (tempdata.IsDead) return;

                if (!hitEnemies.Contains(hitObject))
                {
                    hitEnemies.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => Head,
                        "Body" => Body,
                        "Thigh" => Thigh,
                        "Shin" => Shin,
                        "Foot" => Foot,
                        "Arm" => Arm,
                        "Forearm" => Forearm,
                        _ => 0f
                    };

                    if (isFinalHit) tempdata.IsStagger = true;
                    else tempdata.IsHit = true;

                    tempdata.ApplyDamage(tempdamage, isBot? BotData.BotName : PlayerCore.Username, CurrentPlayer);
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();

                if (tempplayables.healthV2.IsStagger) return;
                if (tempplayables.healthV2.IsGettingUp) return;

                // Avoid duplicate hits
                if (!hitEnemies.Contains(hitObject))
                {
                    hitEnemies.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => Head,
                        "Body" => Body,
                        "Thigh" => Thigh,
                        "Shin" => Shin,
                        "Foot" => Foot,
                        "Arm" => Arm,
                        "Forearm" => Forearm,
                        _ => 0f
                    };

                    PlayerHealthV2 healthV2 = hitObject.GetComponent<PlayerHealthV2>();

                    if (isFinalHit)
                        healthV2.IsStagger = true;
                    else
                    {
                        healthV2.IsHitUpper = true;
                        healthV2.IsHit = true;
                    }

                    healthV2.ApplyDamage(tempdamage, isBot ? BotData.BotName : PlayerCore.Username, CurrentPlayer);
                }
            }
        }
    }

    public void ClearHitEnemies()
    {
        hitEnemies.Clear();
    }

    public void OnDrawGizmos()
    {
        if (impactPoint == null) return;

        // Save the current matrix
        Matrix4x4 originalMatrix = Gizmos.matrix;

        // Apply the transformation matrix using position and rotation
        Gizmos.matrix = Matrix4x4.TRS(impactPoint.position, impactPoint.rotation, Vector3.one);

        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawWireCube(Vector3.zero, impactSize);

        // Restore the original matrix
        Gizmos.matrix = originalMatrix;
    }
}
