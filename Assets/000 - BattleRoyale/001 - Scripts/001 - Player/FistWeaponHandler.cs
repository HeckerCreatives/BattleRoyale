using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistWeaponHandler : NetworkBehaviour
{
    [SerializeField] private PlayerNetworkLoader loader;
    [SerializeField] private PlayerInventory inventory;

    [Space]
    [SerializeField] private float attackRadius;
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

        if (inventory.WeaponIndex != 1) return;

        // Use the runner to get lag-compensated overlap sphere results
        int hitCount = Runner.LagCompensation.OverlapSphere(
                impactFirstFistPoint.position,
                attackRadius,
                Object.InputAuthority,
                hitsFirstFist,
                enemyLayerMask,
                HitOptions.IgnoreInputAuthority
         );

        for (int i = 0; i < hitCount; i++)
        {
            NetworkObject hitObject = hitsFirstFist[i].Hitbox?.transform.root.GetComponent<NetworkObject>();

            Debug.Log($"{hitObject.name} {(hitObject != null && !hitEnemiesFirstFist.Contains(hitObject))}");

            // Ensure we only hit once per enemy per attack sequence
            if (hitObject != null && !hitEnemiesFirstFist.Contains(hitObject))
            {
                float tempdamage = 0;

                switch (hitsFirstFist[i].Hitbox.tag)
                {
                    case "Head":
                        tempdamage = 25f;
                        break;
                    case "Body":
                        tempdamage = 20f;
                        break;
                    case "Thigh":
                        tempdamage = 15f;
                        break;
                    case "Shin":
                        tempdamage = 10f;
                        break;
                    case "Foot":
                        tempdamage = 5f;
                        break;
                    case "Arm":
                        tempdamage = 15f;
                        break;
                    case "Forearm":
                        tempdamage = 10f;
                        break;
                }

                Debug.Log($"Damge: {tempdamage}");

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

        if (inventory.WeaponIndex != 1) return;

        // Use the runner to get lag-compensated overlap sphere results
        int hitCount = Runner.LagCompensation.OverlapSphere(
                impactSecondFistPoint.position,
                attackRadius,
                Object.InputAuthority,
                hitsSecondFist,
                enemyLayerMask,
                HitOptions.IgnoreInputAuthority
         );

        for (int i = 0; i < hitCount; i++)
        {
            NetworkObject hitObject = hitsSecondFist[i].Hitbox?.transform.root.GetComponent<NetworkObject>();

            // Ensure we only hit once per enemy per attack sequence
            if (hitObject != null && !hitEnemiesSecondFist.Contains(hitObject))
            {
                float tempdamage = 0;

                switch (hitsSecondFist[i].Hitbox.tag)
                {
                    case "Head":
                        tempdamage = 25f;
                        break;
                    case "Body":
                        tempdamage = 20f;
                        break;
                    case "Thigh":
                        tempdamage = 15f;
                        break;
                    case "Shin":
                        tempdamage = 10f;
                        break;
                    case "Foot":
                        tempdamage = 5f;
                        break;
                    case "Arm":
                        tempdamage = 15f;
                        break;
                    case "Forearm":
                        tempdamage = 10f;
                        break;
                }

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
        Gizmos.DrawWireSphere(impactFirstFistPoint.position, attackRadius);


        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(impactSecondFistPoint.position, attackRadius);
    }
}
