using Fusion;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SafeZoneController : NetworkBehaviour
{
    public List<Vector3> safeZoneShrinkSize;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public Vector3 SpawnPosition { get; set; }
    [field: SerializeField][Networked] public int ShrinkSizeIndex { get; set; }
    [field: SerializeField][Networked] public Vector3 CurrentShrinkSize { get; set; }
    [field: SerializeField][Networked] public Vector3 CurrentShrink { get; set; }
    [field: SerializeField][Networked] public Vector3 StartShrinkSize { get; set; }
    [field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    public void InitializeSafeZone()
    {
        transform.position = SpawnPosition;

        transform.localScale = safeZoneShrinkSize[0];
        CurrentShrinkSize = safeZoneShrinkSize[0];
        StartShrinkSize = safeZoneShrinkSize[0];
        CurrentShrink = safeZoneShrinkSize[ShrinkSizeIndex];
    }

    public override void Render()
    {
        if (HasStateAuthority) return;

        if (ServerManager == null) return;

        transform.position = SpawnPosition;
        transform.localScale = CurrentShrinkSize;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (ServerManager == null) return;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (ServerManager.CurrentSafeZoneState == SafeZoneState.SHRINK)
        {
            if (Vector3.Distance(transform.localScale, safeZoneShrinkSize[ShrinkSizeIndex]) <= 30f)
            { 
                ServerManager.SafeZoneTimer = 30f;

                if (ShrinkSizeIndex < safeZoneShrinkSize.Count - 1)
                {
                    ServerManager.CurrentSafeZoneState = SafeZoneState.TIMER;
                    StartShrinkSize = safeZoneShrinkSize[ShrinkSizeIndex];
                    ShrinkSizeIndex++;
                    CurrentShrink = safeZoneShrinkSize[ShrinkSizeIndex];
                }
                else
                {
                    ServerManager.CurrentSafeZoneState = SafeZoneState.NONE;
                }
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, safeZoneShrinkSize[ShrinkSizeIndex], Runner.DeltaTime * 0.02f);
                CurrentShrinkSize = transform.localScale;
            }
        }
    }
}
