using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    [field: Header("SETTINGS")]
    [field: SerializeField] public Animator PlayerAnimator { get; private set; }

    [field: Header("SCRIPT REFERENCE")]
    [field: SerializeField] public MovementData PlayerMovementData { get; private set; }
    [field: SerializeField] public PlayerEnvironment Environment { get; private set; }
    [field: SerializeField] public GameplayController GameplayController { get; private set; }

    //  ==============================

    //  ==============================

    public PlayerStateChanger Changer { get; private set; }
    public AirState Air { get; private set; }
    public IdleState Idle { get; private set; }
    public MoveState Move { get; private set; }
    public JumpState Jump { get; private set; }
    public LandingState Landing { get; private set; }

    //  ==============================

    private void Awake()
    {
        Changer = new PlayerStateChanger();
        Idle = new IdleState(Changer, PlayerMovementData, PlayerAnimator, this, GameplayController, Environment, "idle");
        Move = new MoveState(Changer, PlayerMovementData, PlayerAnimator, this, GameplayController, Environment, "run");
        Air = new AirState(Changer, PlayerMovementData, PlayerAnimator, this, GameplayController, Environment, "inair");
        Landing = new LandingState(Changer, PlayerMovementData, PlayerAnimator, this, GameplayController, Environment, "landing");
        Jump = new JumpState(Changer, PlayerMovementData, PlayerAnimator, this, GameplayController, Environment, "jump");

        Changer.Initialize(Idle);
    }

    private void Update()
    {
        if (Changer != null)
            Changer.CurrentState.LogicUpdate();
    }

    private void FixedUpdate()
    {
        if (Changer != null)
            Changer.CurrentState.PhysicsUpdate();
    }

    public void TriggerAnimationExit()
    {
        if (Changer != null)
            Changer.CurrentState.AnimationFinishTrigger();
    }
}
