using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistWeaponHandler : NetworkBehaviour
{
    [SerializeField] private PlayerNetworkLoader loader;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private BareHandsMovement bareHandsMovement;

    [Space]
    [SerializeField] private Vector3 attackRadius;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

    //  ================

    private HashSet<NetworkObject> hitEnemiesFirstFist = new HashSet<NetworkObject>();
    private HashSet<NetworkObject> hitEnemiesSecondFist = new HashSet<NetworkObject>();
    private List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();
    private List<LagCompensatedHit> hitsSecondFist = new List<LagCompensatedHit>();

    //  ================

    public void PerformFirstAttack()
    {
        if (!HasStateAuthority) return;

        int hitCount = Runner.LagCompensation.OverlapBox(impactFirstFistPoint.position, attackRadius, Quaternion.identity, Object.InputAuthority, hitsFirstFist, enemyLayerMask, HitOptions.SubtickAccuracy);

        for (int i = 0; i < hitCount; i++)
        {
            NetworkObject hitObject = hitsFirstFist[i].Hitbox?.transform.root.GetComponent<NetworkObject>();

            if (hitObject == Object) continue;

            // Ensure we only hit once per enemy per attack sequence
            if (hitObject != null && !hitEnemiesFirstFist.Contains(hitObject))
            {
                float tempdamage = 0;

                switch (hitsFirstFist[i].Hitbox.tag)
                {
                    case "Head":
                        tempdamage = 30f;
                        break;
                    case "Body":
                        tempdamage = 25f;
                        break;
                    case "Thigh":
                        tempdamage = 20f;
                        break;
                    case "Shin":
                        tempdamage = 15f;
                        break;
                    case "Foot":
                        tempdamage = 10f;
                        break;
                    case "Arm":
                        tempdamage = 20f;
                        break;
                    case "Forearm":
                        tempdamage = 15f;
                        break;
                }

                Debug.Log($"Fist fist Damage by {loader.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {hitsFirstFist[i].Hitbox.tag}: {tempdamage}");

                // Process the hit (apply damage, etc.)
                HandleHit(hitObject, tempdamage);

                // Mark this enemy as hit
                hitEnemiesFirstFist.Add(hitObject);
            }
        }
    }

    public void PerformSecondAttack()
    {
        if (!HasStateAuthority) return;

        int hitCount = Runner.LagCompensation.OverlapBox(impactSecondFistPoint.position, attackRadius, Quaternion.identity, Object.InputAuthority, hitsSecondFist, enemyLayerMask, HitOptions.SubtickAccuracy);

        for (int i = 0; i < hitCount; i++)
        {
            NetworkObject hitObject = hitsSecondFist[i].Hitbox?.transform.root.GetComponent<NetworkObject>();

            if (hitObject == Object) continue;

            // Ensure we only hit once per enemy per attack sequence
            if (hitObject != null && !hitEnemiesSecondFist.Contains(hitObject))
            {
                float tempdamage = 0;

                switch (hitsSecondFist[i].Hitbox.tag)
                {
                    case "Head":
                        tempdamage = 30f;
                        break;
                    case "Body":
                        tempdamage = 25f;
                        break;
                    case "Thigh":
                        tempdamage = 20f;
                        break;
                    case "Shin":
                        tempdamage = 15f;
                        break;
                    case "Foot":
                        tempdamage = 10f;
                        break;
                    case "Arm":
                        tempdamage = 20f;
                        break;
                    case "Forearm":
                        tempdamage = 15f;
                        break;
                }

                Debug.Log($"Second Fist Damage by {loader.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {hitsSecondFist[i].Hitbox.tag}: {tempdamage}");

                // Process the hit (apply damage, etc.)
                HandleHit(hitObject, tempdamage);

                // Mark this enemy as hit
                hitEnemiesSecondFist.Add(hitObject);
            }
        }
    }


    private void HandleHit(NetworkObject enemy, float damage)
    {
        // Implement your logic for handling a hit here, e.g., apply damage

        PlayerHealth health = enemy.GetComponent<PlayerHealth>();

        health.ReduceHealth(damage, loader.Username, Object);
    }

    public void ResetFirstAttack()
    {
        hitEnemiesFirstFist.Clear();
    }

    public void ResetSecondAttack()
    {
        hitEnemiesSecondFist.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(impactFirstFistPoint.position, attackRadius);


        Gizmos.color = Color.red;
        Gizmos.DrawCube(impactSecondFistPoint.position, attackRadius);
    }
}
