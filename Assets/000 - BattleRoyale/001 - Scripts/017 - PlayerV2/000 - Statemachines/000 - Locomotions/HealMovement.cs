using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.Animations;
using Fusion.Addons.SimpleKCC;

public class HealMovement : NetworkBehaviour
{
    [SerializeField] private PlayerPlayables playerPlayables;
    [SerializeField] private SimpleKCC simpleKCC;
    [SerializeField] private PlayerMovementV2 playerMovementV2;

    [Space]
    [SerializeField] private AvatarMask upperBodyMask;

    [Space]
    [SerializeField] private List<string> animationnames;
    [SerializeField] private List<string> mixernames;

    [Space]
    [SerializeField] private AnimationClip heal;
    [SerializeField] private AnimationClip repair;

    //  ===================

    public AnimationMixerPlayable mixerPlayable;

    public HealState HealPlayable { get; private set; }

    //  ===================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 3);

        var healClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, heal);
        var repairClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, repair);

        playerPlayables.playableGraph.Connect(healClip, 0, mixerPlayable, 1);

        return mixerPlayable;
    }
}
