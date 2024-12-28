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

    [field: Header("DEBUGGER NETWORK")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsSkinInitialized { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsWeaponInitialize { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int WeaponIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int TempLastIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairStyle { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int ClothingColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int SkinColorIndex { get; set; }

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
            while (!IsSkinInitialized) await Task.Delay(100);

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
                    break;
                case nameof(IsWeaponInitialize):
                    if (!HasInputAuthority) break;

                    while (!HandBtn.gameObject.activeInHierarchy && !PrimaryBtn.gameObject.activeInHierarchy && !SecondaryBtn.gameObject.activeInHierarchy) await Task.Delay(100);

                    HandBtn.SetIndicator(WeaponIndex == 1 ? true : false);
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
        bodyColorMR.material.SetColor("_BaseColor", skinColor[SkinColorIndex]);
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
        TempLastIndex = WeaponIndex;
        WeaponIndex = 1;
    }

    #endregion
}
