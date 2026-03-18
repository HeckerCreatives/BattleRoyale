using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class UpperNoAimState : UpperBodyAnimations
{
    public UpperNoAimState(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();
        //playerPlayables.aimWeights.RigBuilderSetter(false);

        playerMovement.CurrentAttackingEnabler(false);

        playerPlayables.SetLookAtWeight(0f);
    }

    public override void NetworkLocalUpdate()
    {
        base.NetworkLocalUpdate();
        //playerPlayables.aimWeights.HipsWeight(0f);
        playerPlayables.SetLookAtWeight(0f);    
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        //playerPlayables.aimWeights.HipsWeight(0f);
        playerPlayables.cameraRotation.HandleCameraNoAim();
        playerPlayables.SetLookAtWeight(0f);
    }
}
