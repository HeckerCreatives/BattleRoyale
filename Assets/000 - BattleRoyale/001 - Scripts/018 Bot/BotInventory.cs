using Fusion;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public class BotInventory : NetworkBehaviour
{
    public NetworkObject ArmorBack
    {
        get => armorBack;
    }

    public Transform SwordHand
    {
        get => swordHand;
    }

    public Transform SwordBack
    {
        get => swordBack;
    }

    public Transform SpearHand
    {
        get => spearHand;
    }

    public Transform SpearBack
    {
        get => spearBack;
    }

    //  ================

    [Header("COLORS")]
    [SerializeField] private List<Color> hairColorList;
    [SerializeField] private List<Color> clothingColorList;
    [SerializeField] private List<Color> skinColorList;

    [Header("SKINS")]
    [SerializeField] private List<GameObject> hairStyles;
    [SerializeField] private List<MeshRenderer> hairMR;
    [SerializeField] private SkinnedMeshRenderer bodyColorMR;
    [SerializeField] private SkinnedMeshRenderer upperClothingMR;
    [SerializeField] private SkinnedMeshRenderer lowerClothingMR;

    [Space]
    [SerializeField] private NetworkObject armorBack;
    [SerializeField] private Transform swordHand;
    [SerializeField] private Transform swordBack;
    [SerializeField] private Transform spearHand;
    [SerializeField] private Transform spearBack;

    [field: Header("DEBUGGER NETWORK")]
    [field: SerializeField][Networked] public NetworkBool IsSkinInitialized { get; set; }
    [field: SerializeField][Networked] public int HairStyle { get; set; }
    [field: SerializeField][Networked] public int HairColorIndex { get; set; }
    [field: SerializeField][Networked] public int ClothingColorIndex { get; set; }
    [field: SerializeField][Networked] public int SkinColorIndex { get; set; }
    [field: SerializeField][Networked] public int WeaponIndex { get; set; }
    [field: SerializeField][Networked] public PrimaryWeaponItem PrimaryWeapon { get; set; }
    [field: SerializeField][Networked] public SecondaryWeaponItem SecondaryWeapon { get; set; }
    [field: SerializeField][Networked] public ArmorItem Armor { get; set; }
    [field: SerializeField][Networked] public int HealCount { get; set; }
    [field: SerializeField][Networked] public int RepairCount { get; set; }
    [field: SerializeField][Networked] public int TrapCount { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            HairStyle = Random.Range(0, hairStyles.Count);
            HairColorIndex = Random.Range(0, hairColorList.Count);
            ClothingColorIndex = Random.Range(0, clothingColorList.Count);
            SkinColorIndex = Random.Range(0, skinColorList.Count);
            IsSkinInitialized = true;

            hairStyles[HairStyle].SetActive(true);
            hairMR[HairStyle].material.SetColor("_BaseColor", hairColorList[HairColorIndex]);
            upperClothingMR.material.SetColor("_BaseColor", clothingColorList[ClothingColorIndex]);
            lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColorList[ClothingColorIndex]);
            bodyColorMR.material.SetColor("_BaseColor", skinColorList[SkinColorIndex]);
        }
        else if (!HasInputAuthority && !HasStateAuthority)
            InitializeSkinOnStart();
    }

    private async void InitializeSkinOnStart()
    {
        while (!IsSkinInitialized) await Task.Yield();

        hairStyles[HairStyle].SetActive(true);
        hairMR[HairStyle].material.SetColor("_BaseColor", hairColorList[HairColorIndex]);
        upperClothingMR.material.SetColor("_BaseColor", clothingColorList[ClothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColorList[ClothingColorIndex]);
        bodyColorMR.material.SetColor("_BaseColor", skinColorList[SkinColorIndex]);
    }

    public string GetPrimaryWeaponID()
    {
        if (PrimaryWeapon == null) return "";

        return PrimaryWeapon.WeaponID;
    }
}
