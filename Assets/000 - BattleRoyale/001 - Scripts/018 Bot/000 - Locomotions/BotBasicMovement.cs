using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BotBasicMovement : NetworkBehaviour
{
    [SerializeField] private SimpleKCC simpleKCC;
    [SerializeField] private BotPlayables botPlayables;
    [SerializeField] private BotMovementController botMovement;

    [Space]
    [SerializeField] private List<string> animationnames;
    [SerializeField] private List<string> mixernames;

    //  ================

    public AnimationMixerPlayable mixerPlayable;

    //  =================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(botPlayables.playableGraph, 2);

        //var idleClip = AnimationClipPlayable.Create(botPlayables.playableGraph, idle);

        //botPlayables.playableGraph.Connect(idleClip, 0, mixerPlayable, 1);

        //IdlePlayable = new IdleState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false);

        return mixerPlayable;
    }


    public BotAnimationPlayable GetPlayableAnimation(int index)
    {
        switch (index)
        {
            //case 1:
            //    return IdlePlayable;
            default: return null;
        }
    }
}
