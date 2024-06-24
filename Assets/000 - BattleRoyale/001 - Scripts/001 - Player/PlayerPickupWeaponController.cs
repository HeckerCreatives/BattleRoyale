using Fusion;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPickupWeaponController : NetworkBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private WeaponSpawnData itemSpritesData;
    [SerializeField] private GameObject itemButton;

    [field: Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private Transform contentTF;
    [field: MyBox.ReadOnly][field: SerializeField] public GameObject PickupItemBtn { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public GameObject PickupItemList { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public NetworkObject CrateObject { get; set; }

    //  ========================

    Coroutine initialize;
    Coroutine destroy;

    //  ========================

    public void InitializeContentTF()
    {
        contentTF = PickupItemList.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Crate")
        {
            if (!HasInputAuthority && !HasStateAuthority) return;

            CrateObject = other.GetComponent<NetworkObject>();
            CrateObject.GetComponent<CrateController>().OnItemListChange += ItemListChange;

            if (PickupItemBtn == null && PickupItemList == null) return;

            if (destroy != null) StopCoroutine(destroy);

            initialize = StartCoroutine(SpawnListItems(other.GetComponent<CrateController>().Weapons));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Crate")
        {
            if (!HasInputAuthority && !HasStateAuthority) return;

            CrateObject.GetComponent<CrateController>().OnItemListChange -= ItemListChange;
            CrateObject = null;

            if (PickupItemBtn == null && PickupItemList == null) return;

            CrateObject = null;

            PickupItemBtn.SetActive(false);
            PickupItemList.SetActive(false);

            if (initialize != null) StopCoroutine(initialize);

            destroy = StartCoroutine(DestoryListItems());
        }
    }

    private void ItemListChange(object sender, ItemListChangeEventArgs e)
    {
        if (!HasInputAuthority) return;

        Destroy(contentTF.GetChild(e.Index).gameObject);
    }

    private IEnumerator SpawnListItems(NetworkDictionary<NetworkString<_4>, int> data)
    {
        while (contentTF.childCount > 0)
        {
            for (int a = 0; a < contentTF.childCount; a++)
            {
                Destroy(contentTF.GetChild(a).gameObject);
                yield return null;
            }

            yield return null;
        }

        if (data.Count <= 0) yield break;

        for (int a = 0; a < data.Count; a++)
        {
            GameObject item = Instantiate(itemButton, contentTF);

            item.GetComponent<WeaponListUIController>().InitializeData(itemSpritesData.GetItemListSprite(data.ElementAt(a).Key.ToString()), itemSpritesData.GetItemName(data.ElementAt(a).Key.ToString()), data.ElementAt(a).Value.ToString());
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                int index = item.GetComponent<WeaponListUIController>().Index;

                string weapondata = JsonConvert.SerializeObject(new TempItemWeaponData()
                {
                    index = index,
                    itemID = data.ElementAt(index).Key.ToString(),
                    ammo = data.ElementAt(index).Value,
                });

                playerInventory.Rpc_SpawnWeaponForPlayer(weapondata, Runner.LocalPlayer);
                Rpc_RemoveWeaponFromList(weapondata);
            });

            yield return null;
        }

        PickupItemBtn.SetActive(true);
        PickupItemList.SetActive(true);

        initialize = null;
    }

    private IEnumerator DestoryListItems()
    {
        while (contentTF.childCount > 0)
        {
            for (int a = 0; a < contentTF.childCount; a++)
            {
                contentTF.GetChild(a).GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(contentTF.GetChild(a).gameObject);
                yield return null;
            }

            yield return null;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RemoveWeaponFromList(string data)
    {
        CrateObject.GetComponent<CrateController>().Rpc_RemoveItemFromObject(data);
    }
}

[System.Serializable]
public class TempItemWeaponData
{
    public int index;
    public string itemID;
    public int ammo;
}
