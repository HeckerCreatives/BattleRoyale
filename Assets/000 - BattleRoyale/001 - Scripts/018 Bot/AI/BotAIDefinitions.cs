/// <summary>Shared enums for modular bot cognition. Tactical playables consume strategic goals indirectly via <see cref="BotMovementController"/>.</summary>
public enum BotStrategicGoal
{
    /// <summary>Find weapons / heals / ammo on the floor.</summary>
    AcquireLoot,
    /// <summary>Weighted move toward populated fight centroid.</summary>
    HuntPlayers,
    /// <summary>Wide flank waypoint (same traversal as hunt, different rationale for future tuning).</summary>
    RotatePosition,
    /// <summary>Hold a ring position inside zone, watch centroid.</summary>
    HoldCamp,
    /// <summary>Break contact toward quiet vector + heals.</summary>
    Recover,
}
