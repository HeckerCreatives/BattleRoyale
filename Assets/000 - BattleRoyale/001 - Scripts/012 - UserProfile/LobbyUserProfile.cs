using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyUserProfile : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Header("LOBBY")]
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI levelXPTMP;
    [SerializeField] private GameObject userDetailsObj;

    [Header("PROFILE")]
    [SerializeField] private TextMeshProUGUI usernameProfileTMP;
    [SerializeField] private TextMeshProUGUI levelXPProfileTMP;
    [SerializeField] private TextMeshProUGUI killProfileTMP;
    [SerializeField] private TextMeshProUGUI deathProfileTMP;
    [SerializeField] private TextMeshProUGUI rankProfileTMP;

    public void SetData()
    {
        usernameTMP.text = userData.Username;
        levelXPTMP.text = $"Level: {userData.GameDetails.level}   XP: {userData.GameDetails.xp} / 20";

        usernameProfileTMP.text = userData.Username;
        levelXPProfileTMP.text = $"Level: {userData.GameDetails.level}   XP: {userData.GameDetails.xp} / 20";
        killProfileTMP.text = $"Kill: {userData.GameDetails.kill:n0}";
        deathProfileTMP.text = $"Death: {userData.GameDetails.death:n0}";
    }
}

[System.Serializable]
public class GameUserDetails
{
    public int kill;
    public int death;
    public int level;
    public int xp;
}