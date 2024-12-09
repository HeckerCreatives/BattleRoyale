using Fusion;
using MyBox;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;


public class PlayerInventory : NetworkBehaviour
{
    //  ==========================

    [SerializeField] private UserData userData;
    [SerializeField] private WeaponSpawnData weaponSpawnData;
    [SerializeField] private Animator playerAnimator;

    [Header("SKINS")]
    [SerializeField] private List<GameObject> hairStyles;
    [SerializeField] private List<MeshRenderer> hairMR;
    [SerializeField] private SkinnedMeshRenderer bodyColorMR;
    [SerializeField] private SkinnedMeshRenderer upperClothingMR;
    [SerializeField] private SkinnedMeshRenderer lowerClothingMR;

    [Header("SKIN COLOR")]
    [SerializeField] private List<Color> hairColor;
    [SerializeField] private List<Color> clothingColor;
    [SerializeField] private List<Color> skinColor;

    [Header("WEAPON BACK HANDLE")]
    [SerializeField] private NetworkObject swordHandle;
    [SerializeField] private NetworkObject spearHandle;
    [SerializeField] private NetworkObject rifleHandle;
    [SerializeField] private NetworkObject bowHandle;
    [SerializeField] private NetworkObject arrowHandle;

    [Header("WEAPON HAND HANDLE")]
    [SerializeField] private NetworkObject swordHandHandle;
    [SerializeField] private NetworkObject spearHandHandle;
    [SerializeField] private NetworkObject rifleHandHandle;
    [SerializeField] private NetworkObject bowHandHandle;

    [Header("WEAPON EQUIP BUTTONS")]
    [SerializeField] private WeaponEquipBtnController HandBtn;
    [SerializeField] private WeaponEquipBtnController PrimaryBtn;
    [SerializeField] private WeaponEquipBtnController SecondaryBtn;

