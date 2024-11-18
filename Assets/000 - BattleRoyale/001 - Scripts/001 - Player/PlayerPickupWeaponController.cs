using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPickupWeaponController : NetworkBehaviour
{

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private WeaponSpawnData itemSpritesData;
    [SerializeField] private GameObject itemButton;
    [SerializeField] private LayerMask objecMask;
    [SerializeField] private float itemDetectorRadius;

    [field: Space]
    [field: SerializeField] private GameObject PickupItemBtn { get; set; }
    [field: SerializeField] private GameObject PickupItemList { get; set; }
    [SerializeField] private Transform contentTF;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField] public NetworkObject CrateObject { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public GameObject CrateObjectDetector { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public List<GameObject> CrateObjectButtonList { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public List<NetworkObject> WeaponsPickup { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public List<GameObject> WeaponsPickupDetector { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public Dictionary<GameObject, GameObject> WeaponsPickupList { get; set; } = new Dictionary<GameObject, GameObject>();

    //  ========================

    Coroutine initialize;
    Coroutine destroy;

    private List<Collider> currentColliders = new List<Collider>();
    private List<Collider> previousColliders = new List<Collider>();

    //  ========================

    async public override void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        if (!HasInputAuthority) return;

        PickupItemBtn.SetActive(false);
        PickupItemList.SetActive(false);
    }

    private void Update()
    {
        CollisionEnter();
        CollisionExit();
    }
    private void CollisionEnter()
    {
        if (!HasInputAuthority) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, itemDetectorRadius, objecMask);

        currentColliders.Clear();
        currentColliders.AddRange(hitColliders);

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Crate"))
            {
                if (CrateObjectDetector != hit.gameObject)
                {
                    CrateObjectDetector = hit.gameObject;
                    HandleCrateEnter(CrateObjectDetector);
                }
            }
            else if (hit.CompareTag("Weapon"))
            {
                if (!WeaponsPickupDetector.Contains(hit.gameObject))
                {
                    WeaponsPickupDetector.Add(hit.gameObject);
                    HandleWeaponEnter(hit.gameObject);  
                }
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
                    CrateObjectDetector = null;
                    HandleCrateExit();
                }
                else if (previousCollider.CompareTag("Weapon"))
                {
                    WeaponsPickupDetector.Remove(previousCollider.gameObject);
                    HandleWeaponExit(previousCollider.gameObject);
                }
            }
        }

        // Update the previousColliders list to the current frame's colliders
        previousColliders.Clear();
        previousColliders.AddRange(currentColliders);
    }

    private void HandleCrateEnter(GameObject other)
    {
        if (!HasInputAuthority && !HasStateAuthority) return;

        if (PickupItemBtn == null || PickupItemList == null) return;

        CrateObject = other.GetComponent<NetworkObject>();
        CrateObject.GetComponent<CrateController>().OnItemListChange += ItemListChange;

        if (destroy != null) StopCoroutine(destroy);
        initialize = StartCoroutine(SpawnListItems(other.gameObject.GetComponent<CrateController>().Weapons));
    }

    private void HandleWeaponEnter(GameObject other)
    {
        if (!HasInputAuthority) return;

        if (other.GetComponent<WeaponItem>().IsPickedUp) return;

        var weaponNetworkObject = other.GetComponent<NetworkObject>();
        if (WeaponsPickup.Contains(weaponNetworkObject)) return;

        WeaponsPickup.Add(weaponNetworkObject);

        var item = Instantiate(itemButton, contentTF);
        WeaponsPickupList.Add(other, item);

        var weaponItem = other.GetComponent<WeaponItem>();
        item.GetComponent<WeaponListUIController>().InitializeData(
            itemSpritesData.GetItemListSprite(weaponItem.WeaponID),
            itemSpritesData.GetItemName(weaponItem.WeaponID),
            weaponItem.Ammo.ToString(),
            weaponNetworkObject);

        weaponItem.OnWeaponPickupChange += WeaponPickupChange;

        item.GetComponent<Button>().onClick.AddListener(() =>
        {
            weaponItem.Rpc_SendPickupEvent(item.GetComponent<WeaponListUIController>().Index, other.GetComponent<NetworkObject>().Id);
            playerInventory.Rpc_ReassignWeaponToPlayer(weaponItem.Object.Id, Runner.LocalPlayer);
        });

        if (CrateObject == null)
        {
            PickupItemBtn.SetActive(true);
            PickupItemList.SetActive(true);
        }
    }

    private void HandleCrateExit()
    {
        if (!HasInputAuthority && !HasStateAuthority) return;

        if (PickupItemBtn == null || PickupItemList == null) return;

        if (CrateObject == null) return;

        CrateObject.GetComponent<CrateController>().OnItemListChange -= ItemListChange;
        CrateObject = null;

        if (WeaponsPickup.Count <= 0)
        {
            PickupItemBtn.SetActive(false);
            PickupItemList.SetActive(false);
        }

        if (initialize != null) StopCoroutine(initialize);
        destroy = StartCoroutine(DestoryListItems());
    }

    private void HandleWeaponExit(GameObject other)
    {
        if (!HasInputAuthority) return;

        var weaponNetworkObject = other.gameObject.GetComponent<NetworkObject>();

        if (!WeaponsPickup.Contains(weaponNetworkObject)) return;

        if (!WeaponsPickupList.ContainsKey(other))
        {
            WeaponsPickup.Remove(weaponNetworkObject);

            if (CrateObject == null)
            {
                PickupItemBtn.SetActive(false);
                PickupItemList.SetActive(false);
            }

            return;
        }

        var itemToDestroy = WeaponsPickupList[other];

        if (itemToDestroy != null)
        {
            WeaponsPickupList.Remove(other);
            itemToDestroy.GetComponent<Button>().onClick.RemoveAllListeners();
            Destroy(itemToDestroy);
        }

        WeaponsPickup.Remove(weaponNetworkObject);

        if (CrateObject == null)
        {
            PickupItemBtn.SetActive(false);
            PickupItemList.SetActive(false);
        }
    }

    private void ItemListChange(object sender, ItemListChangeEventArgs e)
    {
        if (!HasInputAuthority) return;
        Destroy(contentTF.GetChild(e.Index).gameObject);
    }

    private void WeaponPickupChange(object sender, WeaponItemListChangeEventArgs e)
    {
        if (!HasInputAuthority) return;

        if (WeaponsPickupDetector.Contains(contentTF.GetChild(e.Index).gameObject))
            WeaponsPickupDetector.Remove(contentTF.GetChild(e.Index).gameObject);

        if (Runner.TryFindObject(e.Objectid, out NetworkObject tempWeapon))
        {
            if (WeaponsPickupList.ContainsKey(tempWeapon.gameObject))
                WeaponsPickupList.Remove(tempWeapon.gameObject);
        }

        Destroy(contentTF.GetChild(e.Index).gameObject);
    }

    private IEnumerator SpawnListItems(NetworkDictionary<NetworkString<_4>, int> data)
    {
        yield return ClearContentTF();

        if (data.Count <= 0) yield break;

        foreach (var item in data)
        {
            var newItem = Instantiate(itemButton, contentTF);

            CrateObjectButtonList.Add(newItem);

            newItem.GetComponent<WeaponListUIController>().InitializeData(
                itemSpritesData.GetItemListSprite(item.Key.ToString()),
                itemSpritesData.GetItemName(item.Key.ToString()),
                item.Value.ToString());

            newItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                var index = newItem.GetComponent<WeaponListUIController>().Index;
                var weaponData = new TempItemWeaponData
                {
                    index = index,
                    itemID = item.Key.ToString(),
                    ammo = item.Value,
                    objectId = CrateObject.Id
                };
                CrateObjectButtonList.Remove(newItem);
                playerInventory.Rpc_SpawnWeaponForPlayer(JsonConvert.SerializeObject(weaponData), Runner.LocalPlayer);
                Rpc_RemoveWeaponFromList(JsonConvert.SerializeObject(weaponData));
            });

            yield return null;
        }

        PickupItemBtn.SetActive(true);
        PickupItemList.SetActive(true);
        initialize = null;
    }

    private IEnumerator DestoryListItems()
    {
        yield return ClearContentTF();
    }

    private IEnumerator ClearContentTF()
    {
        while (CrateObjectButtonList.Count > 0)
        {
            for (int i = 0; i < CrateObjectButtonList.Count; i++)
            {
                var child = CrateObjectButtonList[i];
                CrateObjectButtonList.Remove(child);
                child.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(child.gameObject);
                yield return null;
            }
            yield return null;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RemoveWeaponFromList(string data)
    {
        TempItemWeaponData weapondata = JsonConvert.DeserializeObject<TempItemWeaponData>(data);

        if (Runner.TryFindObject(weapondata.objectId, out NetworkObject crateObj))
            crateObj.GetComponent<CrateController>().Rpc_RemoveItemFromObject(data);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, itemDetectorRadius);
        Gizmos.color = Color.blue;
    }
}

[System.Serializable]
public class TempItemWeaponData
{
    public int index;
    public string itemID;
    public int ammo;
    public NetworkId objectId;
}
