using Fusion;
using log4net.Filter;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public enum LeaderboardState
{
    POINTS,
    KILL,
    DEATH,
    LEVEL
}

public class LobbyController : MonoBehaviour
{
    private event EventHandler LeaderboardStateChange;
    public event EventHandler OnLeaderboardStateChange
    {
        add
        {
            if (LeaderboardStateChange == null || !LeaderboardStateChange.GetInvocationList().Contains(value))
                LeaderboardStateChange += value;
        }
        remove { LeaderboardStateChange -= value; }
    }
    public LeaderboardState CurrentLeaderboardState
    {
        get => currentLeaderboardState;
        set
        {
            currentLeaderboardState = value;
            LeaderboardStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    //  ========================

    [SerializeField] private UserData userData;
    [SerializeField] private ClientMatchmakingController matchmakingController;
    [SerializeField] private CharacterCreationController characterCreationController;
    [SerializeField] private GameSettingController gameSettingController;
    [SerializeField] private ControllerSetting controllerSetting;
    [SerializeField] private LobbyUserProfile userProfile;
    [SerializeField] private InventoryController inventoryController;

    [Space]
    [SerializeField] private AudioClip bgMusicClip;
    [SerializeField] private TextMeshProUGUI serverTMP;
    [SerializeField] private NetworkRunner instanceRunner;
    [SerializeField] private GameObject serverList;
    [SerializeField] private TextMeshProUGUI totalPlayersOnlineTMP; 
    [SerializeField] private TextMeshProUGUI seasonTMP;
    [SerializeField] private TextMeshProUGUI potionMultiplier;
    [SerializeField] private TextMeshProUGUI titleTMP;

    [Space]
    [SerializeField] private List<LeaderboardItem> leaderboardItems;
    [SerializeField] private List<LeaderboardItem> killLeaderboardItems;
    [SerializeField] private List<LeaderboardItem> deathLeaderboardItems;
    [SerializeField] private List<LeaderboardItem> levelLeaderboardItems;

    [Space]
    [SerializeField] private List<ProfileHistoryItem> profileHistoryItems;

    [Space]
    [SerializeField] private AudioClip buttonClip;

    [SerializeField] private GameObject unreadObj;
    [SerializeField] private TextMeshProUGUI unreadValueTMP;

    [Space]
    [SerializeField] private TextMeshProUGUI resetTimerTMP;

    [Header("DEBUGGER")]
    [SerializeField] private bool cancountdowntime;
    [SerializeField] public NetworkRunner currentRunnerInstance;
    [SerializeField] private LeaderboardState currentLeaderboardState;

    //  ==================

    public Dictionary<string, int> AvailableServers = new Dictionary<string, int>();

    //  ==================

    private void Awake()
    {
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/characters/getcharactersetting", "", false, (response) =>
        {
            try
            {
                PlayerCharacterSetting charactersetting = JsonConvert.DeserializeObject<PlayerCharacterSetting>(response.ToString());

                Debug.Log(response.ToString());
                userData.CharacterSetting = charactersetting;
                characterCreationController.InitializeCharacterSettings(userData.CharacterSetting.hairstyle, userData.CharacterSetting.haircolor, userData.CharacterSetting.clothingcolor, userData.CharacterSetting.skincolor);
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 1", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 2", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/usergamedetail/getusergamedetails", "", false, (response) =>
        {
            Debug.Log(response.ToString());
            try
            {
                GameUserDetails gameUserDetails = JsonConvert.DeserializeObject<GameUserDetails>(response.ToString());

                userData.GameDetails = gameUserDetails;
                userProfile.SetData();

                cancountdowntime = true;
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getleaderboard", "", false, (response) =>
        {
            try
            {
                Dictionary<string, object> responsetempdata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());

                if (responsetempdata.Count <= 0) return;

                Dictionary<string, LeaderboardData> tempdata = JsonConvert.DeserializeObject<Dictionary<string, LeaderboardData>>(responsetempdata["leaderboard"].ToString());

                //  POINTS
                for (int a = 0; a < leaderboardItems.Count; a++)
                {
                    if (a < tempdata.Count)
                    {
                        leaderboardItems[a].SetData(tempdata[a.ToString()].user, (a + 1).ToString("n0"), tempdata[a.ToString()].amount.ToString("n0"));
                    }
                    else
                    {
                        leaderboardItems[a].SetData("-", (a + 1).ToString("n0"), "-");
                    }
                }
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getkillleaderboard", "", false, (response) =>
        {
            try
            {
                Dictionary<string, object> responsetempdata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());

                if (responsetempdata.Count <= 0) return;

                Dictionary<string, LeaderboardData> tempdata = JsonConvert.DeserializeObject<Dictionary<string, LeaderboardData>>(responsetempdata["leaderboard"].ToString());

                //  KILL
                for (int a = 0; a < killLeaderboardItems.Count; a++)
                {
                    if (a < tempdata.Count)
                    {
                        killLeaderboardItems[a].SetData(tempdata[a.ToString()].user, (a + 1).ToString("n0"), tempdata[a.ToString()].amount.ToString("n0"));
                    }
                    else
                    {
                        killLeaderboardItems[a].SetData("-", (a + 1).ToString("n0"), "-");
                    }
                }
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getdeathleaderboard", "", false, (response) =>
        {
            try
            {
                Dictionary<string, object> responsetempdata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());

                if (responsetempdata.Count <= 0) return;

                Dictionary<string, LeaderboardData> tempdata = JsonConvert.DeserializeObject<Dictionary<string, LeaderboardData>>(responsetempdata["leaderboard"].ToString());

                //  KILL
                for (int a = 0; a < deathLeaderboardItems.Count; a++)
                {
                    if (a < tempdata.Count)
                    {
                        deathLeaderboardItems[a].SetData(tempdata[a.ToString()].user, (a + 1).ToString("n0"), tempdata[a.ToString()].amount.ToString("n0"));
                    }
                    else
                    {
                        deathLeaderboardItems[a].SetData("-", (a + 1).ToString("n0"), "-");
                    }
                }
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getlevelleaderboard", "", false, (response) =>
        {
            try
            {
                Dictionary<string, object> responsetempdata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());

                if (responsetempdata.Count <= 0) return;

                Dictionary<string, LeaderboardData> tempdata = JsonConvert.DeserializeObject<Dictionary<string, LeaderboardData>>(responsetempdata["leaderboard"].ToString());

                //  KILL
                for (int a = 0; a < levelLeaderboardItems.Count; a++)
                {
                    if (a < tempdata.Count)
                    {
                        levelLeaderboardItems[a].SetData(tempdata[a.ToString()].user, (a + 1).ToString("n0"), tempdata[a.ToString()].amount.ToString("n0"));
                    }
                    else
                    {
                        levelLeaderboardItems[a].SetData("-", (a + 1).ToString("n0"), "-");
                    }
                }
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/usergamedetail/getmatchhistory", "?limit=10", false, (response) =>
        {
            Debug.Log(response.ToString());
            try
            {
                Dictionary<string, MatchHistory> tempdata = JsonConvert.DeserializeObject<Dictionary<string, MatchHistory>>(response.ToString());

                //  KILL
                for (int a = 0; a < profileHistoryItems.Count; a++)
                {
                    if (a < tempdata.Count)
                    {
                        profileHistoryItems[a].InitializeHistory(tempdata[a.ToString()].kill, tempdata[a.ToString()].placement, tempdata[a.ToString()].date);
                    }
                    else
                    {
                        profileHistoryItems[a].InitializeHistory("-", "-", "-");
                    }
                }
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/marketplace/wallets", "", false, (response) =>
        {
            try
            {
                Dictionary<string, float> tempdata = JsonConvert.DeserializeObject<Dictionary<string, float>>(response.ToString());

                userData.GameDetails.coins = tempdata["coins"];

                userProfile.SetData();
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));

        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/season/getcurrentseason", "", false, (response) =>
        {
            try
            {
                seasonTMP.text = response.ToString();
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));

        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/marketplace/effects", "", false, (response) =>
        {
            try
            {
                Dictionary<string, ItemEffects> tempeffects = JsonConvert.DeserializeObject<Dictionary<string, ItemEffects>>(response.ToString());

                userData.PlayerItemEffects = tempeffects;
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetVolumeSlidersOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetGraphicsOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetLookSensitivityOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/inbox/getinboxlist", "", false, (response) =>
        {
            if (response != null)
            {
                userData.Messages = JsonConvert.DeserializeObject<List<MessageItem>>(response.ToString());

                int unopenedCount = userData.Messages.Count(m => m.status == "unopen");

                if (unopenedCount > 0)
                {
                    unreadValueTMP.text = unopenedCount.ToString("n0");
                    unreadObj.SetActive(true);
                }
                else
                    unreadObj.SetActive(false);
            }
        }, null));
        GameManager.Instance.SceneController.AddActionLoadinList(inventoryController.GetInventory());
        GameManager.Instance.SceneController.AddActionLoadinList(CheckIfFirstTimeDownload());
        GameManager.Instance.AudioController.SetBGMusic(bgMusicClip);
        GameManager.Instance.SceneController.ActionPass = true;


        GameManager.Instance.SocketMngr.OnPlayerCountServerChange += PlayerCountChange;

        userData.OnSelectedServerChange += ServerChange;
        userData.OnTitleChange += TitleChange;

        serverTMP.text = $"Server: {GameManager.GetRegionName(userData.SelectedServer)}";
        totalPlayersOnlineTMP.text = $"Online: <color=green>{GameManager.Instance.SocketMngr.PlayerCountServer:n0}</color>";
    }

    private void OnDisable()
    {
        GameManager.Instance.SocketMngr.OnPlayerCountServerChange -= PlayerCountChange;
        userData.OnSelectedServerChange -= ServerChange;
        userData.OnTitleChange -= TitleChange;
    }

    private void Update()
    {
        if (cancountdowntime)
        {
            if (userData.GameDetails.energyresettime > 0)
            {
                userData.GameDetails.energyresettime -= Time.unscaledDeltaTime;

                float totalSeconds = userData.GameDetails.energyresettime; // example: 86400 seconds = 24 hours

                float hours = totalSeconds / 3600;
                float minutes = (totalSeconds % 3600) / 60;

                string formatted = $"{hours:00}h {minutes:00}m";

                resetTimerTMP.text = $"<size=18>RESET IN</size> <size=30>{formatted}</size>";
            }
            else
            {
                userData.GameDetails.energyresettime = 86400; // Reset to 24 hours

                if (userData.GameDetails.energy > 10) return;

                userData.GameDetails.energy = 10;

            }
        }

        if (userData.PlayerItemEffects.Count > 0)
        {
            userData.PlayerItemEffects.ElementAt(0).Value.timeRemaining -= Time.unscaledDeltaTime;

            potionMultiplier.text = $"x{userData.PlayerItemEffects.ElementAt(0).Value.multiplier}";

            if (userData.PlayerItemEffects.ElementAt(0).Value.timeRemaining <= 0)
                userData.PlayerItemEffects.Clear();
        }
        else
            potionMultiplier.text = $"x1";
    }

    private IEnumerator CheckIfFirstTimeDownload()
    {
        if (PlayerPrefs.HasKey("firstdownload")) yield break;

        GameManager.Instance.NotificationController.ShowError("You're currently on the lowest graphics settings. You can change your graphics by going to settings");

        PlayerPrefs.SetInt("firstdownload", 1);

        yield return null;
    }

    private void ServerChange(object sender, EventArgs e)
    {
        serverTMP.text = $"Server: {GameManager.GetRegionName(userData.SelectedServer)}";
    }

    private void PlayerCountChange(object sender, EventArgs e)
    {
        totalPlayersOnlineTMP.text = $"Online: <color=green>{GameManager.Instance.SocketMngr.PlayerCountServer:n0}</color>";
    }

    private void TitleChange(object sender, EventArgs e)
    {
        TitleChecker();
    }

    public async void GetAvailableRegions()
    {
        GameManager.Instance.NoBGLoading.SetActive(true);

        //if (currentRunnerInstance != null)
        //{
        //    Destroy(currentRunnerInstance.gameObject);

        //    currentRunnerInstance = null;
        //}
        //else
        //{
        //    currentRunnerInstance = Instantiate(instanceRunner);
        //}

        //var _tokenSource = new CancellationTokenSource();

        //var regions = await NetworkRunner.GetAvailableRegions(cancellationToken: _tokenSource.Token);

        //AvailableServers.Clear();

        //foreach (var region in regions)
        //{
        //    if (userData.SelectedServer != "asia" && userData.SelectedServer != "za" && userData.SelectedServer != "uae" && userData.SelectedServer != "us" && userData.SelectedServer != "usw") continue;

        //    AvailableServers.Add(region.RegionCode, region.RegionPing);

        //    await Task.Yield();
        //}

        //serverList.SetActive(true);

        //Destroy(currentRunnerInstance.gameObject);

        //currentRunnerInstance = null;

        serverList.SetActive(true);

        GameManager.Instance.NoBGLoading.SetActive(false);
    }

    public void ChangeScene()
    {
        GameManager.Instance.SceneController.CurrentScene = "Prototype";
    }

    public void Logout()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to logout?", async () => 
        {
            GameManager.Instance.NoBGLoading.SetActive(true);
            if (matchmakingController.currentRunnerInstance != null)
                await matchmakingController.ShutdownServer();

            userData.ResetLogin();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
        }, null);
    }

    public void ButtonPress() => GameManager.Instance.AudioController.PlaySFX(buttonClip);

    public void TitleChecker()
    {
        if (userData.PlayerInventory.Count > 0)
        {
            var filteredItems = userData.PlayerInventory
                .Where(kvp => kvp.Value.type == "title" && kvp.Value.isEquipped == true)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (filteredItems.Count > 0)
                titleTMP.text = filteredItems.ElementAt(0).Value.itemname;
            else
                titleTMP.text = "";
        }
        else
            titleTMP.text = "";
    }
}

[System.Serializable]
public class LeaderboardData
{
    public string user;
    public int amount;
}

[System.Serializable]
public class MatchHistory
{
    public string kill;
    public string placement;
    public string date;
}