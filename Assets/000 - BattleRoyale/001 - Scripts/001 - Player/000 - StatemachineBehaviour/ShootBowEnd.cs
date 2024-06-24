using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootBowEnd : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<PlayerInventory>().Reload();
        animator.GetComponent<PlayerController>().ResetAttack();
    }
}
