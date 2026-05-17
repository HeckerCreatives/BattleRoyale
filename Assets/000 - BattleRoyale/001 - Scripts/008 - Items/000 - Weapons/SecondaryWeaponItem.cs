using Fusion;
using System;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using UnityEngine;

public class SecondaryWeaponItem : NetworkBehaviour, IPickupItem
{
    public bool IsRifle => isRifle;
    public string WeaponID
    {
        get => weaponID;
    }

    public string WeaponName
    {
        get => weaponName;
    }

    public GunSoundController SoundController
    {
        get => meleeSoundController;
    }

    public Transform ImpactPoint { get  => impactPoint; }

    //  ======================

    [SerializeField] private GunSoundController meleeSoundController;

    [Space]
    [SerializeField] private string weaponID;
    [SerializeField] private string weaponName;
    [SerializeField] private bool isRifle;

    [Space]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private Transform impactPoint;

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
    [Networked][field: SerializeField] public Quaternion Rotation { get; set; }

    public override void Render()
    {
        if (HasStateAuthority) return;

        if (IsPickedUp)
        {
            if (TryGetSecondaryCarryParent(out Transform carry))
            {
                transform.SetParent(carry, false);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        else
        {
            // If dropped, keep the position/rotation as-is (or update if needed)
            transform.SetParent(null, false);
            transform.position = Position;
            transform.rotation = Rotation != Quaternion.identity ? Rotation : Quaternion.Euler(dropRotation);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Only the simulation should update the authoritative state
        if (IsPickedUp)
        {
            if (TryGetSecondaryCarryParent(out Transform carry))
            {
                transform.SetParent(carry, false);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        else
        {
            // If dropped, keep the position/rotation as-is (or update if needed)
            transform.SetParent(null, false);
            transform.position = Position;
            transform.rotation = Rotation != Quaternion.identity ? Rotation : Quaternion.Euler(dropRotation);
        }
    }

    /// <summary>Hand/back socket for players (<see cref="PlayerCore"/>) or bots (<see cref="BotData"/> + <see cref="BotInventory"/>).</summary>
    private bool TryGetSecondaryCarryParent(out Transform parent)
    {
        parent = null;

        if (BotData != null)
        {
            if (CurrentPlayer == null) return false;
            BotInventory inv = CurrentPlayer.GetComponent<BotInventory>();
            if (inv == null) return false;

            if (IsRifle)
            {
                parent = IsEquipped ? inv.RifleHand : inv.RifleBack != null ? inv.RifleBack : inv.RifleHand;
            }
            else
            {
                parent = IsEquipped ? inv.BowHand : inv.BowBack != null ? inv.BowBack : inv.BowHand;
            }

            return parent != null;
        }

        if (PlayerCore == null || PlayerCore.Inventory == null) return false;

        if (IsRifle)
        {
            parent = IsEquipped
                ? PlayerCore.Inventory.RifleHand
                : PlayerCore.Inventory.RifleBack != null
                    ? PlayerCore.Inventory.RifleBack
                    : PlayerCore.Inventory.RifleHand;
        }
        else
        {
            parent = IsEquipped
                ? PlayerCore.Inventory.BowHand
                : PlayerCore.Inventory.BowBack != null
                    ? PlayerCore.Inventory.BowBack
                    : PlayerCore.Inventory.BowHand;
        }

        return parent != null;
    }

    public void InitializeItemOnSpawn(Vector3 position, Quaternion rotation, int supplies = 0)
    {
        Position = position;
        Rotation = rotation;
        IsEquipped = false;
        IsPickedUp = false;

        Supplies = supplies;
    }

    public void InitializeItem(NetworkObject player, int supplies, bool isBot = false, Action finalAction = null)
    {
        BotInventory tempBotinventory = null;
        PlayerInventoryV2 tempPlayerinventory = null;

        if (isBot)
        {
            tempBotinventory = player.GetComponent<BotInventory>();

            if (tempBotinventory.SecondaryWeapon != null) tempBotinventory.SecondaryWeapon.DropWeapon();
        }
        else
        {
            tempPlayerinventory = player.GetComponent<PlayerInventoryV2>();

            if (tempPlayerinventory.SecondaryWeapon != null) tempPlayerinventory.SecondaryWeapon.DropWeapon();

            //if (tempPlayerinventory.MagazineContainer != null)
            //    tempPlayerinventory.MagazineContainer.DropWeapon();

            if (WeaponID == "004")
                tempPlayerinventory.RPC_SpawnBowMagazine();
        }

        Object.AssignInputAuthority(player.InputAuthority);

        CurrentPlayer = player;

        if (isBot)
            tempBotinventory.SecondaryWeapon = this;
        else
            tempPlayerinventory.SecondaryWeapon = this;

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

        Supplies = supplies;

        IsPickedUp = true;

        if (isBot) IsEquipped = true;
        else
        {
            if (tempPlayerinventory.WeaponIndex == 3)
            {
                IsEquipped = true;
                //transform.parent = Hand.transform;
            }
            else
            {
                IsEquipped = false;
                //transform.parent = Back.transform;
            }
        }

        Rotation = Quaternion.identity;

        finalAction?.Invoke();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupSecondaryWeapon(NetworkObject player, int supplies)
    {
        InitializeItem(player, supplies);
    }

    public void DropWeapon()
    {
        IsPickedUp = false;
        transform.parent = null;

        Position = CurrentPlayer.transform.position + new Vector3(0f, 0.1f, 0f);

        if (BotData != null)
            CurrentPlayer.GetComponent<BotInventory>().SecondaryWeapon = null;
        else
            CurrentPlayer.GetComponent<PlayerInventoryV2>().SecondaryWeapon = null;

        IsEquipped = false;

        CurrentPlayer = null;
        PlayerCore = null;
        BotData = null;
        Object.RemoveInputAuthority();
    }
}
