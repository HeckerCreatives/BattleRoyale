using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRifleShoot : UpperWithAimState
{
    float timer;

    public PlayerUpperRifleShoot(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;

        playerPlayables.inventory.SecondaryWeapon.SoundController.PlayGunshot();

        if (playerPlayables.HasStateAuthority)
            playerPlayables.inventory.SecondaryWeapon.SpawnBullet(playerMovement.CameraHitOrigin, playerMovement.CameraHitDirection, animationLength);

        playerPlayables.inventory.SecondaryWeapon.Supplies -= 1;
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
            if (playerPlayables.inventory.SecondaryWeapon.Supplies > 0)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleCockingPlayable);
            }
            else
            {
                if (playerPlayables.inventory.RifleMagazine > 0)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleReloadPlayable);
                else
                {
                    if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                    {
                        if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleSprintPlayable);
                        else
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleRunPlayable);
                    }
                    else
                    {
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleIdle);
                    }
                }
            }
        }
    }
}
