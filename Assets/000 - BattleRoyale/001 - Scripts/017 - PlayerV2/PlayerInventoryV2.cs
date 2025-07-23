using Fusion;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryV2 : NetworkBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private PlayerOwnObjectEnabler playerOwnObjectEnabler;

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

    [Header("WEAPON EQUIP BUTTONS")]
    [SerializeField] private WeaponEquipBtnController HandBtn;
    [SerializeField] private WeaponEquipBtnController PrimaryBtn;
    [SerializeField] private WeaponEquipBtnController SecondaryBtn;

    [Header("HEAL")]
    [SerializeField] private Image healCountIndicator;
    [SerializeField] private Image repairCountIndicator;

    [Header("TRAP")]
    [SerializeField] private TextMeshProUGUI trapCountTMP;
    [SerializeField] private NetworkObject trapObj;

    [Header("CRATE DETECTOR")]
    [SerializeField] private LayerMask objecMask;
    [SerializeField] private Vector3 colliderOffset;
    [SerializeField] private Vector3 colliderSize;
    [SerializeField] private GameObject itemListObj;
    [SerializeField] private GameObject itemButton;
    [SerializeField] private Transform contentTF;
    [SerializeField] private WeaponSpawnData itemSpritesData;

    [field: Header("ITEM HIDE IN PLAYER")]
    [field: SerializeField][Networked] public NetworkObject ArmorHand { get; set; }
    [field: SerializeField][Networked] public NetworkObject SwordHand { get; set; }
    [field: SerializeField][Networked] public NetworkObject SwordBack { get; set; }

    [field: Header("DEBUGGER")]
    [field: SerializeField] public CrateController CrateObject { get; set; }
    [field: SerializeField] public GameObject CrateObjectDetector { get; set; }
    [field: SerializeField] private List<GameObject> localNearbyWeapon = new List<GameObject>();
    [field: SerializeField] private bool isCacheUpdated = false;

    [field: Header("DEBUGGER NETWORK")]
    [field: SerializeField][Networked] public NetworkBool IsSkinInitialized { get; set; }
    [field: SerializeField][Networked] public NetworkBool IsWeaponInitialize { get; set; }
    [field: SerializeField][Networked] public int HairStyle { get; set; }
    [field: SerializeField][Networked] public int HairColorIndex { get; set; }
    [field: SerializeField][Networked] public int ClothingColorIndex { get; set; }
    [field: SerializeField][Networked] public int SkinColorIndex { get; set; }
    [field: SerializeField][Networked] public int WeaponIndex { get; set; }
    [field: SerializeField][Networked] public int HealCount { get; set; }
    [field: SerializeField][Networked] public int ArmorRepairCount { get; set; }
    [field: SerializeField][Networked] public int TrapCount { get; set; }
    [field: SerializeField][Networked] public PrimaryWeaponItem PrimaryWeapon { get; set; }
    [field: SerializeField][Networked] public ArmorItem Armor { get; set; }


    //  ========================

    private List<Collider> currentColliders = new List<Collider>();
    private List<Collider> previousColliders = new List<Collider>();

    private Dictionary<string, int> _localCache = new Dictionary<string, int>();

    private ChangeDetector _changeDetector;

    //  =========================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
        {
            RPC_SendPlayerDataToServer(JsonConvert.SerializeObject(userData.CharacterSetting));
            InitializeSkinOnStart();
        }
        else if (!HasInputAuthority && !HasStateAuthority)
            InitializeSkinOnStart();
    }

    public override void Render()
    {
        if (HasInputAuthority)
        {
            CrateCache();

            HandBtn.SetIndicator(WeaponIndex == 1 ? true : false);
            PrimaryBtn.SetIndicator(WeaponIndex == 2 ? true : false);
            SecondaryBtn.SetIndicator(WeaponIndex == 3 ? true : false);
            healCountIndicator.fillAmount = 1 - (float)HealCount / 4;
            repairCountIndicator.fillAmount = 1 - (float)ArmorRepairCount / 4;
            trapCountTMP.text = $"{TrapCount} / 4";

            if (PrimaryWeapon != null)
                PrimaryBtn.ChangeSpriteButton(PrimaryWeapon.WeaponID, PrimaryWeapon.Supplies.ToString(), false);
            else PrimaryBtn.ResetUI();
        }
    }

    private void FixedUpdate()
    {
        CrateCollisionChecker();
    }

    

    #region SKINS

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendPlayerDataToServer(string data)
    {
        var characterSetting = JsonConvert.DeserializeObject<PlayerCharacterSetting>(data);
        HairStyle = characterSetting.hairstyle;
        HairColorIndex = characterSetting.haircolor;
        ClothingColorIndex = characterSetting.clothingcolor;
        SkinColorIndex = characterSetting.skincolor;
        IsSkinInitialized = true;


        hairStyles[HairStyle].SetActive(true);
        hairMR[HairStyle].material.SetColor("_BaseColor", hairColor[HairColorIndex]);
        upperClothingMR.material.SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColor[ClothingColorIndex]);
        bodyColorMR.material.SetColor("_BaseColor", skinColor[SkinColorIndex]);

        WeaponIndex = 1;
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

    #region INVENTORY

    public void SpawnTrap()
    {
        TrapCount -= 1;

        if (!HasStateAuthority) return;

        Runner.Spawn(trapObj, transform.position, Quaternion.identity, Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) => {
            obj.GetComponent<TrapWeaponController>().Initialize(playerOwnObjectEnabler.Username, Vector3.zero);
        });
    }

    public void SwitchToHands()
    {
        WeaponIndex = 1;

        if (PrimaryWeapon != null) PrimaryWeapon.IsEquipped = false;
    }

    public void SwitchToPrimary()
    {
        if (PrimaryWeapon == null) return;

        if (PrimaryWeapon.IsEquipped) return;

        WeaponIndex = 2;

        PrimaryWeapon.IsEquipped = true;
    }

    #endregion

    #region CRATE ITEMS DETECTOR

    private void CrateCache()
    {
        if (CrateObject == null && localNearbyWeapon.Count <= 0)
        {
            itemListObj.SetActive(false); // Hide the UI
            ClearLocalCacheAndUI();      // Clear cache and UI
            return;
        }

        // Check if the crate's contents or nearby weapons have changed

        if (CrateObject != null && !CheckItemCrateDictionary(CrateObject.Weapons, _localCache))
        {
            isCacheUpdated = true;
        }

        if (!CheckWeaponList())
        {
            isCacheUpdated = true;
        }

        // Update the cache and UI if changes are detected
        if (isCacheUpdated)
        {
            UpdateLocalCache();
        }

        // Show the UI
        itemListObj.SetActive(true);
    }

    private void ClearLocalCacheAndUI()
    {
        _localCache.Clear();
        foreach (Transform child in contentTF)
        {
            Destroy(child.gameObject);
        }
    }

    private bool CheckItemCrateDictionary(NetworkDictionary<NetworkString<_4>, int> dict1, Dictionary<string, int> dict2)
    {
        if (dict1.Count != dict2.Count) return false;

        foreach (var pair in dict1)
        {
            if (!dict2.TryGetValue(pair.Key.ToString(), out var value) || !value.Equals(pair.Value))
                return false;
        }

        return true;
    }

    private bool CheckWeaponList()
    {
        var itemsToRemove = new List<GameObject>();

        foreach (var item in localNearbyWeapon)
        {
            var weaponItem = item.GetComponent<IPickupItem>();

            if (weaponItem != null && weaponItem.IsPickedUp)
                itemsToRemove.Add(item);
        }

        // Remove items outside the loop
        foreach (var item in itemsToRemove)
        {
            localNearbyWeapon.Remove(item);
        }

        // Return true if no items were removed, false otherwise
        return itemsToRemove.Count == 0;
    }

    private void UpdateLocalCache()
    {
        if (!HasInputAuthority) return;

        // Clear current UI only when necessary
        foreach (Transform child in contentTF)
        {
            Destroy(child.gameObject);
        }

        _localCache.Clear();

        // Update crate items
        if (CrateObject != null)
        {
            foreach (var pair in CrateObject.Weapons)
            {
                _localCache[pair.Key.ToString()] = pair.Value;
                AddItemButton(pair.Key.ToString(), pair.Value, isCrateItem: true);
            }
        }

        // Update nearby weapons
        foreach (var weapon in localNearbyWeapon)
        {
            var weaponItem = weapon.GetComponent<IPickupItem>();
            if (weaponItem == null) continue;

            AddItemButton(weaponItem.WeaponID, weaponItem.Supplies, isCrateItem: false);
        }

        isCacheUpdated = false;
    }

    private void AddItemButton(string itemID, int value, bool isCrateItem)
    {
        GameObject tempbuttons = Instantiate(itemButton, contentTF);

        tempbuttons.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (isCrateItem)
            {
                CrateObject.RPC_RemoveItem(itemID, this);

                return;
            }

            var weapon = localNearbyWeapon.FirstOrDefault(w => w.GetComponent<IPickupItem>()?.WeaponID == itemID);

            if (weapon == null) return;

            NetworkObject tempobject = Object;

            if (itemID == "001")
            {
                PrimaryWeaponItem tempweapon = weapon.GetComponent<PrimaryWeaponItem>();

                tempweapon.RPC_PickupPrimaryWeapon(tempobject, SwordBack, SwordHand);
            }

            else if (itemID == "007")
            {
                ArmorItem temparmor = weapon.GetComponent<ArmorItem>();

                temparmor.RPC_PickupArmor(tempobject, ArmorHand);
            }
        });

        tempbuttons.GetComponent<WeaponListUIController>().InitializeData(
            itemSpritesData.GetItemListSprite(itemID),
            itemSpritesData.GetItemName(itemID),
            value.ToString());
    }

    private void CrateCollisionChecker()
    {
        if (Runner == null) return;

        if (!HasInputAuthority) return;

        CollisionEnter();
        CollisionExit();
    }

    private void CollisionEnter()
    {
        if (!HasInputAuthority) return;

        Collider[] hitColliders = Physics.OverlapBox(transform.position + colliderOffset, colliderSize, Quaternion.identity, objecMask, QueryTriggerInteraction.Collide);

        currentColliders.Clear();
        currentColliders.AddRange(hitColliders);

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Crate"))
            {
                HandleCrateEnter(hit.gameObject);
            }
            else if (hit.CompareTag("Weapon"))
            {
                HandleWeaponEnter(hit.gameObject);
            }
        }
    }

    private void CollisionExit()
    {
        if (!HasInputAuthority) return;

        foreach (var previousCollider in previousColliders)
        {
            if (!currentColliders.Contains(previousCollider))
            {
                if (previousCollider == null) continue;

                if (previousCollider.CompareTag("Crate"))
                {
                    HandleCrateExit();
                }
                else if (previousCollider.CompareTag("Weapon"))
                {
                    HandleWeaponExit(previousCollider.gameObject);
                }
            }
        }

        previousColliders.Clear();
        previousColliders.AddRange(currentColliders);
    }

    private void HandleWeaponEnter(GameObject weapon)
    {
        if (localNearbyWeapon.Contains(weapon)) return;

        if (weapon.GetComponent<IPickupItem>().IsPickedUp) return;

        localNearbyWeapon.Add(weapon);
        isCacheUpdated = true; // Mark cache updated if a new weapon is detected
    }

    private void HandleCrateEnter(GameObject other)
    {
        if (!HasInputAuthority) return;

        CrateObject = other.GetComponent<CrateController>();
    }

    private void HandleWeaponExit(GameObject weapon)
    {
        localNearbyWeapon.Remove(weapon);
    }

    private void HandleCrateExit()
    {
        if (!HasInputAuthority) return;

        if (CrateObject == null) return;

        CrateObject = null;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + colliderOffset, colliderSize);
    }

    #endregion

}

public interface IPickupItem
{
    string WeaponID { get; }
    string WeaponName { get; }
    bool IsPickedUp { get; }
    int Supplies { get; }
}