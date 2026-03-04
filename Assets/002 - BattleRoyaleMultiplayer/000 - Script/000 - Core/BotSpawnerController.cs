using Fusion;
using NUnit.Framework.Internal.Execution;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotSpawnerController : NetworkBehaviour
{
    public static BotSpawnerController Instance { get; private set; }

    //  ======================

    public List<string> CBSBotNames { get => cbsbotnames; }
    public List<string> BotNames { get => botnames; }

    //  ====================

    [Header("BOTS")]
    [SerializeField] private int maxBot;
    [SerializeField] private NetworkObject botPrefab;
    [SerializeField] private List<Transform> botSpawnPoints;

    [Header("BOT WEAPONS")]
    [SerializeField] private PrimaryWeaponItem swordItem;
    [SerializeField] private PrimaryWeaponItem spearItem;
    [SerializeField] private ArmorItem armorItem;

    [Space]
    [SerializeField] private List<string> cbsbotnames = new List<string>
    {
        "kelboogle",
        "Boomboomyehey",
        "Joe Mama",
        "The Real",
        "Alex",
        "Darilyo",
        "Alexanderrr",
        "ganielleDOTexe",
        "Hebe Handsome",
        "crazychixx",
        "bellemona"
    };
    [SerializeField] private List<string> botnames = new List<string>
    {
        "BitBuddie",
        "SirTalksALot",
        "BlipBlop",
        "MissExecute",
        "CaptainQuirk",
        "BotlyMcBotface",
        "ChatChum",
        "NannyByte",
        "GlitchyWitch",
        "TaskToto",
        "MechaMilo",
        "ProntoPal",
        "WobbleBot",
        "Zapsterino",
        "ByteyBoi",
        "ClippyBot3000",
        "CircuitSis",
        "Dude.exe",
        "DottieBot",
        "CodeSnacc",
        "FunkyFirmware",
        "BuddyBoot",
        "ChatNugget",
        "Quirk.exe",
        "SnarkBot",
        "AutoBoi",
        "RoboChatter",
        "CaffeinateMe",
        "Pesto.exe",
        "HiccupBot",
        "Dorkatron",
        "SassCircuit",
        "GiggleLoop",
        "Spamuel",
        "BanterBot",
        "CaptainClick",
        "BinaryBabe",
        "BotOnTheRocks",
        "FrydayBot",
        "NibletBot",
        "Sassynator",
        "MechMimi",
        "Peppy.exe",
        "BoopBeep",
        "ByteSnax",
        "LazyLoop",
        "MuffinBot",
        "Yawn.exe",
        "CheekyBits",
        "RoboNoob",
        "BugsyBot",
        "LiloBot",
        "ToastyCore",
        "NekoNode",
        "GooberBot",
        "CrashPal",
        "SillyChip",
        "DonutBot",
        "Drifty.exe",
        "Perky.exe",
        "CuddleBit",
        "NapsterBot",
        "HappySudo",
        "MechaMocha",
        "QuirkyByte",
        "SnipSnip.exe",
        "DotDotBot",
        "RoboTeehee",
        "CodeSprout",
        "PeekoBot",
        "Wink.exe",
        "BashBuddy",
        "TinkerTock",
        "Botwink",
        "Purrtocol",
        "FrankieFirmware",
        "LolaLogic",
        "ElBotto",
        "QuinnBot",
        "JazzyBytes",
        "Tobi.exe",
        "Buglet",
        "RebootRicky",
        "MechaMittens",
        "GrinBot",
        "MoeBot",
        "PixelPunk",
        "TaterBot",
        "TwinkleChip",
        "ChuckleUnit",
        "FuzzyLogic",
        "ByteBella",
        "AutoAmy",
        "NannyNode",
        "FlipScript",
        "PingPongBot",
        "LolliBot",
        "Bloopette",
        "RikkaBot",
        "DitzBot",
        "ZappyZoe",
        "KikiKernel",
        "HappyCrank",
        "BlinkieBot",
        "ToastBot",
        "Nibble.exe",
        "RollyBot",
        "Chill.exe",
        "MintyBot",
        "SassyBoot",
        "HonkBot",
        "CooCoo.exe",
        "Chipette",
        "NuggieBot",
        "FroyoBot",
        "BeepBae",
        "Clunkie",
        "PikaBot",
        "PopPopBot",
        "BananaBot",
        "SudoSis",
        "Gloop.exe",
        "OllieBot",
        "WidgetWoo",
        "DotFace",
        "MechaWink",
        "SparkNeko",
        "CrankyBot",
        "SprinkleBit",
        "ByteBopper",
        "Cookie.exe",
        "FluffyBot",
        "RoboSniff",
        "ScootBot",
        "BlinkyBelle",
        "MrScrambles",
        "PoppyProtocol",
        "JellyBot",
        "ZuzuBot",
        "Fizz.exe",
        "GiddyCore",
        "BunnyBytes",
        "MechaToast",
        "DashieBot",
        "QuokkaBot",
        "HappyPatch",
        "Cheek.exe",
        "CuppyBot",
        "FloofCircuit",
        "Meowdule"
    };

    [Header("DEBUGGER")]
    [SerializeField] private int spawnBotIndex;
    [SerializeField] private int botNameIndex;
    [SerializeField] private bool doneCBSBotNames;

    //  ==========================

    [Networked, Capacity(50)] public NetworkDictionary<int, NetworkObject> Bots => default;

    Coroutine despawnBotsCoroutine;

    //  ==========================

    private void Awake()
    {
        Instance = this;
    }

    private void SpawnItems(NetworkObject obj)
    {
        BotInventory inventory = obj.GetComponent<BotInventory>();

        int randomWeapon = UnityEngine.Random.Range(0, 2);
        //int randomWeapon = 1;

        if (randomWeapon == 1)
        {
            int primaryWeaponRand = UnityEngine.Random.Range(0, 2);
            //int primaryWeaponRand = 1;

            Runner.Spawn(primaryWeaponRand == 0 ? swordItem : spearItem, Vector3.zero, Quaternion.identity, obj.InputAuthority, onBeforeSpawned: (runner, weaponObj) =>
            {
                PrimaryWeaponItem tempWeapon = weaponObj.GetComponent<PrimaryWeaponItem>();

                weaponObj.GetComponent<PrimaryWeaponItem>().InitializeItem(obj, true);

                inventory.PrimaryWeapon = tempWeapon;
                inventory.WeaponIndex = 2;
            });
        }
        else
        {
            inventory.WeaponIndex = 1;
        }

        int randHeal = UnityEngine.Random.Range(0, 6);

        inventory.HealCount = randHeal;

        int randArmor = UnityEngine.Random.Range(0, 3);

        if (randArmor == 0)
        {
            Runner.Spawn(armorItem, Vector3.zero, Quaternion.identity, obj.InputAuthority, onBeforeSpawned: (runner, armorObj) =>
            {
                ArmorItem tempArmor = armorObj.GetComponent<ArmorItem>();

                armorObj.GetComponent<ArmorItem>().InitializeItem(obj, inventory.ArmorBack, true, true);

                inventory.Armor = tempArmor;
            });

            int randRepair = UnityEngine.Random.Range(0, 6);
            inventory.RepairCount = randRepair;
        }

        int randTrap = UnityEngine.Random.Range(0, 5);

        inventory.TrapCount = randTrap;
    }


    private void SpawnBot()
    {
        Vector3 spawnPosition = botSpawnPoints[spawnBotIndex].position;

        Runner.Spawn(botPrefab, spawnPosition, Quaternion.identity, Object.StateAuthority, onBeforeSpawned: (runner, obj) =>
        {
            string tempbotname = GetBotName();

            Botdata tempbotdata = obj.GetComponent<Botdata>();

            //tempbotdata.ServerManager = this;
            tempbotdata.BotName = tempbotname;
            tempbotdata.BotIndex = spawnBotIndex;
            tempbotdata.Inventory.WeaponIndex = 1;

            Bots.Add(spawnBotIndex, obj);

            //SpawnItems(obj);

            spawnBotIndex++;
        });
    }

    IEnumerator UnspawnedBots()
    {
        for (int a = 0; a < Bots.Count; a++)
        {
            Runner.Despawn(Bots.ElementAt(a).Value);
            yield return null;
        }

        Bots.Clear();
    }

    private string GetBotName()
    {
        string name;

        if (!doneCBSBotNames && botNameIndex < cbsbotnames.Count)
        {
            name = cbsbotnames[botNameIndex];
            botNameIndex++;

            if (botNameIndex >= cbsbotnames.Count)
            {
                doneCBSBotNames = true;
                botNameIndex = 0;
            }
        }
        else
        {
            // Fallback to default botnames list
            name = botnames[botNameIndex % botnames.Count];
            botNameIndex++;
        }

        return name;
    }
}
