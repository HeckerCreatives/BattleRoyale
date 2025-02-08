using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItemCheckerController : NetworkBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private WeaponSpawnData itemSpritesData;

    [Space]
    [SerializeField] private LayerMask objecMask;
    [SerializeField] private Vector3 colliderOffset;
    [SerializeField] private Vector3 colliderSize;

    [Space]
    [SerializeField] private GameObject itemListObj;
    [SerializeField] private GameObject itemButton;
    [SerializeField] private Transform contentTF;

    [Header("WEAPON BACK HANDLE")]
    [SerializeField] private NetworkObject swordBackHandle;
    [SerializeField] private NetworkObject spearBackHandle;
    [SerializeField] private NetworkObject rifleBackHandle;
    [SerializeField] private NetworkObject bowBackHandle;
    [SerializeField] private NetworkObject arrowBackHandle;

    [Header("WEAPON HAND HANDLE")]
    [SerializeField] private NetworkObject swordHandHandle;
    [SerializeField] private NetworkObject spearHandHandle;
    [SerializeField] private NetworkObject rifleHandHandle;
    [SerializeField] private NetworkObject bowHandHandle;
    [SerializeField] private NetworkObject shieldHandHandle;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField] public CrateController CrateObject { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public GameObject CrateObjectDetector { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] private List<GameObject> localNearbyWeapon = new List<GameObject>();
        bool isCacheUpdated = false;

    //  ========================

    private List<Collider> currentColliders = new List<Collider>();
    private List<Collider> previousColliders = new List<Collider>();

    private Dictionary<string, int> _localCache = new Dictionary<string, int>();

    //  =========================

    private void Update()
    {
        if (!HasInputAuthority) return;

        // Check if no crate or nearby weapons are detected
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


    private void FixedUpdate()
    {
        CollisionEnter();
        CollisionExit();
    }

    private void ClearLocalCacheAndUI()
    {
        _localCache.Clear();
        foreach (Transform child in contentTF)
        {
            Destroy(child.gameObject);
        }
    }

    private void CollisionEnter()
    {
        if (!HasInputAuthority) return;

        Collider[] hitColliders = Physics.OverlapBox(transform.position + colliderOffset, colliderSize, Quaternion.identity, objecMask);

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

        if (weapon.GetComponent<WeaponItem>().IsPickedUp) return;

        localNearbyWeapon.Add(weapon);
        isCacheUpdated = true; // Mark cache updated if a new weapon is detected
    }

    private void HandleWeaponExit(GameObject weapon)
    {
        localNearbyWeapon.Remove(weapon);
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
            var weaponItem = item.GetComponent<WeaponItem>();
            if (weaponItem != null && weaponItem.IsPickedUp)
            {
                itemsToRemove.Add(item);
            }
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
            var weaponItem = weapon.GetComponent<WeaponItem>();
            if (weaponItem == null) continue;

            AddItemButton(weaponItem.WeaponID, weaponItem.Ammo, isCrateItem: false);
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
                CrateObject.RPC_RemoveItem(itemID, itemSpritesData.GetItemName(itemID), Object, Back(itemID), Hand(itemID), arrowBackHandle);
            }
            else
            {
                var weapon = localNearbyWeapon.FirstOrDefault(w => w.GetComponent<WeaponItem>()?.WeaponID == itemID);

                if (weapon == null) return;

                WeaponItem tempWeaponItem = weapon?.GetComponent<WeaponItem>();

                if (tempWeaponItem.IsPickedUp) return;

                if (itemID == "001" || itemID == "002")
                {
                    tempWeaponItem?.RPC_RepickupPrimaryWeapon(Object, Back(itemID), Hand(itemID), playerInventory.WeaponIndex == 1 || playerInventory.WeaponIndex == 2);
                }
                else if (itemID == "007")
                {
                    tempWeaponItem?.RPC_RepickupArmor(Object, Back(itemID), Hand(itemID));
                }
                else if (itemID == "003")
                {
                    tempWeaponItem?.RPC_RepickupSecondaryWeapon(Object, Back(itemID), Hand(itemID), playerInventory.WeaponIndex == 1 || playerInventory.WeaponIndex == 3);
                }
                else if (itemID == "004")
                {
                    tempWeaponItem?.RPC_RepickupSecondaryWithAmmoCaseWeapon(Object, Back(itemID), Hand(itemID), playerInventory.WeaponIndex == 1 || playerInventory.WeaponIndex == 3);
                }
            }
        });

        tempbuttons.GetComponent<WeaponListUIController>().InitializeData(
            itemSpritesData.GetItemListSprite(itemID),
            itemSpritesData.GetItemName(itemID),
            value.ToString());
    }

    private NetworkObject Hand(string id)
    {
        switch (id)
        {
            case "001":
                return swordHandHandle;
            case "002":
                return spearHandHandle;
            case "003":
                return rifleHandHandle;
            case "004":
                return bowHandHandle;
            case "007":
                return shieldHandHandle;
            default:
                return null;
        }
    }

    private NetworkObject Back(string id)
    {
        switch (id)
        {
            case "001":
                return swordBackHandle;
            case "002":
                return spearBackHandle;
            case "003":
                return rifleBackHandle;
            case "004":
                return bowBackHandle;
            case "007":
                return shieldHandHandle;
            default:
                return null;
        }
    }

    private void HandleCrateEnter(GameObject other)
    {
        if (!HasInputAuthority) return;

        CrateObject = other.GetComponent<CrateController>();
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
}
