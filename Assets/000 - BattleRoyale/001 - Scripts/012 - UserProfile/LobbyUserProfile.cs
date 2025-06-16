using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyUserProfile : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Header("LOBBY")]
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI levelTMP;
    [SerializeField] private TextMeshProUGUI xpTMP;
    [SerializeField] private GameObject userDetailsObj;

    [Header("PROFILE")]
    [SerializeField] private TextMeshProUGUI usernameProfileTMP;
    [SerializeField] private TextMeshProUGUI levelProfileTMP;
    [SerializeField] private TextMeshProUGUI xpProfileTMP;
    [SerializeField] private TextMeshProUGUI killProfileTMP;
    [SerializeField] private TextMeshProUGUI deathProfileTMP;
    [SerializeField] private TextMeshProUGUI rankProfileTMP;

    public void SetData()
    {
        usernameTMP.text = userData.Username;
        levelTMP.text = $"{userData.GameDetails.level:n0}";
        xpTMP.text = $"{userData.GameDetails.xp:n0} / {80 * userData.GameDetails.level}";

        usernameProfileTMP.text = userData.Username;
        levelProfileTMP.text = $"{userData.GameDetails.level:n0}";
        xpProfileTMP.text = $"{userData.GameDetails.xp:n0} / {80 * userData.GameDetails.level}";
        killProfileTMP.text = $"{userData.GameDetails.kill:n0}";
        deathProfileTMP.text = $"{userData.GameDetails.death:n0}";
        rankProfileTMP.text = $"{userData.GameDetails.userrank:n0}";
    }
}

[System.Serializable]
public class GameUserDetails
{
    public int kill;
    public int death;
    public int level;
    public int xp;
    public int userrank;
}