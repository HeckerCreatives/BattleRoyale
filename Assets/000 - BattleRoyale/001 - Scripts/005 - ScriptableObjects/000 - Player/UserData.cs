using MyBox;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    //  =====================

    [field: ReadOnly][field: SerializeField] public string UserToken { get; set; }
    [field: ReadOnly][field: SerializeField] public string Username { get; set; }
    [field: ReadOnly][field: SerializeField] public string Password { get; set; }
    [field: ReadOnly][field: SerializeField] public bool RememberMe { get; set; }
    [field: ReadOnly][field: SerializeField] public string SelectedServer { get; set; }

    [field: Header("CHARACTER")]
    [field: ReadOnly][field: SerializeField] public PlayerCharacterSetting CharacterSetting { get; set; }

    [field: Header("GAME USER DETAILS")]
    [field: ReadOnly][field: SerializeField] public GameUserDetails GameDetails { get; set; }

    [field: Header("MESSAGES")]
    [field: ReadOnly][field: SerializeField] public List<MessageItem> Messages { get; set; }



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
                { "Aim", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 654f, localPositionY = -90.999f, opacity = 1f} },
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
                { "PickupItemList", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = 453.2848f, localPositionY = 148.1666f, opacity = 1f} },
                { "Settings", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -319.6002f, localPositionY = -15.69989f, opacity = 1f} },
                { "KillNotification", new ControllerSettingData{ sizeDeltaX = 1f, sizeDeltaY = 1f, localPositionX = -944.5391f, localPositionY = 341.4225f, opacity = 1f} },
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
