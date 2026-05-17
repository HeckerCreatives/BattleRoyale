using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Place on an empty GameObject at each building door threshold (ground level).
/// The bot navigation system queries these to find a reachable exit when the bot
/// is inside a building and needs to reach an outside destination.
/// </summary>
public class BuildingExit : MonoBehaviour
{
    private static readonly List<BuildingExit> _all = new List<BuildingExit>();

    public static IReadOnlyList<BuildingExit> All => _all;

    public Vector3 Position => transform.position;

    private void OnEnable()  => _all.Add(this);
    private void OnDisable() => _all.Remove(this);

    /// <summary>
    /// Returns the nearest BuildingExit whose NavMesh position is reachable from
    /// <paramref name="agentPosition"/> via a complete path.
    /// Returns null if none are reachable (agent is already outside or NavMesh is disconnected).
    /// </summary>
    public static BuildingExit FindNearest(NavMeshAgent agent, Vector3 agentPosition)
    {
        BuildingExit best = null;
        float bestDist = float.MaxValue;

        foreach (var exit in _all)
        {
            float d = Vector3.Distance(agentPosition, exit.Position);
            if (d >= bestDist) continue;

            if (!agent.isOnNavMesh) continue;

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(exit.Position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                best = exit;
                bestDist = d;
            }
        }

        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, 0.4f);

        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, "Exit");
    }
#endif
}
