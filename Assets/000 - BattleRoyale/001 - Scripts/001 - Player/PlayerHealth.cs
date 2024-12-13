using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private KillCountCounterController killCounterController;
    [SerializeField] private PlayerNetworkLoader loader;
    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private float startingHealth;
    [SerializeField] private float startingArmor;

    [Space]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;

    [Space]
    [SerializeField] private Image damageIndicator;

    [Space]
    [SerializeField] private Animator playerAnimator;

    [Space]
    [SerializeField] private GameObject controllerObj;
    [SerializeField] private GameObject pauseObj;
    [SerializeField] private GameObject gameOverPanelObj;
    [SerializeField] private GameObject winMessageObj;
    [SerializeField] private GameObject loseMessageObj;

    [Space]
    [SerializeField] private TextMeshProUGUI usernameResultTMP;
    [SerializeField] private TextMeshProUGUI playerCountResultTMP;
    [SerializeField] private TextMeshProUGUI rankResultTMP;
    [SerializeField] public TextMeshProUGUI killCountResultTMP;

    [field: Header("DEBUGGER")]
    [Networked] [field: SerializeField] public float CurrentHealth { get; set; }
    [Networked][field: SerializeField] public float CurrentArmor { get; set; }
    [Networked][field: SerializeField] public bool GetHit { get; set; }

    [field: Space]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int PlayerPlacement { get; set; }

    //  =========================

    private ChangeDetector _changeDetector;

    int damageIndicatorLT;

    //  =========================

    public override void Spawned()
    {
        InitializeHealth();

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override async void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentHealth):

                    if (HasInputAuthority)
                    {
                        healthSlider.value = CurrentHealth / 100f;
                    }


                    break;
                case nameof(PlayerPlacement):

                    if (HasInputAuthority)
                    {
                        Debug.Log($"Player lose, player placement: {PlayerPlacement}");
                        usernameResultTMP.text = userData.Username;
                        playerCountResultTMP.text = $"<color=yellow><size=\"55\">#{PlayerPlacement}</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity}</size>";
                        rankResultTMP.text = PlayerPlacement.ToString();
                        killCountResultTMP.text = killCounterController.KillCount.ToString();

                        controllerObj.SetActive(false);
                        pauseObj.SetActive(false);

                        winMessageObj.SetActive(false);
                        loseMessageObj.SetActive(true);
                        gameOverPanelObj.SetActive(true);
                    }

                    if (CurrentHealth <= 0)
                    {
                        playerAnimator.SetTrigger("died");
                    }

                    break;
                case nameof(CurrentArmor):

                    if (!HasInputAuthority) return;

                    armorSlider.value = CurrentArmor / 100f;
                    break;
                case nameof(GetHit):

                    if (HasInputAuthority && GetHit)
                    {
                        if (damageIndicatorLT != 0) LeanTween.cancel(damageIndicatorLT);

                        damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 255f), 0.12f).setEase(LeanTweenType.easeInOutSine).setOnComplete(() =>
                        {
                            damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 0), 5f).setEase(LeanTweenType.easeInSine).setDelay(2f).id;
                        }).id;
                    }

                    GetHit = false;
                    break;
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

        GetHit = true;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        CurrentHealth = (byte)Mathf.Max(0, CurrentHealth - damage);

        if (CurrentHealth <= 0)
        {
            PlayerPlacement = ServerManager.RemainingPlayers.Count;
            ServerManager.RemainingPlayers.Remove(Object.InputAuthority);
            nobject.GetComponent<KillCountCounterController>().KillCount++;
            RPC_ReceiveKillNotification(killer, loader.Username);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ReceiveKillNotification(string killer, string killed)
    {
        Debug.Log($"Receive Kill Notif by: {loader.Username}");

        KillNotificationController.KillNotifInstance.ShowMessage(killer, killed);
    }
}
