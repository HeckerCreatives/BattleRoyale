using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    [field: Header("SCRIPT REFERENCE")]
    [field: SerializeField] public MovementData PlayerMovementData { get; private set; }
    [field: SerializeField] public PlayerEnvironment Environment { get; private set; }
    [field: SerializeField] public GameplayController GameplayController { get; private set; }

    //  ==============================

    //  ==============================

    public PlayerStateChanger Changer { get; private set; }
    public IdleState Idle { get; private set; }
    public MoveState Move { get; private set; }

    //  ==============================

    private void Awake()
    {
        Changer = new PlayerStateChanger();
        Idle = new IdleState(Changer, PlayerMovementData, this, GameplayController, Environment, "idle");
        Move = new MoveState(Changer, PlayerMovementData, this, GameplayController, Environment, "run");

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
}
