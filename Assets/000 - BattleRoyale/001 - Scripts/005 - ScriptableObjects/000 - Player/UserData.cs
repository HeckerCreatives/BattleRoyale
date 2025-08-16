using JetBrains.Annotations;
using MyBox;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

[CreateAssetMenu(fileName = "UserData", menuName = "Battle Royale/Player/UserData")]
public class UserData : ScriptableObject
{
    private event EventHandler SelectedServerChange;
    public event EventHandler OnSelectedServerChange
    {
        add
        {
            if (SelectedServerChange == null || !SelectedServerChange.GetInvocationList().Contains(value))
                SelectedServerChange += value;
        }
        remove
        {
            SelectedServerChange -= value;
        }
    }

    private event EventHandler LeaderboardPointsChange;
    public event EventHandler OnLeaderboardPointsChange
    {
        add
        {
            if (LeaderboardPointsChange == null || !LeaderboardPointsChange.GetInvocationList().Contains(value))
                LeaderboardPointsChange += value;
        }
        remove { LeaderboardPointsChange -= value; }
    }


    private event EventHandler CoinsPointsChange;
    public event EventHandler OnCoinsPointsChange
    {
        add
        {
            if (CoinsPointsChange == null || !CoinsPointsChange.GetInvocationList().Contains(value))
                CoinsPointsChange += value;
        }
        remove { CoinsPointsChange -= value; }
    }

    public void AddLeaderboardPoints(int amount)
    {
        GameDetails.leaderboard += amount;
        LeaderboardPointsChange?.Invoke(this, EventArgs.Empty);
    }

    public void AddCoins(float amount)
    {
        GameDetails.coins += amount;
        CoinsPointsChange?.Invoke(this, EventArgs.Empty);
    }

    private event EventHandler TitleChange;
    public event EventHandler OnTitleChange
    {
        add
        {
            if (TitleChange == null || !TitleChange.GetInvocationList().Contains(value))
                TitleChange += value;
        }
        remove { TitleChange -= value; }
    }
    public void TitleChangeFireEvent()
    {
        TitleChange?.Invoke(this, EventArgs.Empty);
    }

    private event EventHandler EnergyChange;
    public event EventHandler OnEnergyChange
    {
        add
        {
            if (EnergyChange == null || !EnergyChange.GetInvocationList().Contains(value))
                EnergyChange += value;
        }
        remove { EnergyChange -= value; }
    }
    public void EnergyChangeFireEvent()
    {
        EnergyChange?.Invoke(this, EventArgs.Empty);
    }

    //  =====================

    [field: SerializeField] public string UserToken { get; set; }
    [field: SerializeField] public string Username { get; set; }
    [field: SerializeField] public string Password { get; set; }
    [field: SerializeField] public bool RememberMe { get; set; }
    [field: SerializeField] public string SelectedServer { get; set; }

    [field: Header("CHARACTER")]
    [field: SerializeField] public PlayerCharacterSetting CharacterSetting { get; set; }

    [field: Header("GAME USER DETAILS")]
    [field: SerializeField] public GameUserDetails GameDetails { get; set; }

    [field: Header("PLAYER INVENTORY")]
    [field: SerializeField] public Dictionary<string, Inventory> PlayerInventory { get; set; }
    [field: SerializeField] public Dictionary<string, ItemEffects> PlayerItemEffects { get; set; }

    [field: Header("MESSAGES")]
    [field: SerializeField] public List<MessageItem> Messages { get; set; }

    [field: Header("SERVERS")]
    [field: SerializeField] public List<string> ServerList { get; set; }



    //  ===========================

    //  SETTINGS
    public Dictionary<string, ControllerSettingData> ControlSetting;

    private List<string> controllerSettingKeys = new List<string>
    {
        "AnalogStick",
        "LeftAttack",
        "RightAttack",
        "Aim",
        "Jump",
        "Sprint",
        "Roll",
        "Block",
        "Punch",
        "Primary",
        "Secondary",
        "Trap",
        "Heal",
        "RepairArmor",
        "Minimap",
        "Pickup",
        "PickupItemList",
        "Settings",
        "KillNotification",
        "Reload",
        "Stamina",
        "SafeZoneTimer",
        "GameStatus",
        "HealthArmor",
        "Slot 1",
        "Slot 2"
    };

    //  ===========================

    private void OnEnable()
    {
        UserToken = "";
        Username = "";
        Password = "";
        SelectedServer = "";
        RememberMe = false;
        CharacterSetting = new PlayerCharacterSetting();
        GameDetails = new GameUserDetails();
        Messages = new List<MessageItem>();
        PlayerInventory = new Dictionary<string, Inventory>();
        PlayerItemEffects = new Dictionary<string, ItemEffects>();
    }

    public IEnumerator CheckControlSettingSave()
    {
        if (PlayerPrefs.HasKey("ControlSetting"))
        {
            Dictionary<string, ControllerSettingData> tempsettingcontroller = JsonConvert.DeserializeObject<Dictionary<string, ControllerSettingData>>(PlayerPrefs.GetString("ControlSetting"));

            if (tempsettingcontroller.Keys.Count < controllerSettingKeys.Count)
            {
                DefaultControllerLayout();
                yield break;
            }

            ControlSetting = tempsettingcontroller;
        }
        else
            DefaultControllerLayout();

        yield return null;
    }

