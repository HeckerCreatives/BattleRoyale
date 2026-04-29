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
using Unity.VisualScripting;
using UnityEngine;

public enum LeaderboardState
{
    POINTS,
    KILL,
    DEATH,
    LEVEL,
    PLAYTIME,
    MATCH,
    WIN
}

public enum MENUSTATE
{
    LOBBY,
    MESSAGES,
    SETTINGS,
    UISETTINGS,
    LEADERBOARD,
    PROFILE,
    SERVERSELECT,
    MARKETPLACE,
    INVENTORY
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

    private event EventHandler LobbyStateChange;
    public event EventHandler OnLobbyStateChange
    {
        add
        {
            if (LobbyStateChange == null || !LobbyStateChange.GetInvocationList().Contains(value))
                LobbyStateChange += value;
        }
        remove {  LobbyStateChange -= value; }
    }
    public MENUSTATE CurrentMenuState
    {
        get => currentMenuState;
        set
        {
            currentMenuState = value;
            LobbyStateChange?.Invoke(this, EventArgs.Empty);
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
    [SerializeField] private MarketplaceController martketController;
    [SerializeField] private LobbyQuestController questController;

    [Space]
    [SerializeField] private AudioClip bgMusicClip;
    [SerializeField] private TextMeshProUGUI serverTMP;
    [SerializeField] private NetworkRunner instanceRunner;
    [SerializeField] private GameObject serverList;
    [SerializeField] private TextMeshProUGUI totalPlayersOnlineTMP; 
    [SerializeField] private TextMeshProUGUI seasonTMP;
    [SerializeField] private TextMeshProUGUI potionMultiplier;

    [Space]
    [SerializeField] private List<string> titlesName;
    [SerializeField] private List<GameObject> titles;

    [Space]
    [SerializeField] private List<LeaderboardItem> leaderboardItems;
    [SerializeField] private List<LeaderboardItem> killLeaderboardItems;
    [SerializeField] private List<LeaderboardItem> deathLeaderboardItems;
    [SerializeField] private List<LeaderboardItem> levelLeaderboardItems;
    [SerializeField] private List<LeaderboardItem> playTimeLeaderboadItems;
    [SerializeField] private List<LeaderboardItem> matchLeaderboadItems;
    [SerializeField] private List<LeaderboardItem> winsLeaderboadItems;

    [Space]
    [SerializeField] private List<ProfileHistoryItem> profileHistoryItems;

    [Space]
    [SerializeField] private AudioClip buttonClip;

    [SerializeField] private GameObject unreadObj;
    [SerializeField] private TextMeshProUGUI unreadValueTMP;

    [Space]
    [SerializeField] private TextMeshProUGUI resetTimerTMP;

    [Space]
    [SerializeField] private GameObject reconObj;
    [SerializeField] private GameObject findMatchObj;

    [Header("DEBUGGER")]
    [SerializeField] private MENUSTATE currentMenuState;
    [SerializeField] private bool cancountdowntime;
    [SerializeField] public NetworkRunner currentRunnerInstance;
    [SerializeField] private LeaderboardState currentLeaderboardState;

    //  ==================

    public Dictionary<string, int> AvailableServers = new Dictionary<string, int>();

    //  ==================

    private void Awake()
    {
        GameManager.Instance.SceneController.AddActionLoadinList(matchmakingController.WaitForReconnectStatus());
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/characters/getcharactersetting", "", false, (response) =>
        {
            try
            {
                Debug.Log("2 LOADING");
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
            GameManager.Instance.SocketMngr.isOnLogin = false;
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
                Debug.Log("3 LOADING");
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
                Debug.Log("4 LOADING");
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
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getplaytimeleaderboard", "", false, (response) =>
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
                        playTimeLeaderboadItems[a].SetData(tempdata[a.ToString()].user, (a + 1).ToString("n0"), GameManager.Instance.GetHourMinuteSecondsTime(tempdata[a.ToString()].amount));
                    }
                    else
                    {
                        playTimeLeaderboadItems[a].SetData("-", (a + 1).ToString("n0"), "-");
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
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getmatchesleaderboard", "", false, (response) =>
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
                        matchLeaderboadItems[a].SetData(tempdata[a.ToString()].user, (a + 1).ToString("n0"), tempdata[a.ToString()].amount.ToString("n0"));
                    }
                    else
                    {
                        matchLeaderboadItems[a].SetData("-", (a + 1).ToString("n0"), "-");
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
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getwinsleaderboard", "", false, (response) =>
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
                        winsLeaderboadItems[a].SetData(tempdata[a.ToString()].user, (a + 1).ToString("n0"), tempdata[a.ToString()].amount.ToString("n0"));
                    }
                    else
                    {
                        winsLeaderboadItems[a].SetData("-", (a + 1).ToString("n0"), "-");
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

                for (int a = 0; a < profileHistoryItems.Count; a++)
                {
                    profileHistoryItems[a].InitializeHistory("-", "-", "-", "-", "-");
                }

                //  KILL
                for (int a = 0; a < profileHistoryItems.Count; a++)
                {
                    if (a < tempdata.Count)
                    {
                        profileHistoryItems[a].InitializeHistory("NORMAL", tempdata[a.ToString()].kill, tempdata[a.ToString()].placement, GameManager.Instance.GetMinuteSecondsTime(tempdata[a.ToString()].playtime), tempdata[a.ToString()].date);
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
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/avatar/getavatar", "", false, (response) =>
        {
            Debug.Log(response.ToString());
            try
            {
                userData.AvatarID = response.ToString();
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
                Debug.Log("INITIALIZE WALLET");
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
                Debug.Log("INITIALIZE SEASONS");
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
                Debug.Log("INITIALIZE EFFECTS");
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
        GameManager.Instance.SceneController.AddActionLoadinList(LoadRewardAds());
        GameManager.Instance.SceneController.AddActionLoadinList(martketController.GetAdsData());
        GameManager.Instance.SceneController.AddActionLoadinList(questController.FetchQuestDataRoutine());
        GameManager.Instance.SceneController.AddActionLoadinList(CheckIfFirstTimeDownload());
        GameManager.Instance.AudioController.SetBGMusic(bgMusicClip);
        GameManager.Instance.SceneController.ActionPass = true;


        GameManager.Instance.SocketMngr.OnPlayerCountServerChange += PlayerCountChange;
        OnLobbyStateChange += MenuStateChange;

        userData.OnSelectedServerChange += ServerChange;
        userData.OnTitleChange += TitleChange;

        serverTMP.text = $"Server: {GameManager.GetRegionName(userData.SelectedServer)}";
        totalPlayersOnlineTMP.text = $"Online: {GameManager.Instance.SocketMngr.PlayerCountServer:n0}";
    }

    private void OnDisable()
    {
        GameManager.Instance.SocketMngr.OnPlayerCountServerChange -= PlayerCountChange;
        userData.OnSelectedServerChange -= ServerChange;
        userData.OnTitleChange -= TitleChange;
        OnLobbyStateChange -= MenuStateChange;
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


    private void MenuStateChange(object sender, EventArgs e)
    {
    }

    public void ChangeMenuState(int index) => CurrentMenuState = (MENUSTATE)index;

    public IEnumerator RefreshUserData(bool stayAliveLoadingAfter = false)
    {
        yield return StartCoroutine(GameManager.Instance.GetRequest("/usergamedetail/getusergamedetails", "", stayAliveLoadingAfter, (response) =>
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
    }

    private IEnumerator LoadRewardAds()
    {
        GameManager.Instance.AdsManager.LoadRewardedAd();

        yield return null;
    } 

    private IEnumerator CheckIfFirstTimeDownload()
    {
        if (PlayerPrefs.HasKey("firstdownload")) yield break;

        //GameManager.Instance.NotificationController.ShowError("You're currently on the lowest graphics settings. You can change your graphics by going to settings");

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

    public void ChangeScene()
    {
        GameManager.Instance.SceneController.CurrentScene = "Prototype";
    }

    public void Logout(bool ask = true)
    {
        if (ask)
            GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to logout?", async () =>
            {
                GameManager.Instance.NoBGLoading.SetActive(true);
                if (matchmakingController.currentRunnerInstance != null)
                    await matchmakingController.ShutdownServer();

                userData.ResetLogin();
                GameManager.Instance.SocketMngr.LogoutAndDisconnect();
                GameManager.Instance.SceneController.CurrentScene = "Login";
                GameManager.Instance.NoBGLoading.SetActive(false);
            }, null);
        else
            AwaitLogout();
    }

    private async void AwaitLogout()
    {
        GameManager.Instance.NoBGLoading.SetActive(true);
        if (matchmakingController.currentRunnerInstance != null)
            await matchmakingController.ShutdownServer();

        userData.ResetLogin();
        GameManager.Instance.SocketMngr.LogoutAndDisconnect();
        GameManager.Instance.SceneController.CurrentScene = "Login";
        GameManager.Instance.NoBGLoading.SetActive(false);
    }

    public void ButtonPress() => GameManager.Instance.AudioController.PlaySFX(buttonClip);

    public void TitleChecker()
    {
        for (int a = 0; a < titles.Count; a++)
            titles[a].SetActive(false);

        //Debug.Log("fck 2");
        if (userData.PlayerInventory.Count > 0)
        {
            var filteredItems = userData.PlayerInventory
                .Where(kvp => kvp.Value.type == "title" && kvp.Value.isEquipped == true)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (filteredItems.Count > 0)
            {
                Debug.Log(filteredItems.ElementAt(0).Value.itemname);
                int index = titlesName.IndexOf(filteredItems.ElementAt(0).Value.itemname);

                Debug.Log(index);
                titles[index].SetActive(true);
            }
        }
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
    public float playtime;
}