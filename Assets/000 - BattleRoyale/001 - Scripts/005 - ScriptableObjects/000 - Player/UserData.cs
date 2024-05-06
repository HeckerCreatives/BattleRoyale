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

    //  ===========================

    //  SETTINGS
    public Dictionary<string, ControllerSettingData> ControlSetting;

    //  ===========================

    private void Awake()
    {
        ControlSetting = new Dictionary<string, ControllerSettingData>();
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
                { "AnalogStick", new ControllerSettingData{ sizeDeltaX = 225.3762f, sizeDeltaY = 225.3762f, localPositionX = 241f, localPositionY = 156f, opacity = 1f} },
                { "LeftAttack", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = 559f, localPositionY = 369f, opacity = 1f} },
                { "RightAttack", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = -643f, localPositionY = 369f, opacity = 1f} },
                { "Shoot", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = -352f, localPositionY = 283f, opacity = 1f} },
                { "Jump", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = -223.9999f, localPositionY = 191f, opacity = 1f} },
                { "Crouch", new ControllerSettingData{ sizeDeltaX = 108f, sizeDeltaY = 108f, localPositionX = -109.9998f, localPositionY = 63f, opacity = 1f} },
                { "Slider", new ControllerSettingData{ sizeDeltaX = 552.6965f, sizeDeltaY = 78.92438f, localPositionX = 0f, localPositionY = 29.00006f, opacity = 1f} },
                { "Primary", new ControllerSettingData{ sizeDeltaX = 302.2324f, sizeDeltaY = 125.2833f, localPositionX = -33.28766f, localPositionY = -204.9432f, opacity = 1f} },
                { "Secondary", new ControllerSettingData{ sizeDeltaX = 302.2324f, sizeDeltaY = 108.2833f, localPositionX = -33.28766f, localPositionY = -358.7864f, opacity = 1f} },
                { "Trap", new ControllerSettingData{ sizeDeltaX = 302.2324f, sizeDeltaY = 108.2833f, localPositionX = -33.28766f, localPositionY = -495.6298f, opacity = 1f} },
                { "Minimap", new ControllerSettingData{ sizeDeltaX = 345f, sizeDeltaY = 345f, localPositionX = 82f, localPositionY = -29.00006f, opacity = 1f} }
            };

        PlayerPrefs.SetString("ControlSetting", JsonConvert.SerializeObject(ControlSetting));
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
