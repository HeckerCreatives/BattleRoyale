using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class UpperWithAimState : UpperBodyAnimations
{
    public UpperWithAimState(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();
        //playerPlayables.aimWeights.RigBuilderSetter(true);
        playerPlayables.SetLookAtWeight(1f);
    }

    public override void NetworkLocalUpdate()
    {
        base.NetworkLocalUpdate();
        //playerPlayables.aimWeights.HipsWeight(1f);
        playerPlayables.SetLookAtWeight(1f);
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        //playerPlayables.aimWeights.HipsWeight(1f);
        playerPlayables.cameraRotation.HandleCameraAimInput();
        playerPlayables.SetLookAtWeight(1f);
    }
}
