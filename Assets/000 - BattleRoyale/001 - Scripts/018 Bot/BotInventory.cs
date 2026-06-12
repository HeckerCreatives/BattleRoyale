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

    public Transform RifleHand => rifleHand;
    public Transform RifleBack => rifleBack;
    public Transform BowHand => bowHand;
    public Transform BowBack => bowBack;
    public Transform BowStringPullPoint => bowStringPullPoint;

    //  ================

    [Header("COLORS")]
    [SerializeField] private List<Color> hairColorList;
    [SerializeField] private List<Color> clothingColorList;
    [SerializeField] private List<Color> skinColorList;

    [Header("SKINS")]
    [SerializeField] private List<GameObject> hairStyles;
    [SerializeField] private List<SkinnedMeshRenderer> hairMR;
    [SerializeField] private List<SkinnedMeshRenderer> clothingMR;
    [SerializeField] private SkinnedMeshRenderer bodyColorMR;

    [Space]
    [SerializeField] private NetworkObject armorBack;
    [SerializeField] private Transform swordHand;
    [SerializeField] private Transform swordBack;
    [SerializeField] private Transform spearHand;
    [SerializeField] private Transform spearBack;
    [SerializeField] private Transform rifleHand;
    [SerializeField] private Transform rifleBack;
    [SerializeField] private Transform bowHand;
    [SerializeField] private Transform bowBack;
    [Tooltip("Empty Transform parented to the left-hand bone where the bow string is gripped during draw. The equipped bow's BowStringFollower follows this each frame.")]
    [SerializeField] private Transform bowStringPullPoint;

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
    [field: SerializeField][Networked] public int RifleMagazine { get; set; }
    [field: SerializeField][Networked] public int BowMagazine { get; set; }

    private bool _costumeFromMatchData;

    /// <summary>Server: call from onBeforeSpawned so Spawned skips random rolls (matchmaking / debug costumes).</summary>
    public void ApplyCostumeFromMatchData(int hairstyle, int haircolor, int clothingcolor, int skincolor)
    {
        _costumeFromMatchData = true;
        HairStyle = Mathf.Clamp(hairstyle, 0, Mathf.Max(0, hairStyles.Count - 1));
        HairColorIndex = Mathf.Clamp(haircolor, 0, Mathf.Max(0, hairColorList.Count - 1));
        ClothingColorIndex = Mathf.Clamp(clothingcolor, 0, Mathf.Max(0, clothingColorList.Count - 1));
        SkinColorIndex = Mathf.Clamp(skincolor, 0, Mathf.Max(0, skinColorList.Count - 1));
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            if (!_costumeFromMatchData)
            {
                HairStyle = Random.Range(0, hairStyles.Count);
                HairColorIndex = Random.Range(0, hairColorList.Count);
                ClothingColorIndex = Random.Range(0, clothingColorList.Count);
                SkinColorIndex = Random.Range(0, skinColorList.Count);
            }

            HairStyle = Mathf.Clamp(HairStyle, 0, Mathf.Max(0, hairStyles.Count - 1));
            HairColorIndex = Mathf.Clamp(HairColorIndex, 0, Mathf.Max(0, hairColorList.Count - 1));
            ClothingColorIndex = Mathf.Clamp(ClothingColorIndex, 0, Mathf.Max(0, clothingColorList.Count - 1));
            SkinColorIndex = Mathf.Clamp(SkinColorIndex, 0, Mathf.Max(0, skinColorList.Count - 1));
            IsSkinInitialized = true;

            hairStyles[HairStyle].SetActive(true);
            hairMR[HairStyle].material.SetColor("_BaseColor", hairColorList[HairColorIndex]);
            
            for (int a = 0; a < clothingMR.Count; a++)
                clothingMR[a].material.SetColor("_BaseColor", clothingColorList[ClothingColorIndex]);

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

        for (int a = 0; a < clothingMR.Count; a++)
            clothingMR[a].material.SetColor("_BaseColor", clothingColorList[ClothingColorIndex]);

        bodyColorMR.material.SetColor("_BaseColor", skinColorList[SkinColorIndex]);
    }

    public string GetPrimaryWeaponID()
    {
        if (PrimaryWeapon == null) return "";
        return PrimaryWeapon.WeaponID;
    }

    public string GetSecondaryWeaponID()
    {
        if (SecondaryWeapon == null) return "";
        string id = SecondaryWeapon.WeaponID;
        if (string.IsNullOrEmpty(id)) return "";
        id = id.Trim();
        if (int.TryParse(id, out int n))
            return n.ToString("000");
        return id;
    }

    public void SwitchToHands()
    {
        WeaponIndex = 1;
        if (PrimaryWeapon != null) PrimaryWeapon.IsEquipped = false;
        if (SecondaryWeapon != null) SecondaryWeapon.IsEquipped = false;
    }

    public void SwitchToPrimary()
    {
        WeaponIndex = 2;
        if (PrimaryWeapon != null) PrimaryWeapon.IsEquipped = true;
        if (SecondaryWeapon != null) SecondaryWeapon.IsEquipped = false;
    }

    public void SwitchToSecondary()
    {
        WeaponIndex = 3;
        if (SecondaryWeapon != null) SecondaryWeapon.IsEquipped = true;
        if (PrimaryWeapon != null) PrimaryWeapon.IsEquipped = false;
    }

    /// <summary>
    /// Keeps weapon equip booleans consistent with WeaponIndex.
    /// Useful as a safety net when spawned pickups initialize with default equipped states.
    /// </summary>
    public void SyncEquipFlagsToWeaponIndex()
    {
        if (WeaponIndex == 2)
        {
            if (PrimaryWeapon != null) PrimaryWeapon.IsEquipped = true;
            if (SecondaryWeapon != null) SecondaryWeapon.IsEquipped = false;
        }
        else if (WeaponIndex == 3)
        {
            if (SecondaryWeapon != null) SecondaryWeapon.IsEquipped = true;
            if (PrimaryWeapon != null) PrimaryWeapon.IsEquipped = false;
        }
        else
        {
            if (PrimaryWeapon != null) PrimaryWeapon.IsEquipped = false;
            if (SecondaryWeapon != null) SecondaryWeapon.IsEquipped = false;
        }
    }
}