    [Header("DEBUGGER NETWORK")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsSkinInitialized { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsWeaponInitialize { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int WeaponIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int TempLastIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairStyle { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int ClothingColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int SkinColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public string EquipWeaponType { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem PrimaryWeapon { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem SecondaryWeapon { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem AmmoObject { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int BowAmmo { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int RifleAmmo { get; set; }

    //  =========================

    private ChangeDetector _changeDetector;

    //  =========================

    public override async void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        while (!Runner) await Task.Delay(100);

        if (HasStateAuthority)
        {
            SetWeaponOnStart();
        }

        if (HasInputAuthority)
        {
            RPC_SendPlayerDataToServer(JsonConvert.SerializeObject(userData.CharacterSetting));
        }
        else if (!HasInputAuthority && !HasStateAuthority)
        {
            while(!IsSkinInitialized) await Task.Delay(100);

            InitializeSkinOnStart();
        }
    }

    public override async void Render()
    {
        while (!Runner) await Task.Delay(100);

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsSkinInitialized):
                    Debug.Log($"Activating hair index: {HairStyle}");
                    hairStyles[HairStyle].SetActive(true);
                    Debug.Log($"Activating hair color index: {HairColorIndex}");
                    hairMR[HairStyle].material.SetColor("_BaseColor", hairColor[HairColorIndex]);
                    Debug.Log($"Activating upper clothing color index: {ClothingColorIndex}");
                    upperClothingMR.material.SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
                    Debug.Log($"Activating lower clothing color index: {ClothingColorIndex}");
                    lowerClothingMR.material.SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
                    Debug.Log($"Activating body color index: {SkinColorIndex}");
                    bodyColorMR.material.SetColor("_BaseColor", skinColor[SkinColorIndex]);
                    break;
                case nameof(WeaponIndex):

                    if (!HasInputAuthority) break;

                    while (!IsWeaponInitialize) await Task.Delay(100);

                    while (!HandBtn.gameObject.activeInHierarchy && !PrimaryBtn.gameObject.activeInHierarchy && !SecondaryBtn.gameObject.activeInHierarchy) await Task.Delay(100);

                    HandBtn.SetIndicator(WeaponIndex == 1 ? true : false);
                    PrimaryBtn.SetIndicator(PrimaryWeapon != null ? WeaponIndex == PrimaryWeapon.AnimatorID ? true : false : false);
                    SecondaryBtn.SetIndicator(SecondaryWeapon != null ? WeaponIndex == SecondaryWeapon.AnimatorID ? true : false : false);
                    break;
                case nameof(IsWeaponInitialize):
                    if (!HasInputAuthority) break;

                    while (!HandBtn.gameObject.activeInHierarchy && !PrimaryBtn.gameObject.activeInHierarchy && !SecondaryBtn.gameObject.activeInHierarchy) await Task.Delay(100);

                    HandBtn.SetIndicator(WeaponIndex == 1 ? true : false);
                    PrimaryBtn.SetIndicator(PrimaryWeapon != null ? WeaponIndex == PrimaryWeapon.AnimatorID ? true : false : false);
                    SecondaryBtn.SetIndicator(SecondaryWeapon != null ? WeaponIndex == SecondaryWeapon.AnimatorID ? true : false : false);
                    break;
            }
        }
    }

    private void SetWeaponOnStart()
    {
        WeaponHandChange();

        IsWeaponInitialize = true;
    }

    #region Initialize Player Skin

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendPlayerDataToServer(string data)
    {
        Debug.Log($"Send player to server: {data}");

        var characterSetting = JsonConvert.DeserializeObject<PlayerCharacterSetting>(data);
        HairStyle = characterSetting.hairstyle;
        HairColorIndex = characterSetting.haircolor;
        ClothingColorIndex = characterSetting.clothingcolor;
        SkinColorIndex = characterSetting.skincolor;
        IsSkinInitialized = true;
    }

    private void InitializeSkinOnStart()
    {
        hairStyles[HairStyle].SetActive(true);
        hairMR[HairStyle].material.SetColor("_BaseColor", hairColor[HairColorIndex]);
        upperClothingMR.material.SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        lowerClothingMR.materials[1].SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        bodyColorMR.material.SetColor("_BaseColor", skinColor[SkinColorIndex]);
    }

    #endregion

    #region TELEPORT TO BATTLE FIELD

    public void Rpc_DropWeaponsAfterTeleportBattlefield()
    {
        #region WEAPONS

        Debug.Log("start removing items");

        if (PrimaryWeapon != null)
        {
            Debug.Log("remove primary");
            Rpc_ChangeWeaponInPickup(PrimaryWeapon.AnimatorID);
            Runner.Despawn(PrimaryWeapon.Object);
        }
        if (SecondaryWeapon != null)
        {
            Debug.Log("remove secondary");
            Rpc_ChangeWeaponInPickup(SecondaryWeapon.AnimatorID);
            Runner.Despawn(SecondaryWeapon.Object);
        }
        if (AmmoObject != null) Runner.Despawn(AmmoObject.Object);

        PrimaryWeapon = null;
        SecondaryWeapon = null;
        AmmoObject = null;
        EquipWeaponType = "";
        WeaponIndex = 1;
        TempLastIndex = 0;
        BowAmmo = 0;
        RifleAmmo = 0;

        #endregion

        Rpc_DropWeaponsByServerToPlayer();
    }

    [Rpc (RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_DropWeaponsByServerToPlayer()
    {
        #region UI

        WeaponHandChange();

        PrimaryBtn.ResetUI();
        SecondaryBtn.ResetUI();

        #endregion
    }

    #endregion

    #region Pickup Weapon

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SpawnWeaponForPlayer(string data, PlayerRef player)
    {
        var weaponData = JsonConvert.DeserializeObject<TempItemWeaponData>(data);

        if (weaponSpawnData.GetItemType(weaponData.itemID) == "RifleAmmo")
        {
            RifleAmmo += weaponData.ammo;
            return;
        }
        else if (weaponSpawnData.GetItemType(weaponData.itemID) == "BowAmmo")
        {
            BowAmmo += weaponData.ammo;
            return;
        }

        Runner.Spawn(
            weaponSpawnData.GetItemObject(weaponData.itemID),
            Vector3.zero,
            Quaternion.identity,
            inputAuthority: player,
            onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<WeaponItem>().InitializeItem(
                    weaponSpawnData.GetItemName(weaponData.itemID),
                    weaponData.itemID,
                    weaponSpawnData.GetItemAnimatorIndex(weaponData.itemID),
                    Object,
                    GetWeaponBackHandle(weaponData.itemID),
                    GetWeaponHandHandle(weaponData.itemID),
                    weaponData.ammo,
                    weaponSpawnData.GetItemType(weaponData.itemID) == EquipWeaponType ? true : false);

                HandleWeaponAssignment(weaponData, obj.GetComponent<WeaponItem>(), player);
            });
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_ReassignWeaponToPlayer(NetworkId objectId, PlayerRef player)
    {
        var weaponRunner = Runner.FindObject(objectId);

        if (weaponRunner == null)
        {
            Debug.Log($"No weapon found for reassign player");
            return;
        }

        WeaponItem weapon = weaponRunner.GetComponent<WeaponItem>();

        WeaponItem tempWeapon = null;

        if (weapon.WeaponID == "001" || weapon.WeaponID == "002")
        {
            Rpc_ChangePrimarySpriteWeapon(weapon.WeaponID);

            tempWeapon = PrimaryWeapon;
            if (PrimaryWeapon != null)
            {
                PrimaryWeapon.Object.RemoveInputAuthority();
                PrimaryWeapon.Rpc_DropWeaponClients(transform.position);
            }

            PrimaryWeapon = weapon;
            PrimaryWeapon.Object.AssignInputAuthority(player);
        }
        else if (weapon.WeaponID == "003" || weapon.WeaponID == "004")
        {
            Rpc_ChangeSecondarySpriteWeapon(weapon.WeaponID);

            tempWeapon = SecondaryWeapon;
            if (SecondaryWeapon != null)
            {
                SecondaryWeapon.Object.RemoveInputAuthority();
                SecondaryWeapon.Rpc_DropWeaponClients(transform.position);
            }

            SecondaryWeapon = weapon;
            SecondaryWeapon.Object.AssignInputAuthority(player);
        }

        if (weapon.WeaponID == "004")
        {
            var ammoObject = Runner.Spawn(
            weaponSpawnData.GetItemObject("arrowcontainer"),
            Vector3.zero,
            Quaternion.identity,
            inputAuthority: player,
            onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<WeaponItem>().InitializeItem("", "0", 0, obj, arrowHandle, null, 0);
            });
        }

        weapon.Rpc_ReassignParentPlayer(Object, GetWeaponBackHandle(weapon.WeaponID).Id, GetWeaponHandHandle(weapon.WeaponID).Id, weaponSpawnData.GetItemType(weapon.WeaponID) == EquipWeaponType ? true : false);

        if (weaponSpawnData.GetItemType(weapon.WeaponID) == "Primary")
        {
            if (tempWeapon != null && EquipWeaponType == weaponSpawnData.GetItemType(weapon.WeaponID))
            {
                Rpc_ChangeWeaponInPickup(tempWeapon.AnimatorID);

                WeaponPrimaryChange();
            }
        }
        else if (weaponSpawnData.GetItemType(weapon.WeaponID) == "Secondary")
        {
            if (tempWeapon != null && EquipWeaponType == weaponSpawnData.GetItemType(weapon.WeaponID))
            {
                Rpc_ChangeWeaponInPickup(tempWeapon.AnimatorID);

                WeaponSecondaryChange();
            }
        }
    }

    private void HandleWeaponAssignment(TempItemWeaponData weaponData, WeaponItem weaponItem, PlayerRef playerRef)
    {
        switch (weaponData.itemID)
        {
            case "001":
            case "002":
                ReplacePrimaryWeapon(weaponItem);
                break;
            case "003":
                ReplaceSecondaryWeapon(weaponItem);
                break;
            case "004":
                ReplaceWeaponWithAmmoObject(weaponItem, playerRef);
                break;
        }
    }

    private void ReplacePrimaryWeapon(WeaponItem weaponItem)
    {
        WeaponItem tempWeapon = null;

        Rpc_ChangePrimarySpriteWeapon(weaponItem.WeaponID);

        if (PrimaryWeapon != null)
        {
            tempWeapon = PrimaryWeapon;
            PrimaryWeapon.Object.RemoveInputAuthority();
            PrimaryWeapon.Rpc_DropWeaponClients(transform.position);
        }

        PrimaryWeapon = weaponItem;

        if (EquipWeaponType == "Primary")
        {
            if (tempWeapon != null)
                Rpc_ChangeWeaponInPickup(tempWeapon.AnimatorID);

            WeaponPrimaryChange();
        }
    }

    private void ReplaceSecondaryWeapon(WeaponItem weaponItem)
    {
        if (AmmoObject != null)
        {
            Runner.Despawn(AmmoObject.Object);
            AmmoObject = null;
        }

        WeaponItem tempWeapon = null;

        Rpc_ChangeSecondarySpriteWeapon(weaponItem.WeaponID);

        if (SecondaryWeapon != null)
        {
            tempWeapon = SecondaryWeapon;
            SecondaryWeapon.Object.RemoveInputAuthority();
            SecondaryWeapon.Rpc_DropWeaponClients(transform.position);
        }

        SecondaryWeapon = weaponItem;

        if (EquipWeaponType == "Secondary")
        {
            if (tempWeapon != null)
                Rpc_ChangeWeaponInPickup(tempWeapon.AnimatorID);

            WeaponSecondaryChange();
        }
    }

    private void ReplaceWeaponWithAmmoObject(WeaponItem weaponItem, PlayerRef player)
    {
        if (AmmoObject != null)
        {
            Runner.Despawn(AmmoObject.Object);
            AmmoObject = null;
        }

        WeaponItem tempWeapon = null;

        Rpc_ChangeSecondarySpriteWeapon(weaponItem.WeaponID);

        if (SecondaryWeapon != null)
        {
            tempWeapon = SecondaryWeapon;
            SecondaryWeapon.Object.RemoveInputAuthority();
            SecondaryWeapon.Rpc_DropWeaponClients(transform.position);
        }

        var ammoObject = Runner.Spawn(
            weaponSpawnData.GetItemObject("arrowcontainer"),
            Vector3.zero,
            Quaternion.identity,
            inputAuthority: player, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<WeaponItem>().InitializeItem(
                    "",
                    "",
                    0,
                    Object,
                    arrowHandle,
                    null,
                    0,
                    false);
            });

        AmmoObject = ammoObject.GetComponent<WeaponItem>();
        SecondaryWeapon = weaponItem;

        if (EquipWeaponType == "Secondary")
        {
            if (tempWeapon != null)
                Rpc_ChangeWeaponInPickup(tempWeapon.AnimatorID);

            WeaponSecondaryChange();
        }
    }

    private NetworkObject GetWeaponBackHandle(string itemID)
    {
        return itemID switch
        {
            "001" => swordHandle,
            "002" => spearHandle,
            "003" => rifleHandle,
            "004" => bowHandle,
            _ => null
        };
    }

    private NetworkObject GetWeaponHandHandle(string itemID)
    {
        return itemID switch
        {
            "001" => swordHandHandle,
            "002" => spearHandHandle,
            "003" => rifleHandHandle,
            "004" => bowHandHandle,
            _ => null
        };
    }

    #endregion

    #region Weapon Changer

    [Rpc (RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_ChangeWeaponInPickup(int tempWeapon)
    {
        playerAnimator.SetLayerWeight(tempWeapon, 0f);
    }

    public void WeaponHandChange()
    {
        EquipWeaponType = "";
        TempLastIndex = WeaponIndex;
        WeaponIndex = 1;

        PrimaryWeapon?.Rpc_SheatWeapon();
        SecondaryWeapon?.Rpc_SheatWeapon();
    }

    public void WeaponPrimaryChange()
    {
        EquipWeaponType = "Primary";
        TempLastIndex = WeaponIndex;
        WeaponIndex = PrimaryWeapon.AnimatorID;

        PrimaryWeapon.Rpc_ActivateWeapon();
        SecondaryWeapon?.Rpc_SheatWeapon();
    }

    public void WeaponSecondaryChange()
    {
        EquipWeaponType = "Secondary";
        TempLastIndex = WeaponIndex;
        WeaponIndex = SecondaryWeapon.AnimatorID;

        SecondaryWeapon.Rpc_ActivateWeapon();
        PrimaryWeapon?.Rpc_SheatWeapon();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_ChangePrimarySpriteWeapon(string itemID)
    {
        PrimaryBtn.ChangeSpriteButton(itemID);
    }

    [Rpc (RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_ChangeSecondarySpriteWeapon(string itemID)
    {
        SecondaryBtn.ChangeSpriteButton(itemID);
    }

    #endregion

    #region Check Ammo

    public void Reload()
    {
        playerAnimator.SetTrigger("reload");
    }

    public void ResetReload()
    {
        playerAnimator.ResetTrigger("reload");
    }

    #endregion
}
