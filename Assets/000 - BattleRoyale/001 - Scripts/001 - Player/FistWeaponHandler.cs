using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class FistWeaponHandler : NetworkBehaviour
{
    [SerializeField] private PlayerNetworkLoader loader;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private BareHandsMovement bareHandsMovement;

    [Space]
    [SerializeField] private float attackRadius;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

    [Space]
    [SerializeField] private MeleeSoundController punchSoundController;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public int Hit { get; set; }

    //  ================

    [Networked, Capacity(10)] public NetworkLinkedList<NetworkObject> hitEnemiesFirstFist { get; } = new NetworkLinkedList<NetworkObject>();
    [Networked, Capacity(10)] public NetworkLinkedList<NetworkObject> hitEnemiesSecondFist { get; } = new NetworkLinkedList<NetworkObject>();
    private readonly List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> hitsSecondFist = new List<LagCompensatedHit>();

    private ChangeDetector _changeDetector;

    //  ================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        if (!HasStateAuthority)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(Hit):
                        punchSoundController.PlayHit();
                        break;
                }
            }
        }
    }

    public void PerformFirstAttack()
    {
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
            var hitbox = hitsFirstFist[i].Hitbox;
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null)
            {
                continue;
            }

            // Avoid duplicate hits
            if (!hitEnemiesFirstFist.Contains(hitObject))
            {
                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 30f,
                    "Body" => 25f,
                    "Thigh" => 20f,
                    "Shin" => 15f,
                    "Foot" => 10f,
                    "Arm" => 20f,
                    "Forearm" => 15f,
                    _ => 0f
                };

                Debug.Log($"First Fist Damage by {loader.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                // Process the hit
                HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesFirstFist.Add(hitObject);

                Hit++;
            }
        }
    }


    public void PerformSecondAttack()
    {
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
            var hitbox = hitsSecondFist[i].Hitbox;
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null)
            {
                continue;
            }

            // Avoid duplicate hits
            if (!hitEnemiesSecondFist.Contains(hitObject))
            {
                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 30f,
                    "Body" => 25f,
                    "Thigh" => 20f,
                    "Shin" => 15f,
                    "Foot" => 10f,
                    "Arm" => 20f,
                    "Forearm" => 15f,
                    _ => 0f
                };

                Debug.Log($"Second Fist Damage by {loader.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                // Process the hit
                HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesSecondFist.Add(hitObject);

                Hit++;
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
