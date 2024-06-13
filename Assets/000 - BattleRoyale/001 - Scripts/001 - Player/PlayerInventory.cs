using Fusion;
using MyBox;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    //  ==========================

    [SerializeField] private UserData userData;
    [SerializeField] private Animator animator;

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

    [Space]
    [SerializeField] private Transform primaryWeaponSlot;
    [SerializeField] private Transform secondaryWeaponSlot;
    [SerializeField] private Transform trapWeaponSlot;
    [SerializeField] private List<GameObject> primaryWeapons;
    [SerializeField] private List<GameObject> secondaryWeapons;
    [SerializeField] private List<GameObject> trapWeapons;

    [Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject DedicatedServer { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int WeaponIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int TempLastIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairStyle { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int HairColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int ClothingColorIndex { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int SkinColorIndex { get; set; }

    private ChangeDetector _changeDetector;

    //private void Update()
    //{
    //    SetHandsItem();
    //    SetPrimaryItem();
    //    AnimateSwitch();
    //}

    async private void Awake()
    {
        while (!Runner) await Task.Delay(1000);

        if (!HasInputAuthority && !HasStateAuthority) ApplySkins();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (HasInputAuthority)
        {
            RPC_SendPlayerDataToServer(JsonConvert.SerializeObject(userData.CharacterSetting));
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendPlayerDataToServer(string data)
    {
        Debug.Log($"Send player to server: {data}");
        RPC_BroadcastPlayerColor(data);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPlayerColor(string data)
    {
        Debug.Log($"send to all player: {data}");
        PlayerCharacterSetting characterSetting = JsonConvert.DeserializeObject<PlayerCharacterSetting>(data);
        HairStyle = characterSetting.hairstyle;
        HairColorIndex = characterSetting.haircolor;
        ClothingColorIndex = characterSetting.clothingcolor;
        SkinColorIndex = characterSetting.skincolor;
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
}
