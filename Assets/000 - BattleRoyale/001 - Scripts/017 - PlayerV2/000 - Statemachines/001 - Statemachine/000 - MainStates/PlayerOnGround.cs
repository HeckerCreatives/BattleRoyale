using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerOnGround : AnimationPlayable
{
    public PlayerOnGround(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    protected bool IsUpperBodyAiming()
    {
        var upper = playerPlayables.upperBodyMovement;
        var current = playerPlayables.upperBodyChanger.CurrentState;
        return current == upper.BowDrawArrowPlayable
            || current == upper.BowChargePlayable
            || current == upper.BowShotPlayable
            || current == upper.RifleAimPlayable
            || current == upper.RifleShootPlayable
            || current == upper.RifleCockingPlayable
            || current == upper.RifleReloadPlayable;
    }

    protected bool IsUpperBodyCockingOrReloading()
    {
        var upper = playerPlayables.upperBodyMovement;
        var current = playerPlayables.upperBodyChanger.CurrentState;
        return current == upper.RifleCockingPlayable || current == upper.RifleReloadPlayable;
    }
}
