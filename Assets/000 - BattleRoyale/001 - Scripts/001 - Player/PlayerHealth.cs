using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
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

    [field: Header("DEBUGGER")]
    [Networked] [field: SerializeField] public float CurrentHealth { get; set; }
    [Networked][field: SerializeField] public float CurrentArmor { get; set; }


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
        while (!Runner) await Task.Delay(100);

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

                    if (CurrentHealth <= 0)
                    {
                        playerAnimator.SetTrigger("died");
                        RPC_DeadBoy();

                        await Task.Delay(1500);

                        controllerObj.SetActive(false);
                        pauseObj.SetActive(false);

                        winMessageObj.SetActive(false);
                        loseMessageObj.SetActive(true);
                        gameOverPanelObj.SetActive(true);
                    }

                    break;
                case nameof(CurrentArmor):

                    if (!HasInputAuthority) return;

                    armorSlider.value = CurrentArmor / 100f;
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

    public void ReduceHealth(float damage)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth = (byte)Mathf.Max(0, CurrentHealth - damage);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_DeadBoy()
    {
        playerAnimator.SetTrigger("died");
    }
}
