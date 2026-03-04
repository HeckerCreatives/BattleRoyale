using Fusion;
using NUnit.Framework.Internal.Execution;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WeaponCratesSpawnerController : NetworkBehaviour
{
    public bool DoneSpawnCrates { get => doneSpawnCrates; }

    public List<Transform> CreateSpawnLocations { get => createSpawnLocations; set => createSpawnLocations = value; }

    //  =========================

    [Header("CRATES")]
    [SerializeField] private NetworkObject createNO;

    [Header("DEBUGGER")]
    [SerializeField] private bool doneSpawnCrates;
    [SerializeField] private List<Transform> createSpawnLocations;

    private Dictionary<string, int> GenerateRandomItems()
    {
        List<string> itemPool = new List<string>
        {
            "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", // 15%
            "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", // 15%
            "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", // 20%
            "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", // 20%
            "heal", "heal", "heal", "heal", "heal", "heal", "heal", "heal", "heal", "heal", // 10%
            "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", // 10%
            "armor", "armor", "armor", "armor", "armor", "armor", "armor", "armor", "armor", "armor", // 10%
            "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", // 
            "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", // 10%
            "trap", "trap", "trap", "trap", "trap", "trap", "trap", "trap", "trap", "trap", // 10%
        };


        Dictionary<string, string> itemIDMap = new Dictionary<string, string>
        {
            { "sword", "001" },
            { "spear", "002" },
            { "rifle", "003" },
            { "bow", "004" },
            { "rifle ammo", "005" },
            { "bow ammo", "006" },
            { "armor", "007" },
            { "heal", "008" },
            { "repair armor", "009" },
            { "trap", "010" }
        };

        Dictionary<string, int> selectedItems = new Dictionary<string, int>();

        int itemListQTY = UnityEngine.Random.Range(1, 11);

        for (int i = 0; i < itemListQTY; i++)
        {
            string selectedItem = itemPool[UnityEngine.Random.Range(0, itemPool.Count)];
            string itemID = itemIDMap[selectedItem];

            if (selectedItem == "rifle ammo")
            {
                if (!selectedItems.ContainsKey(itemID))
                    selectedItems[itemID] = UnityEngine.Random.Range(10, 21); // Add a random quantity between 1 and 60
            }
            else if (selectedItem == "bow ammo")
            {
                if (!selectedItems.ContainsKey(itemID))
                    selectedItems[itemID] = UnityEngine.Random.Range(5, 16); // Add a random quantity between 1 and 60
            }
            else if (selectedItem == "rifle")
            {
                selectedItems[itemID] = 10;
            }
            else if (selectedItem == "bow")
            {
                selectedItems[itemID] = 5;
            }
            else if (selectedItem == "armor")
            {
                selectedItems[itemID] = 100;
            }
            else if (selectedItem == "heal" || selectedItem == "repair armor")
            {
                selectedItems[itemID] = 1;
            }
            else
            {
                if (!selectedItems.ContainsKey(itemID))
                {
                    selectedItems[itemID] = 1; // Set quantity to 1 for non-ammo items
                }
            }
        }

        return selectedItems;
    }

    public async void SpawnCrates()
    {
        Debug.Log("start spawning crates");

        while (!Runner)
            await Task.Yield();

        int index = 1;

        foreach (var spawnLocations in createSpawnLocations)
        {
            var gameobject = Runner.Spawn(createNO, spawnLocations.transform.position, Quaternion.identity, null);

            gameobject.GetComponent<CrateController>().SetDatas(GenerateRandomItems());

            index++;

            await Task.Yield();
        }

        Debug.Log("done for spawn crates");

        doneSpawnCrates = true;
    }
}
