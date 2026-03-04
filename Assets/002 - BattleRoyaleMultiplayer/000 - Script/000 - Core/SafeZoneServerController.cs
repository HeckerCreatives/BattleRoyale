using Fusion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public class SafeZoneServerController : NetworkBehaviour
{
    public static SafeZoneServerController Instance { get; private set; }

    //==========================

    public bool DoneSetupSafeZone { get => doneSetupSafeZone; }


    private event EventHandler SafeZoneStateChange;
    public event EventHandler OnSafeZoneStateChange
    {
        add
        {
            if (SafeZoneStateChange == null || !SafeZoneStateChange.GetInvocationList().Contains(value))
                SafeZoneStateChange += value;
        }
        remove { SafeZoneStateChange -= value; }
    }

    //  =======================

    [Header("SAFE ZONE")]
    [SerializeField] private NetworkObject safeZoneNO;
    [SerializeField] private List<Vector3> safeZonePosition;
    [SerializeField] private List<ShrinkSizeList> safeZoneShrink;

    [Header("DEBUGGER")]
    [SerializeField] private bool doneSetupSafeZone;

    [field: Header("NETWORK DEBUGGER")]
    [field: SerializeField][Networked] public SafeZoneState CurrentSafeZoneState { get; set; }
    [field: SerializeField][Networked] public float SafeZoneTimer { get; set; }
    [field: SerializeField][Networked] public SafeZoneController SafeZone { get; set; }

    //  =========================

    private ChangeDetector _changeDetector;

    //  =========================

    private void Awake()
    {
        Instance = this;
    }

    public override async void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        Debug.Log("change detector initialized on dedicated server local player");
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentSafeZoneState):
                    SafeZoneStateChange?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        CountDownSafeZoneTimer();
    }

    public async void SetSafeZoneArea()
    {
        while (!Runner) await Task.Yield();

        int rand = UnityEngine.Random.Range(0, safeZonePosition.Count);

        SafeZone = Runner.Spawn(safeZoneNO, safeZonePosition[rand], Quaternion.identity, Object.StateAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {

            obj.GetComponent<SafeZoneController>().safeZoneShrinkSize = safeZoneShrink[rand].shrinksizes;
            obj.GetComponent<SafeZoneController>().SpawnPosition = safeZonePosition[rand];
            obj.GetComponent<SafeZoneController>().ShrinkSizeIndex = 1;
            //obj.GetComponent<SafeZoneController>().ServerManager = this;
            obj.GetBehaviour<SafeZoneController>().InitializeSafeZone();

        }).GetComponent<SafeZoneController>();

        doneSetupSafeZone = true;
    }

    private void CountDownSafeZoneTimer()
    {
        if (!HasStateAuthority) return;

        if (CurrentSafeZoneState != SafeZoneState.TIMER) return;

        SafeZoneTimer -= Runner.DeltaTime;

        if (SafeZoneTimer <= 0f)
        {
            CurrentSafeZoneState = SafeZoneState.SHRINK;
        }
    }
}
