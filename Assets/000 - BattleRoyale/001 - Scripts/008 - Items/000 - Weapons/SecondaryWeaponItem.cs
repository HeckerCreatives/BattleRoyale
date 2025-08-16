using Fusion;
using System;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using UnityEngine;

public class SecondaryWeaponItem : NetworkBehaviour, IPickupItem
{
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

    //  ======================

    [SerializeField] private GunSoundController meleeSoundController;

    [SerializeField] private string weaponID;
    [SerializeField] private string weaponName;

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
            if (IsEquipped)
            {
                transform.SetParent(WeaponID == "003"
                    ? PlayerCore.Inventory.RifleHand
                    : PlayerCore.Inventory.BowHand, false);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                transform.SetParent(WeaponID == "003"
                    ? PlayerCore.Inventory.RifleBack
                    : PlayerCore.Inventory.BowBack, false);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        else
        {
            // If dropped, keep the position/rotation as-is (or update if needed)
            transform.SetParent(null, false);
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Only the simulation should update the authoritative state
        if (IsPickedUp)
        {
            if (IsEquipped)
            {
                transform.SetParent(WeaponID == "003"
                    ? PlayerCore.Inventory.RifleHand
                    : PlayerCore.Inventory.BowHand, false);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                transform.SetParent(WeaponID == "003"
                    ? PlayerCore.Inventory.RifleBack
                    : PlayerCore.Inventory.BowBack, false);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        else
        {
            // If dropped, keep the position/rotation as-is (or update if needed)
            transform.SetParent(null, false);
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
        }
    }

    public void InitializeItem(NetworkObject player, int supplies,  bool isBot = false, Action finalAction = null)
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

            if (tempPlayerinventory.MagazineContainer != null)
                tempPlayerinventory.MagazineContainer.DropWeapon();

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

    public async void SpawnBullet(Vector3 cameraHitOrigin, Vector3 cameraHitDirection, float animationLength, bool isBot = false)
    {
        Ray tempRay = new Ray(cameraHitOrigin, cameraHitDirection);

        LagCompensatedHit hit = new LagCompensatedHit();
        bool validTargetFound = false;
        Vector3 raystart = tempRay.origin;
        float resettemptime = Runner.Tick + animationLength;

        while (!validTargetFound)
        {
            if (Runner.LagCompensation.Raycast(raystart, tempRay.direction, 999f, Object.InputAuthority, out hit, enemyLayerMask, HitOptions.IncludePhysX))
            {
                NetworkObject hitObject = hit.Hitbox?.Root.Object;

                if (hitObject != null && hitObject.InputAuthority == Object.InputAuthority)
                {
                    raystart = hit.Point + tempRay.direction * 0.2f;
                    continue;
                }

                validTargetFound = true;
            }

            if (Runner.Tick >= resettemptime)
                break;

            await Task.Yield();
        }

        if (validTargetFound)
        {
            PlayerCore.CurrentPlayerPlayables.SpawnBullets(impactPoint.transform.position, hit);

            if (hit.Hitbox != null)
            {
                if (hit.Hitbox.Root.tag == "Bot")
                {
                    Botdata tempdata = hit.Hitbox.Root.GetComponent<Botdata>();

                    if (tempdata.IsStagger) return;
                    if (tempdata.IsGettingUp) return;
                    if (tempdata.IsDead) return;

                    string tag = hit.Hitbox.tag;

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

                    tempdata.ApplyDamage(tempdamage, isBot ? BotData.BotName : PlayerCore.Username, CurrentPlayer);
                }

                else
                {
                    PlayerPlayables tempplayables = hit.Hitbox.Root.GetComponent<PlayerPlayables>();

                    if (tempplayables.healthV2.IsStagger) return;
                    if (tempplayables.healthV2.IsGettingUp) return;

                    PlayerHealthV2 playerHealth = hit.Hitbox.Root.GetComponent<PlayerHealthV2>();

                    string tag = hit.Hitbox.tag;

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

                    playerHealth.ApplyDamage(tempdamage, PlayerCore.Username, PlayerCore.Object);
                }
            }

            Vector3 mouseWorldPosition = hit.Point;

            Vector3 aimDir = (mouseWorldPosition - impactPoint.transform.position).normalized;

            //  spawn bullets here
        }
    }
}
