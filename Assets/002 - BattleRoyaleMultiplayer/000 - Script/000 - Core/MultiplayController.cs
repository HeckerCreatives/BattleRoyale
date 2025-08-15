using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
#if !UNITY_ANDROID && !UNITY_IOS
//using Unity.Services.Core;
//using Unity.Services.Matchmaker;
//using Unity.Services.Matchmaker.Models;
//using Unity.Services.Multiplay;
#endif
using UnityEngine;

public class MultiplayController : MonoBehaviour
{

    [SerializeField] private DedicatedServerManager serverManager;

#if !UNITY_ANDROID && !UNITY_IOS

    //  =====================

//    private const int multiplayServiceTimeout = 20000;

//    private IServerQueryHandler _serverQueryHandler;
//    private bool _sqpInitialized = false;

//    private float _sqpUpdateInterval = 0.1f; // Update every 100 miliseconds
//    private float _sqpTimer = 0f;

//    private string allocationID;
//    private MultiplayEventCallbacks _multiplayEventCallbacks;
//    private IServerEvents _serverEvents;

//    private BackfillTicket localBackfillTicket;
//    CreateBackfillTicketOptions backfillTicketOptions;
//    private const int ticketCheckMs = 1000;

//    private string externalIp = "0.0.0.0";
//    private ushort serverPort = 7777;
//    private string externalConnectionString => $"{externalIp}:{serverPort}";

//    private bool isCurrentlyBackfilling;

//    private bool canDeallocate;
//    private bool currentDeallocate;
//    private float timerForDeallocation;

//    //  ======================

//    MatchmakingResults MatchmakingResults;

//    //  ======================



//    private void Awake()
//    {
//        if (GameManager.Instance == null)
//        {
//            Debug.Log("THIS IS A SERVER MULTIPLAY CONTROLLER");
//            serverManager.OnPlayerCountChange += PlayerCountChange;
//        }
//    }

//    private void OnDisable()
//    {
//        if (GameManager.Instance == null)
//        {
//            serverManager.OnPlayerCountChange -= PlayerCountChange;
//        }
//    }

//    private void Update()
//    {
//        SqpUpdate();
//    }

//    private void PlayerCountChange(object sender, EventArgs e)
//    {
//        CheckBackfillWaitingAreaTimer();
//        CheckForDeallocationChangePlayer();
//    }

//    private void CheckBackfillWaitingAreaTimer()
//    {
//        if (serverManager.CurrentGameState == GameState.WAITINGAREA)
//        {
//            if (serverManager.WaitingAreaTimer > 60f)
//            {
//                if (isCurrentlyBackfilling)
//                {
//                    Debug.Log("Player Change but currently backfilling");
//                    return;
//                }

//                Debug.Log("Player Change but restart backfilling");
//                isCurrentlyBackfilling = true;

//#pragma warning disable 4014
//                StartBackfill(MatchmakingResults);
//#pragma warning restore 4014
//            }
//        }
//    }

//    private void CheckForDeallocationChangePlayer()
//    {
//        if (!currentDeallocate)
//        {
//            if (serverManager.CurrentGameState == GameState.WAITINGAREA)
//            {
//                if (serverManager.Players.Count > 0)
//                {
//                    if (canDeallocate)
//                    {
//                        Debug.Log("Stop deallocation becuase player change while in waiting area");
//                        canDeallocate = false;
//                    }
//                }
//                else
//                {
//                    if (!canDeallocate)
//                    {
//                        Debug.Log("Start deallocation becuase player change while in waiting area");
//                        timerForDeallocation = 20f;
//                        canDeallocate = true;

//#pragma warning disable 4014
//                        StartTimerForDeallocation();
//#pragma warning restore 4014
//                    }
//                }
//            }
//            else if (serverManager.CurrentGameState == GameState.ARENA)
//            {
//                if (serverManager.Players.Count <= 0)
//                {
//                    if (!canDeallocate)
//                    {
//                        Debug.Log("Start deallocation becuase player count change to 0 while in arena");
//                        timerForDeallocation = 20f;
//                        canDeallocate = true;

//#pragma warning disable 4014
//                        StartTimerForDeallocation();
//#pragma warning restore 4014
//                    }
//                }
//            }
//        }
//    }

//    public async Task InitializeUnityAuthentication()
//    {
//        Debug.Log("Starting unity authentication");
//        await UnityServices.InitializeAsync();

//        try
//        {
//            _serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync((ushort)10, "n/a", "n/a", "109854", "n/a");
//        }
//        catch(Exception ex)
//        {
//            Debug.Log($"Something went wrong trying to set up the SQP service: {ex}");
//        }

//        timerForDeallocation = 20f;
//        canDeallocate = true;

//#pragma warning disable 4014
//        StartTimerForDeallocation();
//#pragma warning restore 4014

//        try
//        {
//            MatchmakingResults = await GetMatchmakerPayload(multiplayServiceTimeout);

//            if (MatchmakingResults != null)
//            {
//                Debug.Log($"Got payload: {MatchmakingResults}");

//                _sqpInitialized = true;

//                await StartBackfill(MatchmakingResults);
//            }
//            else
//            {
//                Debug.LogWarning("Getting the matchmaker payload time out, starting with defaults");
//            }
//        }
//        catch(Exception ex)
//        {
//            Debug.Log($"Something went wrong trying to set up the Allocation and Backfill services: {ex}");
//        }
//    }

//    private async Task<MatchmakingResults> GetMatchmakerPayload(int timeout)
//    {
//        var matchmakerPayloadTask = SubscribeAndAwaitMatchmakerAllocation();
        
//        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(timeout)) == matchmakerPayloadTask)
//        {
//            return matchmakerPayloadTask.Result;
//        }

//        return null;
//    }

//    private async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
//    {
//        if (MultiplayService.Instance == null) return null;

//        allocationID = null;

//        _multiplayEventCallbacks = new MultiplayEventCallbacks();
//        _multiplayEventCallbacks.Allocate += OnAllocate;
//        _multiplayEventCallbacks.Deallocate += OnDeallocate;
//        _multiplayEventCallbacks.Error += OnError;
//        _serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(_multiplayEventCallbacks);

//        allocationID = await AwaitAllocationId();

//        var mmPayload = await GetMatchmakerPayloadAsync();

//        return mmPayload;
//    }

//    private async Task<string> AwaitAllocationId()
//    {
//        var serverConfig = MultiplayService.Instance.ServerConfig;
//        Debug.Log($"Server ID[{serverConfig.ServerId}], AllocationId[{serverConfig.AllocationId}], Port[{serverConfig.Port}], QueryPort[{serverConfig.QueryPort}], LogDirectory[{serverConfig.ServerLogDirectory}]");

//        externalIp = serverConfig.IpAddress;
//        serverPort = serverConfig.Port;

//        while (string.IsNullOrEmpty(allocationID))
//        {
//            var configID = serverConfig.AllocationId;

//            if (!string.IsNullOrEmpty(configID) && !string.IsNullOrEmpty(allocationID))
//            {
//                allocationID = configID;
//                break;
//            }

//            await Task.Delay(100);
//        }

//        return allocationID;
//    }

//    private async Task<MatchmakingResults> GetMatchmakerPayloadAsync()
//    {
//        try
//        {
//            var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

//            var modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);

//            Debug.Log($"{nameof(GetMatchmakerPayloadAsync)}: \n {modelAsJson}");

//            return payloadAllocation;
//        }
//        catch(Exception ex)
//        {
//            Debug.Log($"Something went wrong trying to set up the get matchmaker payload in GetMatchmakerPayloadAsync: {ex}");
//        }

//        return null;
//    }

//    private async Task StartBackfill(MatchmakingResults payload)
//    {
//        Debug.Log("Start Backfilling");
//        var backfillProperties = new BackfillTicketProperties(payload.MatchProperties);
//        localBackfillTicket = new BackfillTicket { Id = payload.MatchProperties.BackfillTicketId, Properties = backfillProperties };

//        await BeginBackfilling(payload);
//    }

//    private async Task BeginBackfilling(MatchmakingResults payload)
//    {
//        var matchProperties = payload.MatchProperties;

//        backfillTicketOptions = new CreateBackfillTicketOptions
//        {
//            Connection = externalConnectionString,
//            QueueName = payload.QueueName,
//            Properties = new BackfillTicketProperties(matchProperties)
//        };

//        localBackfillTicket = new BackfillTicket();
//        localBackfillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(backfillTicketOptions);

//        isCurrentlyBackfilling = true;

//        Debug.Log("Starting Backfill loop");
//#pragma warning disable 4014
//        BackfillLoop();

//#pragma warning restore 4014
//    }

//    private async Task BackfillLoop()
//    {
//        while (NeedsPlayers())
//        {
//            localBackfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(localBackfillTicket.Id);

//            if (!NeedsPlayers())
//            {
//                if (currentDeallocate)
//                    return;

//                await MatchmakerService.Instance.DeleteBackfillTicketAsync(localBackfillTicket.Id);
//                localBackfillTicket = null;
//                isCurrentlyBackfilling = false;

//                return;
//            }

//            await Task.Delay(ticketCheckMs);
//        }
//    }

//    private void SqpUpdate()
//    {
//        if (currentDeallocate)
//            return;

//        if (_sqpInitialized == true && serverManager.Runner && serverManager.HasStateAuthority)
//        {
//            _sqpTimer += Time.deltaTime;
//            if (_sqpTimer >= _sqpUpdateInterval)
//            {
//                _sqpTimer = 0f;

//                _serverQueryHandler.CurrentPlayers = (ushort)serverManager.Players.Count;// (ushort)Global.Networking.PeerCount;
//                _serverQueryHandler.UpdateServerCheck();
//            }
//        }
//    }

//    private bool NeedsPlayers()
//    {
//        if (currentDeallocate)
//            return false;

//        if (serverManager.CurrentGameState == GameState.WAITINGAREA)
//        {
//            if (serverManager.WaitingAreaTimer > 60)
//            {
//                if (serverManager.Players.Count < serverManager.Players.Capacity)
//                    return true;
//                else
//                    return false;
//            }
//            else
//                return false;
//        }
//        else
//            return false;
//    }

//    private void OnError(MultiplayError error)
//    {
//        LogServerConfig();
//    }

//    private void OnDeallocate(MultiplayDeallocation deallocation)
//    {
//        Debug.Log("Deallocated");

//        _multiplayEventCallbacks.Allocate -= OnAllocate;
//        _multiplayEventCallbacks.Deallocate -= OnDeallocate;
//        _multiplayEventCallbacks.Error -= OnError;

//        _serverEvents?.UnsubscribeAsync();

//        MatchmakingResults = null;

//        // Hack for now, just exit the application on deallocate
//        Application.Quit();
//    }

//    private void OnAllocate(MultiplayAllocation allocation)
//    {
//        if (string.IsNullOrEmpty(allocation.AllocationId)) return;

//        allocationID = allocation.AllocationId;

//        var serverConfig = MultiplayService.Instance.ServerConfig;

//        externalIp = serverConfig.IpAddress;
//        serverPort = serverConfig.Port;

//        LogServerConfig();
//    }

//    private void LogServerConfig()
//    {
//        var serverConfig = MultiplayService.Instance.ServerConfig;

//        Debug.Log($"Server ID[{serverConfig.ServerId}], AllocationId[{serverConfig.AllocationId}], Port[{serverConfig.Port}], QueryPort[{serverConfig.QueryPort}], LogDirectory[{serverConfig.ServerLogDirectory}]");
//    }

//    private async Task StartTimerForDeallocation()
//    {
//        while (canDeallocate)
//        {
//            timerForDeallocation -= Time.deltaTime;

//            if (timerForDeallocation <= 0)
//            {
//                currentDeallocate = true;

//                //  DEALLOCATION
//                await MatchmakerService.Instance.DeleteBackfillTicketAsync(localBackfillTicket.Id);
//                localBackfillTicket = null;
//                isCurrentlyBackfilling = false;

//                Application.Quit();

//                return;
//            }

//            await Task.Delay(100);
//        }
//    }

#endif
}
