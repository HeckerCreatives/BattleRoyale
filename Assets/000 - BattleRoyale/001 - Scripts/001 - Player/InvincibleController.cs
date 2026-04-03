using Fusion;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using UnityEngine;

public class InvincibleController : NetworkBehaviour
{
    [SerializeField] private ParticleSystem invincibleParticles;

    [field: Header("NETWORK")]
    [field: SerializeField][Networked] public bool IsInvincible { get; set; }
    [field: SerializeField][Networked] public bool DoneInit { get; set; }
    [field: SerializeField][Networked] public float InvincibleTimer { get; set; }

    //  ====================

    private ChangeDetector _changeDetector;

    //  ====================

    public override void Spawned()
    {
        CheckPlayOnStart();

        if (!HasStateAuthority)
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            return;
        }

        InvincibleTimer = Runner.SimulationTime + 30;
        IsInvincible = true;
        DoneInit = true;
    }

    public override void Render()
    {
        if (HasStateAuthority) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsInvincible):

                    if (IsInvincible) break;

                    invincibleParticles.Stop();

                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!IsInvincible) return;

        if (Runner.SimulationTime < InvincibleTimer) return;

        IsInvincible = false;
    }

    private async void CheckPlayOnStart()
    {
        if (HasStateAuthority) return;

        while (!DoneInit) await Task.Yield();

        Debug.Log($"DONE INIT: {DoneInit}   Invincible: {IsInvincible}");

        if (IsInvincible)
            invincibleParticles.Play();
        else
            invincibleParticles.Stop();
    }

    public void DisableInvincible()
    {
        if (!HasStateAuthority) return;

        if (!IsInvincible) return;

        IsInvincible = false;
    }
}
