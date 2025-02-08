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
using UnityEngine.UI;

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
    [SerializeField] private NetworkObject shieldHandHandle;

    [Header("WEAPON EQUIP BUTTONS")]
    [SerializeField] private WeaponEquipBtnController HandBtn;
    [SerializeField] private WeaponEquipBtnController PrimaryBtn;
    [SerializeField] private WeaponEquipBtnController SecondaryBtn;

    [Header("HEAL")]
    [SerializeField] private Image healCountIndicator;
    [SerializeField] private Image repairCountIndicator;

    [field: Header("DEBUGGER NETWORK")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsSkinInitialized { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsWeaponInitialize { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int WeaponIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int TempLastIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairStyle { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int ClothingColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int SkinColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem PrimaryWeapon { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem SecondaryWeapon { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WeaponItem Shield { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject ArrowHolder { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HealCount { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int ArmorRepairCount { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int RifleAmmoCount { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int ArrowAmmoCount { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int TrapCount { get; set; }



    //  =========================

    //private ChangeDetector _changeDetector;

    //  =========================

    public override async void Spawned()
    {
        //_changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        while (!Runner) await Task.Yield();

        if (HasStateAuthority)
        {
            WeaponHandChange();
        }

        if (HasInputAuthority)
        {
            RPC_SendPlayerDataToServer(JsonConvert.SerializeObject(userData.CharacterSetting));
            InitializeSkinOnStart();
        }
        else if (!HasInputAuthority && !HasStateAuthority)
        {
            InitializeSkinOnStart();
        }
    }

    public override void Render()
    {
        if (HasInputAuthority)
        {
            HandBtn.SetIndicator(WeaponIndex == 1 ? true : false);
            PrimaryBtn.SetIndicator(WeaponIndex == 2 ? true : false);
            PrimaryBtn.ChangeSpriteButton(PrimaryWeapon != null ? PrimaryWeapon.WeaponID : null, "");
            SecondaryBtn.SetIndicator(WeaponIndex == 3 ? true : false);
            
            if (SecondaryWeapon != null)
            {
                if (SecondaryWeapon.WeaponID == "003")
                    SecondaryBtn.ChangeSpriteButton(SecondaryWeapon != null ? SecondaryWeapon.WeaponID : null, $"{(SecondaryWeapon != null ? SecondaryWeapon.Ammo : 0)} / {RifleAmmoCount}", SecondaryWeapon != null);
                else if (SecondaryWeapon.WeaponID == "004")
                    SecondaryBtn.ChangeSpriteButton(SecondaryWeapon != null ? SecondaryWeapon.WeaponID : null, $"{(SecondaryWeapon != null ? (SecondaryWeapon.Ammo + ArrowAmmoCount) : 0)}", SecondaryWeapon != null);
            }
            else
                SecondaryBtn.ChangeSpriteButton(null, "", false);

            healCountIndicator.fillAmount = 1 - (float)HealCount / 4;
            repairCountIndicator.fillAmount = 1 - (float)ArmorRepairCount / 4;
        }
    }

    #region Initialize Player Skin

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendPlayerDataToServer(string data)
    {
        var characterSetting = JsonConvert.DeserializeObject<PlayerCharacterSetting>(data);
        HairStyle = characterSetting.hairstyle;
        HairColorIndex = characterSetting.haircolor;
        ClothingColorIndex = characterSetting.clothingcolor;
        SkinColorIndex = characterSetting.skincolor;
        IsSkinInitialized = true;
    }

    private async void InitializeSkinOnStart()
    {
        while (!IsSkinInitialized) await Task.Yield();

        hairStyles[HairStyle].SetActive(true);
        hairMR[HairStyle].material.SetColor("_BaseColor", hairColor[HairColorIndex]);
        upperClothingMR.material.SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        bodyColorMR.material.SetColor("_BaseColor", skinColor[SkinColorIndex]);
    }

    #endregion

    #region Weapon Changer

    public void WeaponHandChange()
    {
        TempLastIndex = WeaponIndex;
        WeaponIndex = 1;

        if (PrimaryWeapon != null)
            PrimaryWeapon.IsHand = false;

        if (SecondaryWeapon != null)
            SecondaryWeapon.IsHand = false;
    }

    public void WeaponPrimaryChange()
    {
        TempLastIndex = WeaponIndex;
        WeaponIndex = 2;
        PrimaryWeapon.IsHand = true;

        if (SecondaryWeapon != null)
            SecondaryWeapon.IsHand = false;
    }

    public void WeaponSecondaryChange()
    {
        TempLastIndex = WeaponIndex;
        WeaponIndex = 3;
        SecondaryWeapon.IsHand = true;

        if (PrimaryWeapon != null)
            PrimaryWeapon.IsHand = false;
    }

    #endregion

    #region HEAL REPAIR

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_DropHealRepair()
    {
        HealCount = 0;
        ArmorRepairCount = 0;
    }

    #endregion
}
