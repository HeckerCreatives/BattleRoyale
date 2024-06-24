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

    [Header("WEAPON HAND HANDLE")]
    [SerializeField] private NetworkObject swordHandHandle;

    [Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject DedicatedServer { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsSkinReady { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int WeaponIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int TempLastIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairStyle { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int ClothingColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int SkinColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem PrimaryWeapon { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem SecondaryWeapon { get; set; }

    async private void Awake()
    {
        while (!Runner) await Task.Delay(1000);

        if (!HasInputAuthority && !HasStateAuthority)
        {
            while (!IsSkinReady) await Task.Delay(1000);

            ApplySkins();
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        if (HasInputAuthority)
        {
            RPC_SendPlayerDataToServer(JsonConvert.SerializeObject(userData.CharacterSetting));
        }
        //InitializeItemsInventory();
    }

    #region INITIALIZE PLAYER SKIN

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendPlayerDataToServer(string data)
    {
        Debug.Log($"Send player to server: {data}");
        RPC_BroadcastPlayerSkin(data);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPlayerSkin(string data)
    {
        Debug.Log($"send to all player: {data}");
        PlayerCharacterSetting characterSetting = JsonConvert.DeserializeObject<PlayerCharacterSetting>(data);
        HairStyle = characterSetting.hairstyle;
        HairColorIndex = characterSetting.haircolor;
        ClothingColorIndex = characterSetting.clothingcolor;
        SkinColorIndex = characterSetting.skincolor;
        IsSkinReady = true;
        ApplySkins();
    }

    private void ApplySkins()
    {
        hairStyles[HairStyle].SetActive(true);
        hairMR[HairStyle].material.SetColor("_BaseColor", hairColor[HairColorIndex]);
        upperClothingMR.material.SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        lowerClothingMR.materials[1].SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        bodyColorMR.material.SetColor("_BaseColor", skinColor[SkinColorIndex]);
    }

    #endregion

    #region PICKUP WEAPON

    [Rpc (RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SpawnWeaponForPlayer(string data, PlayerRef player)
    {
        TempItemWeaponData weapondata = JsonConvert.DeserializeObject<TempItemWeaponData>(data);

        var weaponObject = Runner.Spawn(weaponSpawnData.GetItemObject(weapondata.itemID), Vector3.zero, Quaternion.identity, inputAuthority: player, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {
            obj.GetComponent<WeaponItem>().InitializeItem(weaponSpawnData.GetItemName(weapondata.itemID), weapondata.itemID, weaponSpawnData.GetItemAnimatorIndex(weapondata.itemID), obj, swordHandle, swordHandHandle, weapondata.ammo);

            if (weapondata.itemID == "001" || weapondata.itemID == "002")
            {
                PrimaryWeapon = obj.GetComponent<WeaponItem>();
            }
        });
    }

    #endregion

    #region WEAPON CHANGER

    public void WeaponHandChange()
    {
        TempLastIndex = WeaponIndex;

        WeaponIndex = 1;

        if (PrimaryWeapon != null)
            PrimaryWeapon.Rpc_SheatWeapon();
    }

    public void WeaponPrimaryChange()
    {
        TempLastIndex = WeaponIndex;

        WeaponIndex = PrimaryWeapon.AnimatorID;
        PrimaryWeapon.Rpc_ActivateWeapon();
    }

    public void WeaponSecondaryChange()
    {
        TempLastIndex = WeaponIndex;

        WeaponIndex = 5;
    }

    #endregion

    #region CHECK AMMO

    public void Reload()
    {
        playerAnimator.SetTrigger("reload");
    }

    public void ResetReload()
    {
        playerAnimator.ResetTrigger("reload");
    }

    #endregion

    #region OLD CODE

    //private void AnimateSwitch()
    //{
    //    animator.SetLayerWeight(tempLastIndex, Mathf.Lerp(animator.GetLayerWeight(tempLastIndex), 0f, Time.deltaTime * 13f));
    //    animator.SetLayerWeight(weaponIndex, Mathf.Lerp(animator.GetLayerWeight(weaponIndex), 1f, Time.deltaTime * 13f));
    //}

    //private void SetHandsItem()
    //{
    //    if (!Controller.SwitchHands) return;

    //    if (weaponIndex == 1) return;

    //    tempLastIndex = weaponIndex;
    //    weaponIndex = 1;

    //    animator.SetTrigger("switchweapon");

    //    Controller.SwitchHandsStop();
    //}

    //private void SetPrimaryItem()
    //{
    //    if (!Controller.SwitchPrimary) return;

    //    if (weaponIndex == 2) return;

    //    tempLastIndex = weaponIndex;
    //    weaponIndex = 2;

    //    animator.SetTrigger("switchweapon");

    //    Controller.SwitchPrimaryStop();
    //}

    //public void ResetTriggerSwitch() => animator.ResetTrigger("switchweapon");

    #endregion
}
