using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : NetworkBehaviour
{
    [SerializeField] private float startingStamina;
    [SerializeField] private Slider staminaSlider;

    [field: Header("NETWORK DEBUGGER")]
    [field: SerializeField][Networked] public float Stamina { get; set; }
    [field: SerializeField][Networked] public float RecoverTime { get; set; }
    [field: SerializeField][Networked] public bool CanRecover { get; set; }

    public override void Spawned()
    {
        Stamina = 100f;
    }

    public override void Render()
    {
        staminaSlider.value = Stamina / startingStamina;
    }

    public void RecoverStamina(float speed)
    {
        if (Stamina >= startingStamina) return;

        Stamina += speed * Runner.DeltaTime;
    }

    public void DecreaseStamina(float speed)
    {
        if (Stamina <= 0f) return;

        Stamina -= speed * Runner.DeltaTime;
    }

    public void ReduceStamina(float reduceAmount)
    {
        Stamina -= reduceAmount;
    }
}
