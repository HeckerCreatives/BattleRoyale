using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpEnd : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //animator.GetComponent<PlayerController>().ResetAnimationJump();
    }
}
