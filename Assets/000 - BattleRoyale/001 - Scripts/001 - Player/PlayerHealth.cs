using Fusion;
using Fusion.Addons.SimpleKCC;
using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private KillCountCounterController killCounterController;
    [SerializeField] private PlayerGameOverScreen gameOverScreen;
    [SerializeField] private PlayerNetworkLoader loader;
    [SerializeField] private UserData userData;
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private float startingHealth;
    [SerializeField] private float startingArmor;

    [Space]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;

    [Space]
    [SerializeField] private Image damageIndicator;

    [Space]
    [SerializeField] private DeathMovement deathMovement;

    [field: Header("DEBUGGER")]
    [Networked] [field: SerializeField] public float CurrentHealth { get; set; }
    [Networked][field: SerializeField] public float CurrentArmor { get; set; }
    [Networked][field: SerializeField] public bool Heal { get; set; }


    [field: Space]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float PlayerFallDamage { get; set; }
    public NetworkDictionary<int, string> Items { get; } = new NetworkDictionary<int, string>();

    //  =========================

    private ChangeDetector _changeDetector;

    int damageIndicatorLT;

    //  =========================

    public override void Spawned()
    {
        InitializeHealth();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        if (playerInventory.Shield != null)
        {
            armorSlider.value = playerInventory.Shield.Ammo / 100f;
        }

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentHealth):

                    if (!HasInputAuthority) return;

                    healthSlider.value = CurrentHealth / 100f;

                    if (damageIndicatorLT != 0) LeanTween.cancel(damageIndicatorLT);

                    damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 255f), 0.12f).setEase(LeanTweenType.easeInOutSine).setOnComplete(() =>
                    {
                        damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 0), 5f).setEase(LeanTweenType.easeInSine).setDelay(2f).id;
                    }).id;

                    break;
                case nameof(CurrentArmor):

                    if (!HasInputAuthority) return;

                    armorSlider.value = CurrentArmor / 100f;
                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        CircleDmage();
        FallDamage();
    }

    private void FallDamage()
    {
        if (!HasStateAuthority) return;

        if (CurrentHealth <= 0) return;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (characterController.RealVelocity.y <= -20f)
            PlayerFallDamage = Mathf.Abs(characterController.RealVelocity.y) - 5;

        if (characterController.IsGrounded && PlayerFallDamage > 0)
        {
            CurrentHealth -= PlayerFallDamage;
            PlayerFallDamage = 0f;

            if (CurrentHealth <= 0)
            {
                deathMovement.MakePlayerDead();
                gameOverScreen.PlayerPlacement = ServerManager.RemainingPlayers.Count;
                ServerManager.RemainingPlayers.Remove(Object.InputAuthority);
                RPC_ReceiveKillNotification($"{loader.Username} DEATH BY FALL");

                if (playerInventory.PrimaryWeapon != null)
                {
                    playerInventory.PrimaryWeapon.DropWeapon();
                    playerInventory.PrimaryWeapon = null;
                }

                if (playerInventory.Shield != null)
                {
                    playerInventory.Shield.DropShield();
                    playerInventory.Shield = null;
                }
            }
        }
    }

    private void CircleDmage()
    {
        if (!HasStateAuthority) return;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (!ServerManager.DonePlayerBattlePositions) return;

        if (CurrentHealth <= 0) return;

        float distanceFromCenter = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(ServerManager.SafeZone.transform.position.x, 0, ServerManager.SafeZone.transform.position.z));
        float circleRadius = ServerManager.SafeZone.CurrentShrinkSize.x / 2; // Adjust based on your implementation

        if (distanceFromCenter > circleRadius)
        {
            CurrentHealth -= Runner.DeltaTime * ((ServerManager.SafeZone.ShrinkSizeIndex + 1) / 2);
        }

        if (CurrentHealth <= 0)
        {
            deathMovement.MakePlayerDead();
            gameOverScreen.PlayerPlacement = ServerManager.RemainingPlayers.Count;
            ServerManager.RemainingPlayers.Remove(Object.InputAuthority);
            RPC_ReceiveKillNotification($"{loader.Username} WAS KILLED OUTSIDE SAFE ZONE");

            if (playerInventory.PrimaryWeapon != null)
            {
                playerInventory.PrimaryWeapon.DropWeapon();
                playerInventory.PrimaryWeapon = null;
            }

            if (playerInventory.Shield != null)
            {
                playerInventory.Shield.DropShield();
                playerInventory.Shield = null;
            }
        }
    }

    private void InitializeHealth()
    {
        if (HasStateAuthority)
        {
            CurrentHealth = startingHealth;
            CurrentArmor = startingArmor;
        }

        if (HasInputAuthority)
        {
            healthSlider.value = CurrentHealth / 100f;
            armorSlider.value = CurrentArmor / 100f;
        }
    }

    public void ReduceHealth(float damage, string killer, NetworkObject nobject)
    {
        if (CurrentHealth <= 0) return;

        //if (ServerManager.CurrentGameState != GameState.ARENA) return;

        float remainingDamage = damage;

        // Apply damage to the shield first
        if (playerInventory.Shield != null && playerInventory.Shield.Ammo > 0)
        {
            if (playerInventory.Shield.Ammo >= remainingDamage)
            {
                playerInventory.Shield.Ammo -= (int)remainingDamage;
                remainingDamage = 0; // Shield absorbed all damage
            }
            else
            {
                remainingDamage -= playerInventory.Shield.Ammo;
                playerInventory.Shield.Ammo = 0; // Shield fully depleted
            }
        }

        // Apply remaining damage to health
        if (remainingDamage > 0)
        {
            CurrentHealth = (byte)Mathf.Max(0, CurrentHealth - remainingDamage);
            nobject.GetComponent<PlayerGameOverScreen>().HitPoints += remainingDamage;
        }

        // Check if player is dead
        if (CurrentHealth <= 0)
        {
            deathMovement.MakePlayerDead();
            nobject.GetComponent<KillCountCounterController>().KillCount++;
            gameOverScreen.PlayerPlacement = ServerManager.RemainingPlayers.Count;
            ServerManager.RemainingPlayers.Remove(Object.InputAuthority);
            RPC_ReceiveKillNotification($"{killer} KILLED {loader.Username}");

            if (playerInventory.PrimaryWeapon != null)
            {
                playerInventory.PrimaryWeapon.DropWeapon();
                playerInventory.PrimaryWeapon = null;
            }

            if (playerInventory.Shield != null)
            {
                playerInventory.Shield.DropShield();
                playerInventory.Shield = null;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ReceiveKillNotification(string killer)
    {
        Debug.Log($"Receive Kill Notif by: {loader.Username}");

        if (HasStateAuthority) return;

        KillNotificationController.KillNotifInstance.ShowMessage(killer);
    }
}