    public void DefaultControllerLayout()
    {
        ControlSetting = new Dictionary<string, ControllerSettingData>
            {
                { "AnalogStick", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -733f, localPositionY = -219f, opacity = 1f} },
                { "LeftAttack", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 607f, localPositionY = -20f, opacity = 1f} },
                { "RightAttack", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -308f, localPositionY = 177f, opacity = 1f} },
                { "Aim", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -431.9999f, localPositionY = 425f, opacity = 1f} },
                { "Jump", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -253f, localPositionY = 413f, opacity = 1f} },
                { "Block", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -95f, localPositionY = 509f, opacity = 1f} },
                { "Sprint", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -95f, localPositionY = 283f, opacity = 1f} },
                { "Roll", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -95f, localPositionY = 121f, opacity = 1f} },
                { "Stamina", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 0f, localPositionY = -306f, opacity = 1f} },
                { "Punch", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -283f, localPositionY = -360.092f, opacity = 1f} },
                { "Primary", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -114f, localPositionY = -360.092f, opacity = 1f} },
                { "Secondary", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 95f, localPositionY = -360.092f, opacity = 1f} },
                { "Trap", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 260.7891f, localPositionY = -360.092f, opacity = 1f} },
                { "Heal", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 308.6229f, localPositionY = -454.123f, opacity = 1f} },
                { "RepairArmor", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 405f, localPositionY = -454.123f, opacity = 1f} },
                { "Minimap", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -16f, localPositionY = -15.00006f, opacity = 1f} },
                { "PickupItemList", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -424f, localPositionY = -232f, opacity = 1f} },
                { "Settings", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -319.6002f, localPositionY = -15.69989f, opacity = 1f} },
                { "KillNotification", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 0f, localPositionY = -250f, opacity = 1f} },
                { "Reload", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 380f, localPositionY = -361.682f, opacity = 1f} },
                { "SafeZoneTimer", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -16f, localPositionY = -313f, opacity = 1f} },
                { "GameStatus", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 0f, localPositionY = -23f, opacity = 1f} },
                { "HealthArmor", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -20.67504f, localPositionY = -459f, opacity = 1f} },
                { "Slot 1", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -458.377f, localPositionY = -454.123f, opacity = 1f} },
                { "Slot 2", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -362f, localPositionY =- 454.123f, opacity = 1f} }
            };

        PlayerPrefs.SetString("ControlSetting", JsonConvert.SerializeObject(ControlSetting));
    }

    public void RememberMeSave()
    {
        PlayerPrefs.SetString("Username", Username);
        PlayerPrefs.SetString("Password", Password);
        PlayerPrefs.SetString("RememberMe", "true");

        Debug.Log($"Remember Me save: {PlayerPrefs.GetString("RememberMe")}");
    }

    public void RememberMeDelete()
    {
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Password");
        PlayerPrefs.DeleteKey("RememberMe");
        PlayerPrefs.DeleteKey("server");
    }

    public void LoadRememberMe()
    {
        Debug.Log($"UserData Player Prefs Load Remember Me: {PlayerPrefs.GetString("RememberMe")}");
        if (PlayerPrefs.GetString("RememberMe") == "true")
        {
            RememberMe = true;
            Username = PlayerPrefs.GetString("Username");
            Password = PlayerPrefs.GetString("Password");
            SelectedServer = PlayerPrefs.GetString("server");
        }
    }

    public void ResetLogin()
    {
        Debug.Log($"Login reset");
        UserToken = "";
        Username = "";
        Password = "";
        SelectedServer = "";
        RememberMe = false;
        LoadRememberMe();
        ControlSetting = new Dictionary<string, ControllerSettingData>();
        CharacterSetting = new PlayerCharacterSetting();
        GameDetails = new GameUserDetails();
        Messages = new List<MessageItem>();
        PlayerInventory = new Dictionary<string, Inventory>();
        PlayerItemEffects = new Dictionary<string, ItemEffects>();
    }

    public void ChangeServerEventTrigger() => SelectedServerChange?.Invoke(this, EventArgs.Empty);
}

[System.Serializable]
public class PlayerCharacterSetting
{
    public int hairstyle;
    public int haircolor;
    public int clothingcolor;
    public int skincolor;
}

[System.Serializable]
public class ControllerSettingData
{
    public float sizeDeltaX;
    public float sizeDeltaY;
    public float localPositionX;
    public float localPositionY;
    public float opacity;
}

[System.Serializable]
public class Inventory
{
    public string itemid;
    public string itemname;
    public int quantity;
    public string type;
    public bool isEquipped;
    public bool canUse;
    public bool canEquip;
    public bool canSell;
}

[System.Serializable]
public class ItemEffects
{
    public string itemid;
    public string itemname;
    public string type;
    public int multiplier;
    public string expiresAt;
    public float timeRemaining;
}