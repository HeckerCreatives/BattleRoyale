using MyBox;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UserData", menuName = "Battle Royale/Player/UserData")]
public class UserData : ScriptableObject
{
    [field: ReadOnly][field: SerializeField] public string UserToken { get; set; }
    [field: ReadOnly][field: SerializeField] public string Username { get; set; }
    [field: ReadOnly][field: SerializeField] public string Password { get; set; }
    [field: ReadOnly][field: SerializeField] public bool RememberMe { get; set; }

    [field: Header("CHARACTER")]
    [field: ReadOnly][field: SerializeField] public PlayerCharacterSetting CharacterSetting { get; set; }

    [field: Header("GAME USER DETAILS")]
    [field: ReadOnly][field: SerializeField] public GameUserDetails GameDetails { get; set; }

    //  ===========================

    //  SETTINGS
    public Dictionary<string, ControllerSettingData> ControlSetting;

    //  ===========================

    private void OnEnable()
    {
        ResetLogin();
    }

    public IEnumerator CheckControlSettingSave()
    {
        if (PlayerPrefs.HasKey("ControlSetting"))
            ControlSetting = JsonConvert.DeserializeObject<Dictionary<string, ControllerSettingData>>(PlayerPrefs.GetString("ControlSetting"));
        else
            DefaultControllerLayout();

        yield return null;
    }

    public void DefaultControllerLayout()
    {
        ControlSetting = new Dictionary<string, ControllerSettingData>
            {
                { "AnalogStick", new ControllerSettingData{ sizeDeltaX = 278.7875f, sizeDeltaY = 278.79f, localPositionX = 206f, localPositionY = 168f, opacity = 1f} },
                { "LeftAttack", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = 66f, localPositionY = 505f, opacity = 1f} },
                { "RightAttack", new ControllerSettingData{ sizeDeltaX = 177.88f, sizeDeltaY = 177.88f, localPositionX = -301f, localPositionY = 195f, opacity = 1f} },
                { "Aim", new ControllerSettingData{ sizeDeltaX = 115.7508f, sizeDeltaY = 115.7508f, localPositionX = -296f, localPositionY = 435f, opacity = 1f} },
                { "Jump", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = -149f, localPositionY = 363f, opacity = 1f} },
                { "Crouch", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = -82f, localPositionY = 218f, opacity = 1f} },
                { "Prone", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = -82f, localPositionY = 72f, opacity = 1f} },
                { "Punch", new ControllerSettingData{ sizeDeltaX = 123.2555f, sizeDeltaY = 79.816f, localPositionX = -283f, localPositionY = 119f, opacity = 1f} },
                { "Primary", new ControllerSettingData{ sizeDeltaX = 192.5475f, sizeDeltaY = 79.816f, localPositionX = -114f, localPositionY = 119f, opacity = 1f} },
                { "Secondary", new ControllerSettingData{ sizeDeltaX = 192.5475f, sizeDeltaY = 79.816f, localPositionX = 95f, localPositionY = 119f, opacity = 1f} },
                { "Trap", new ControllerSettingData{ sizeDeltaX = 123.2555f, sizeDeltaY = 79.816f, localPositionX = 260.7891f, localPositionY = 119f, opacity = 1f} },
                { "Heal", new ControllerSettingData{ sizeDeltaX = 84.3541f, sizeDeltaY = 84.3541f, localPositionX = 348.9229f, localPositionY = 22.7f, opacity = 1f} },
                { "RepairArmor", new ControllerSettingData{ sizeDeltaX = 84.3541f, sizeDeltaY = 84.3541f, localPositionX = 445.3f, localPositionY = 22.7f, opacity = 1f} },
                { "Minimap", new ControllerSettingData{ sizeDeltaX = 285.7246f, sizeDeltaY = 285.7245f, localPositionX = -16f, localPositionY = -15f, opacity = 1f} },
                { "Pickup", new ControllerSettingData{ sizeDeltaX = 44.24683f, sizeDeltaY = 44.24683f, localPositionX = -430f, localPositionY = -180.7532f, opacity = 1f} },
                { "PickupItemList", new ControllerSettingData{ sizeDeltaX = 493.4304f, sizeDeltaY = 277.6669f, localPositionX = -424f, localPositionY = -232f, opacity = 1f} },
                { "Settings", new ControllerSettingData{ sizeDeltaX = 68f, sizeDeltaY = 62f, localPositionX = -319.6f, localPositionY = -15.70001f, opacity = 1f} }
            };

        PlayerPrefs.SetString("ControlSetting", JsonConvert.SerializeObject(ControlSetting));
    }

    public void RememberMeSave()
    {
        PlayerPrefs.SetString("Username", Username);
        PlayerPrefs.SetString("Password", Password);
        PlayerPrefs.SetString("RememberMe", "true");
    }

    public void RememberMeDelete()
    {
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Password");
        PlayerPrefs.DeleteKey("RememberMe");
    }

    public void LoadRememberMe()
    {
        if (PlayerPrefs.GetString("RememberMe") == "true")
        {
            RememberMe = PlayerPrefs.GetString("RememberMe") == "true" ? true : false;
            Username = PlayerPrefs.GetString("Username");
            Password = PlayerPrefs.GetString("Password");
        }
    }

    public void ResetLogin()
    {
        UserToken = "";
        Username = "";
        Password = "";
        RememberMe = false;
        LoadRememberMe();
        ControlSetting = new Dictionary<string, ControllerSettingData>();
    }
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
