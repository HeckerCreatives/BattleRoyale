using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDamageHandlerBox : NetworkBehaviour
{
    [SerializeField] private Vector3 attackRadius;
    [SerializeField] private Vector3 attackRotation;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private Transform impactPoint;

    [Space]
    [SerializeField] private float headDamage;
    [SerializeField] private float bodyDamge;
    [SerializeField] private float thighDamage;
    [SerializeField] private float shinDamage;
    [SerializeField] private float footDamage;
    [SerializeField] private float armDamage;
    [SerializeField] private float forearmDamage;

    //  ======================

    private HashSet<NetworkObject> hitenemies = new HashSet<NetworkObject>();
    private List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    //  ======================

    public void PerformFirstAttack(NetworkObject nobject)
    {
        if (!HasStateAuthority) return;

        // Use the runner to get lag-compensated overlap sphere results
        int hitCount = Runner.LagCompensation.OverlapBox(
                impactPoint.position,
                attackRadius,
                Object.transform.rotation,
                Object.InputAuthority,
                hits,
                enemyLayerMask,
                HitOptions.IgnoreInputAuthority
         );

        for (int i = 0; i < hitCount; i++)
        {
            NetworkObject hitObject = hits[i].Hitbox?.transform.root.GetComponent<NetworkObject>();

            Debug.Log($"hit enemy: {hitObject.name} {(hitObject != null && !hitenemies.Contains(hitObject))}");

            // Ensure we only hit once per enemy per attack sequence
            if (hitObject != null && !hitenemies.Contains(hitObject))
            {
                float tempdamage = 0;

                switch (hits[i].Hitbox.tag)
                {
                    case "Head":
                        tempdamage = headDamage;
                        break;
                    case "Body":
                        tempdamage = bodyDamge;
                        break;
                    case "Thigh":
                        tempdamage = thighDamage;
                        break;
                    case "Shin":
                        tempdamage = shinDamage;
                        break;
                    case "Foot":
                        tempdamage = footDamage;
                        break;
                    case "Arm":
                        tempdamage = armDamage;
                        break;
                    case "Forearm":
                        tempdamage = forearmDamage;
                        break;
                }

                Debug.Log($"Damge: {tempdamage}");

                // Process the hit (apply damage, etc.)
                HandleHit(hitObject, tempdamage, nobject);

                // Mark this enemy as hit
                hitenemies.Add(hitObject);
            }
        }
    }

    private void HandleHit(NetworkObject enemy, float damage, NetworkObject nobject)
    {
        // Implement your logic for handling a hit here, e.g., apply damage

        PlayerHealth health = enemy.GetComponent<PlayerHealth>();

        health.ReduceHealth(damage, nobject);
    }

    public void ResetFirstAttack()
    {
        hitenemies.Clear();
    }

    private void OnDrawGizmos()
    {
        if (impactPoint == null) return;

        // Save the current Gizmos matrix
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Create a new matrix with the position and rotation you want
        Gizmos.matrix = Matrix4x4.TRS(impactPoint.position, gameObject.transform.rotation, Vector3.one);

        // Set the color of the Gizmo
        Gizmos.color = Color.blue;

        // Draw the wireframe cube with the attackRadius dimensions
        Gizmos.DrawWireCube(Vector3.zero, attackRadius);

        // Restore the previous Gizmos matrix
        Gizmos.matrix = oldMatrix;
    }
}
