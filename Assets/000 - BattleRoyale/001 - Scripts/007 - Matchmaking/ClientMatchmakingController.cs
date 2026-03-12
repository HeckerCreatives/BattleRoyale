using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClientMatchmakingController : MonoBehaviour
{
    public enum PlayState
    {
        NORMAL,
        RANKED
    }
    private event EventHandler PlayStateChange;
    public event EventHandler OnPlayStateChange
    {
        add
        {
            if (PlayStateChange == null || !PlayStateChange.GetInvocationList().Contains(value))
                PlayStateChange += value;
        }
        remove { PlayStateChange -= value; }
    }

    //  =====================

    private event EventHandler ServerChange;
    public event EventHandler OnServerChange
    {
        add
        {
            if (ServerChange == null || ServerChange.GetInvocationList().Contains(value))
                ServerChange += value;
        }
        remove { ServerChange -= value; }
    }
    public void AddServerSelected(string server)
    {
        if (serverSelected.Contains(server)) return;

        serverSelected.Add(server);
    }
    public void RemoveServerSelected(string server)
    {
        if (!serverSelected.Contains(server)) return;

        serverSelected.Remove(server);
    }
    public List<string> ServerSelected
    {
        get => serverSelected;
    }

    public PlayState CurrentPlayState
    {
        get => currentPlayState;
        set
        {
            currentPlayState = value;
            PlayStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    //  ========================

    [SerializeField] private UserData userData;
    [SerializeField] private string lobbyName;
    [SerializeField] private bool useMultiplay;
    [SerializeField] private bool usePrivateServer;
    [SerializeField] private Button changeServerBtn;
    [SerializeField] private Button changeServerBtn1;

    [Space]
    [SerializeField] private NetworkRunner instanceRunner;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private GameObject matchBtn;
    [SerializeField] private GameObject matchmakingObj;
    [SerializeField] private GameObject reconObj;

    [Space]
    [SerializeField] private GameObject serverListLoader;
    [SerializeField] private GameObject serverListContent;

    [Space]
    [SerializeField] private GameObject waitingRoomObj;
    [SerializeField] private TextMeshProUGUI waitingRoomTimerTMP;
    [SerializeField] private List<WaitingPlayerItem> waitingPlayerItems;

    [Space]
    [SerializeField] private RectTransform playSettingsRT;
    [SerializeField] private LeanTweenType easeType;
    [SerializeField] private float easeDuration;
    [SerializeField] private GameObject rankedMatchBtn;
    [SerializeField] private GameObject rankedMatchSettings;
    [SerializeField] private GameObject normalMatchBtn;
    [SerializeField] private GameObject normalMatchSettings;

    [Space]
    [SerializeField] private GameObject enteringMatchObj;
    [SerializeField] private TextMeshProUGUI enteringMatchTimerTMP;
    [SerializeField] private RectTransform enteringMatchTimerRT;
    [SerializeField] private RectTransform enteringMatchFlashRT;
    [SerializeField] private UnityEngine.UI.Image enteringMatchFlashImg;
    [SerializeField] private GameObject lobbyObj;
    [SerializeField] private GameObject lobbyWaitingObj;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] public NetworkRunner currentRunnerInstance;
    [ReadOnly][SerializeField] private float currentTime;
    [ReadOnly][SerializeField] private bool findingMatch;
    [ReadOnly][SerializeField] private bool matchFound;
    [ReadOnly][SerializeField] int minutesFindMatch;
    [ReadOnly][SerializeField] int secondsFindMatch;
    [SerializeField] private List<string> serverSelected;
    [SerializeField] private string roomname;
    [SerializeField] private bool finishCheckingIfCanRecon;
    [SerializeField] private bool playSettingIsOn;
    [SerializeField] private PlayState currentPlayState;
    [SerializeField] private bool inWaitingRoom;
    [SerializeField] private bool isBattle;
    [SerializeField] private float currentWaitingTime;
    [SerializeField] private bool reconToWaiting;
    [SerializeField] private float enteringMatchTimer;

    //  =====================

    CreateTicketResponse ticketResponse;

    Coroutine matching;

    int playLT, enteringMatchTMPLT, enteringMatchFlash;

    //  =====================

    private void Awake()
    {
        ChangePlayStates();
        OnPlayStateChange += PlayStateChanged;
    }

    private void OnEnable()
    {
        GameManager.Instance.SocketMngr.Socket.On("reconnectfail", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                finishCheckingIfCanRecon = true;
            });
        });

        GameManager.Instance.SocketMngr.Socket.On("reconnectexist", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                finishCheckingIfCanRecon = true;

                List<WaitingRoomData> tempdata = JsonConvert.DeserializeObject<List<WaitingRoomData>>(response.ToString());

                if (tempdata == null) return;

                reconToWaiting = true;

                roomname = tempdata[0].roomName;

                Debug.Log($"status: {tempdata[0].status}");

                isBattle = tempdata[0].status == "BATTLE" ? true : false;

                inWaitingRoom = tempdata[0].status == "WAITING" ? true : false;

                currentWaitingTime = tempdata[0].countdown;

                reconObj.SetActive(true);

                matchBtn.SetActive(false);

                for (int a = 0; a < waitingPlayerItems.Count; a++)
                {
                    if (a < tempdata[0].players.Count)
                        waitingPlayerItems[a].SetData(tempdata[0].players[a], true);
                    else
                        waitingPlayerItems[a].SetData("", false);
                }
            });
        });

        GameManager.Instance.SocketMngr.Socket.On("doneremovereconnect", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                GameManager.Instance.NoBGLoading.SetActive(false);

                reconObj.SetActive(false);

                matchBtn.SetActive(true);
            });
        });

        GameManager.Instance.SocketMngr.Socket.On("enteringmatch", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                StartCoroutine(EnteringMatchTimer());
            });
        });

        GameManager.Instance.SocketMngr.Socket.On("matchstatuschanged", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                Debug.Log(response);

                List<WaitingRoomData> tempdata = JsonConvert.DeserializeObject<List<WaitingRoomData>>(response.ToString());

                GameManager.Instance.SocketMngr.EmitEvent("ack", new { eventName = "matchstatuschanged", messageId = tempdata[0].messageId });

                Debug.Log($"Match Status: {response}");

                currentWaitingTime = tempdata[0].countdown;

                timerTMP.text = "MATCH FOUND";

                cancelBtn.interactable = false;

                isBattle = tempdata[0].status == "BATTLE" ? true : false;

                if (!inWaitingRoom)
                {
                    GameManager.Instance.NoBGLoading.SetActive(true);

                    StartCoroutine(GameManager.Instance.PostRequest("/usergamedetail/useenergy", "", new Dictionary<string, object>(), false, (response) =>
                    {
                        if (userData.GameDetails.energy > 0)
                        {
                            userData.GameDetails.energy -= 2;
                            userData.EnergyChangeFireEvent();
                        }

                        inWaitingRoom = true;
                        matchFound = true;

                        roomname = tempdata[0].roomName;

                        if (!reconToWaiting)
                            waitingRoomObj.SetActive(true);

                    }, null));
                }

                for (int a = 0; a < waitingPlayerItems.Count; a++)
                {
                    if (a < tempdata[0].players.Count)
                        waitingPlayerItems[a].SetData(tempdata[0].players[a], true);
                    else
                        waitingPlayerItems[a].SetData("", false);
                }
            });
        });

        if (usePrivateServer)
        {
            //GameManager.Instance.SocketMngr.Socket.On("matchfound", (response) =>
            //{
            //    Debug.Log(response.ToString());
            //    GameManager.Instance.AddJob(() => roomname = response.GetValue<string>());
            //    GameManager.Instance.AddJob(StartMatchFinding);
            //});

            //GameManager.Instance.SocketMngr.Socket.On("matchstatuschanged", (response) =>
            //{
            //    string tempresponse = response.ToString();

            //    Debug.Log(tempresponse);

            //    GameManager.Instance.AddJob(() =>
            //    {
            //        if (matchFound) return;

            //        List<Dictionary<string, string>> tempdata = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(tempresponse);

            //        if (tempdata.Count <= 0) return;

            //        if (tempdata[0]["status"] != "WAITING") return;

            //        GameManager.Instance.AddJob(() => roomname = tempdata[0]["roomName"]);
            //        GameManager.Instance.AddJob(StartMatchFinding);
            //    });
            //});
        }
    }

    private void OnDisable()
    {
        OnPlayStateChange -= PlayStateChanged;
    }

    private void PlayStateChanged(object sender, EventArgs e)
    {
        ChangePlayStates();
    }

    private void Update()
    {
        FindMatchTimer();

        if (inWaitingRoom && currentWaitingTime > 0.0f)
        {
            currentWaitingTime -= Time.deltaTime;

            float minutes = Mathf.FloorToInt(currentWaitingTime / 60);

            float seconds = Mathf.FloorToInt(currentWaitingTime % 60);

            waitingRoomTimerTMP.text = string.Format("{0:00} : {1:00}", minutes, seconds);
        }
        else if (inWaitingRoom && currentWaitingTime <= 0.0f)
            waitingRoomTimerTMP.text = string.Format("{0:00} : {1:00}", 0f, 0f);
    }

    private IEnumerator EnteringMatchTimer()
    {
        currentRunnerInstance = Instantiate(instanceRunner);

        lobbyWaitingObj.SetActive(false);
        lobbyObj.SetActive(false);

        //currentRunnerInstance.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = CancelMatch;

        enteringMatchTimerTMP.text = $"{10:n0}";
        enteringMatchTimer = 10f;

        GameManager.Instance.NotificationController.CloseConfirmationAction(false);
        GameManager.Instance.NotificationController.CloseErrorAction(false);
        GameManager.Instance.NotificationController.CloseCongratsOkAction(false);

        enteringMatchObj.SetActive(true);


        #region TIMER TMP

        enteringMatchTimerTMP.color = UnityEngine.Color.white;

        enteringMatchTMPLT = LeanTween.value(enteringMatchTimerRT.gameObject, 0.1f, 1f, 0.3f).setEase(easeType).setOnUpdate((float val) =>
        {
            enteringMatchTimerRT.localScale = new Vector3(val, val);
        }).setOnComplete(() =>
        {
            enteringMatchTMPLT = LeanTween.value(enteringMatchTimerRT.gameObject, 1f, 0f, 0.6f).setEase(easeType).setOnUpdate((float val) =>
            {
                enteringMatchTimerTMP.color = new UnityEngine.Color(enteringMatchTimerTMP.color.r, enteringMatchTimerTMP.color.g, enteringMatchTimerTMP.color.b, val);
            }).id;
        }).id;

        #endregion

        #region FLASH

        enteringMatchFlashImg.color = new UnityEngine.Color(1f, 1f, 1f, 1f);

        enteringMatchFlashRT.localScale = new Vector3(0f, 0f, 0f);

        enteringMatchFlash = LeanTween.value(enteringMatchFlashRT.gameObject, 0f, 1f, 0.1f).setEase(easeType).setOnUpdate((float val) =>
        {
            enteringMatchFlashRT.localScale = new Vector3(val, val, val);
        }).setOnComplete(() =>
        {
            enteringMatchFlash = LeanTween.value(enteringMatchTimerRT.gameObject, 1f, 0f, 0.25f).setEase(easeType).setOnUpdate((float val) =>
            {
                enteringMatchFlashImg.color = new UnityEngine.Color(enteringMatchFlashImg.color.r, enteringMatchFlashImg.color.g, enteringMatchFlashImg.color.b, val);
            }).id;
        }).id;

        #endregion

        yield return new WaitForSecondsRealtime(1f);

        while (enteringMatchTimer > 0f)
        {
            if (enteringMatchTimer <= 0f) break;

            if (enteringMatchTMPLT > 0) LeanTween.cancel(enteringMatchTMPLT);
            if (enteringMatchFlash > 0) LeanTween.cancel(enteringMatchFlash);

            #region TIMER TMP

            enteringMatchTimerTMP.color = UnityEngine.Color.white;

            enteringMatchTMPLT = LeanTween.value(enteringMatchTimerRT.gameObject, 0.1f, 1f, 0.3f).setEase(easeType).setOnUpdate((float val) =>
            {
                enteringMatchTimerRT.localScale = new Vector3(val, val);
            }).setOnComplete(() =>
            {
                enteringMatchTMPLT = LeanTween.value(enteringMatchTimerRT.gameObject, 255f, 0f, 0.6f).setEase(easeType).setOnUpdate((float val) =>
                {
                    enteringMatchTimerTMP.color = new UnityEngine.Color(enteringMatchTimerTMP.color.r, enteringMatchTimerTMP.color.g, enteringMatchTimerTMP.color.b, val);
                }).id;
            }).id;

            #endregion

            #region FLASH

            enteringMatchFlashImg.color = new UnityEngine.Color(1f, 1f, 1f, 1f);

            enteringMatchFlashRT.localScale = new Vector3(0f, 0f, 0f);

            enteringMatchFlash = LeanTween.value(enteringMatchFlashRT.gameObject, 0f, 1f, 0.1f).setEase(easeType).setOnUpdate((float val) =>
            {
                enteringMatchFlashRT.localScale = new Vector3(val, val, val);
            }).setOnComplete(() =>
            {
                enteringMatchFlash = LeanTween.value(enteringMatchTimerRT.gameObject, 1f, 0f, 0.25f).setEase(easeType).setOnUpdate((float val) =>
                {
                    enteringMatchFlashImg.color = new UnityEngine.Color(enteringMatchFlashImg.color.r, enteringMatchFlashImg.color.g, enteringMatchFlashImg.color.b, val);
                }).id;
            }).id;

            #endregion

            enteringMatchTimer -= 1f;

            enteringMatchTimerTMP.text = $"{enteringMatchTimer:n0}";

            yield return new WaitForSecondsRealtime(1f);
        }

        enteringMatchTimerTMP.color = UnityEngine.Color.white;

        enteringMatchTimerTMP.text = "0";

        GameManager.Instance.SceneController.MultiplayerScene = true;

        yield return new WaitForSecondsRealtime(6f);

        JoinDropballSession();
    }

    private void ChangePlayStates()
    {
        switch (CurrentPlayState)
        {
            case PlayState.NORMAL:
                normalMatchBtn.SetActive(false);
                normalMatchSettings.SetActive(true);

                rankedMatchSettings.SetActive(false);
                rankedMatchBtn.SetActive(true);

                break;
            case PlayState.RANKED:
                normalMatchBtn.SetActive(true);
                normalMatchSettings.SetActive(false);

                rankedMatchSettings.SetActive(true);
                rankedMatchBtn.SetActive(false);

                break;
        }
    }

    public void ChangePlayStateBtn(int index) => CurrentPlayState = (PlayState) index;

    public IEnumerator WaitForReconnectStatus()
    {
        GameManager.Instance.SocketMngr.EmitEvent("needtoreconnect", null);

        while (!finishCheckingIfCanRecon) yield return null;
    }

    private void FindMatchTimer()
    {
        if (!findingMatch) return;

        if (matchFound) return;

        currentTime += Time.deltaTime;

        minutesFindMatch = Mathf.FloorToInt(currentTime / 60);

        secondsFindMatch = Mathf.FloorToInt(currentTime % 60);

        timerTMP.text = string.Format("{0:00} : {1:00}", minutesFindMatch, secondsFindMatch);
    }

    private FusionAppSettings BuildCustomAppSetting(string region)
    {

        var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy(); ;

        appSettings.UseNameServer = true;
        //appSettings.AppVersion = appVersion;

        //if (string.IsNullOrEmpty(customAppID) == false)
        //{
        //    appSettings.AppIdFusion = customAppID;
        //}

        if (string.IsNullOrEmpty(region) == false)
        {
            appSettings.FixedRegion = region.ToLower();
        }

        // If the Region is set to China (CN),
        // the Name Server will be automatically changed to the right one
        // appSettings.Server = "ns.photonengine.cn";

        return appSettings;
    }

    public void FindMatch()
    {
        if (findingMatch) return;

        //changeServerBtn.interactable = false;
        //changeServerBtn1.interactable = false;

        GameManager.Instance.SocketMngr.DisconnectAction = () =>
        {
            ShutdownServer();
        };

        if (userData.GameDetails.energy <= 0)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Your energy is empty! You can still queue for the game but you won't gain any xp and points. Would you like to continue?", () => 
            {
                matching = StartCoroutine(Matching());
            }, null);
        }
        else
            matching = StartCoroutine(Matching());
    }

    IEnumerator Matching()
    {
        PlaySettingsOpener();

        matchmakingObj.SetActive(true);
        matchBtn.SetActive(false);

        //findABattleObj.SetActive(false);

        findingMatch = true;

        cancelBtn.interactable = true;

        if (usePrivateServer)
        {
            float rand = UnityEngine.Random.Range(3, 20);

            yield return new WaitForSecondsRealtime(rand);

            GameManager.Instance.SocketMngr.EmitEvent("findmatch", JsonConvert.SerializeObject(new Dictionary<string, string>()));
        }
        else
        {
            currentRunnerInstance = Instantiate(instanceRunner);
            roomname = "testing";
            StartMatchFinding();
        }
    }

    private void StartMatchFinding()
    {
        JoinDropballSession();
    }

    private async void JoinDropballSession()
    {
        cancelBtn.interactable = false;

        var sessionResult = await StartSimulation(currentRunnerInstance, GameMode.Client);

        if (sessionResult.Ok)
        {
            //timerTMP.text = "MATCH FOUND!";
            //matchFound = true;
            //findingMatch = false;
            //GameManager.Instance.SceneController.MultiplayerScene = true;
        }
        else
        {
            cancelBtn.interactable = true;
            Reconnect();
        }
    }

    public async void Reconnect()
    {
        if (!findingMatch)
            return;

        if (currentRunnerInstance != null)
        {
            await currentRunnerInstance.Shutdown(true);

            Destroy(currentRunnerInstance.gameObject);

            currentRunnerInstance = null;

            await Task.Delay(4000);

            //if (!findingMatch)
            //    return;

            currentRunnerInstance = Instantiate(instanceRunner);

            //currentRunnerInstance.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = CancelMatch;

            Debug.Log("REJOINING LOBBY");

            await JoinLobby(lobbyName);

            JoinDropballSession();
        }
    }

    public Task<StartGameResult> JoinLobby(string lobbyName)
    {
        return currentRunnerInstance.JoinSessionLobby(SessionLobby.Custom, lobbyName);
    }

    public Task<StartGameResult> StartSimulation(NetworkRunner runner, GameMode gameMode)
    {
        if (currentRunnerInstance != null)
        {
            SceneRef sceneRef = default;

            var scenePath = SceneManager.GetActiveScene().path;

            Debug.Log($"scene path: {scenePath}");

            scenePath = scenePath.Substring("Assets/".Length, scenePath.Length - "Assets/".Length - ".unity".Length);
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            if (sceneIndex >= 0)
            {
                sceneRef = SceneRef.FromIndex(5);
            }

            NetworkSceneInfo networkSceneInfo = new NetworkSceneInfo();

            if (sceneRef != null)
            {
                if (sceneRef.IsValid == true)
                {
                    networkSceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single, LocalPhysicsMode.None, true);
                }

                var appSettings = BuildCustomAppSetting(userData.SelectedServer);

                Debug.Log($"FINDING MATCH TO SERVER {userData.SelectedServer}");

                return runner.StartGame(new StartGameArgs()
                {
                    GameMode = gameMode,
                    SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                    Scene = sceneRef,
                    SessionName = roomname,
                    CustomPhotonAppSettings = appSettings,
                    ConnectionToken = Encoding.UTF8.GetBytes(userData.Username)
                });
            }
        }

        return null;
    }

    public void CancelFindingMatch()
    {
        if (matching != null) StopCoroutine(matching);

        currentTime = 0;

        findingMatch = false;
        matchFound = false;
        inWaitingRoom = false;

        cancelBtn.interactable = false;

        reconObj.SetActive(false);

        changeServerBtn.interactable = true;

        matchmakingObj.SetActive(false);
        matchBtn.SetActive(true);

        ShutdownServer();

        waitingRoomObj.SetActive(false);
    }

    public void CancelMatch()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to quit the room? You will lose your energy consumed.", () =>
        {
            currentWaitingTime = 0f;
            currentTime = 0f;

            findingMatch = false;
            matchFound = false;
            inWaitingRoom = false;

            cancelBtn.interactable = false;

            timerTMP.text = "Canceling matchmaking...";

            matchmakingObj.SetActive(false);
            matchBtn.SetActive(true);

            reconObj.SetActive(false);

            //findABattleObj.SetActive(true);

            changeServerBtn.interactable = true;

            //changeServerBtn1.interactable = true;

            waitingRoomObj.SetActive(false);

            ShutdownServer();

            GameManager.Instance.SocketMngr.EmitEvent("quitonmatch", new Dictionary<string, string>{
                { "roomname", roomname }
            });
        }, null);
    }

    public async Task ShutdownServer()
    {
        if (currentRunnerInstance == null) return;

        var tempRunner = currentRunnerInstance;

        if (ticketResponse != null)
            await MatchmakerService.Instance.DeleteTicketAsync(ticketResponse.Id);

        if (tempRunner == null)
        {
            matchmakingObj.SetActive(false);
            matchBtn.SetActive(true);
            return;
        }

        tempRunner.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = null;

        await tempRunner.Shutdown(true);

        Destroy(tempRunner.gameObject);
    }

    public async void ReconnectMatch()
    {
        if (inWaitingRoom)
        {
            waitingRoomObj.SetActive(true);
        }
        else if (isBattle)
        {
            GameManager.Instance.NoBGLoading.SetActive(true);

            currentRunnerInstance = Instantiate(instanceRunner);

            var sessionResult = await StartSimulation(currentRunnerInstance, GameMode.Client);

            if (sessionResult.Ok)
            {
                GameManager.Instance.SceneController.MultiplayerScene = true;

                GameManager.Instance.NoBGLoading.SetActive(false);
            }
            else
            {
                GameManager.Instance.NoBGLoading.SetActive(false);

                roomname = "";

                reconObj.SetActive(false);

                matchBtn.SetActive(true);

                GameManager.Instance.NotificationController.ShowError("Your current match is not available or already finished the match. Please queue up again!", null);

                ShutdownServer();
            }
        }
    }

    public void CancelReconnectMatch()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to leave the current match? You will lose the progress and any currency that you use.", () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);

            GameManager.Instance.SocketMngr.EmitEvent("removereconnect", null);
        }, null);
    }

    public void PlaySettingsOpener()
    {
        if (playLT != 0) LeanTween.cancel(playLT);

        playSettingIsOn = !playSettingIsOn;

        if (playSettingIsOn)
        {
            playLT = LeanTween.value(playSettingsRT.gameObject, playSettingsRT.anchoredPosition.x, 0f, easeDuration).setOnUpdate((float val) =>
            {
                playSettingsRT.anchoredPosition = new Vector3(val, playSettingsRT.anchoredPosition.y, 0f);
            }).setEase(easeType).id;
        }
        else
        {
            playLT = LeanTween.value(playSettingsRT.gameObject, playSettingsRT.anchoredPosition.x, 621f, easeDuration).setOnUpdate((float val) =>
            {
                playSettingsRT.anchoredPosition = new Vector3(val, playSettingsRT.anchoredPosition.y, 0f);
            }).setEase(easeType).id;
        }
    }
}

[System.Serializable]
public class WaitingRoomData
{
    public string messageId;
    public string roomName;
    public List<string> players;
    public int maxPlayers;
    public string status;
    public float countdown;
}