using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperBowShot : UpperWithAimState
{
    float timer;

    public PlayerUpperBowShot(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;

        playerPlayables.inventory.SecondaryWeapon.SoundController.PlayGunshot();

        if (playerPlayables.HasStateAuthority)
            playerPlayables.inventory.SecondaryWeapon.SpawnBullet(playerMovement.CameraHitOrigin, playerMovement.CameraHitDirection, animationLength);

        if (playerPlayables.inventory.SecondaryWeapon.Supplies > 0)
            playerPlayables.inventory.SecondaryWeapon.Supplies -= 1;
        else if (playerPlayables.inventory.BowMagazine > 0)
            playerPlayables.inventory.BowMagazine -= 1;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);

        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
        }

        if (playerPlayables.TickRateAnimation >= timer)
        {
            if (playerMovement.Attacking && (playerPlayables.inventory.SecondaryWeapon.Supplies > 0 || playerPlayables.inventory.BowMagazine > 0))
            {
                // continue firing
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowDrawArrowPlayable);
            }
            else
            {

                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowIdlePlayable);
            }
        }
    }
}
