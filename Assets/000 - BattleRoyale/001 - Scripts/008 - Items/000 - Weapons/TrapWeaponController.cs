using Fusion;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TrapWeaponController : NetworkBehaviour
{
    [SerializeField] private float detectRadius;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private AudioClip trapPlaced;
    [SerializeField] private AudioClip trapActivate;
    [SerializeField] private AudioClip hitEffect;
    [SerializeField] private AudioSource trapAudioSource;

    [Space]
    [SerializeField] private Transform firstTeethTF;
    [SerializeField] private Transform secondTeethTF;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public string SpawnedBy { get; set; }
    [field: SerializeField][Networked] public bool CloseTrap { get; set; }
    [field: SerializeField][Networked] public Vector3 Rotation { get; set; }
    [field: SerializeField][Networked] public Vector3 Position { get; set; }
    [field: SerializeField][Networked] public bool CanDamage { get; set; }

    //  ====================

    private ChangeDetector _changeDetector;
    private readonly List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();

    //  ====================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority) return;

        trapAudioSource.PlayOneShot(trapPlaced);
    }

    public void Initialize(string spawnedby, Vector3 position, Vector3 rotation)
    {
        SpawnedBy = spawnedby;
        Position = position;
        Rotation = rotation;
        Invoke(nameof(DamageStart), 3f);
    }

    public override void Render()
    {
        if (Object.IsValid && !HasStateAuthority)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(CloseTrap):
                        if (!CloseTrap) return;

                        trapAudioSource.PlayOneShot(trapActivate);
                        trapAudioSource.PlayOneShot(hitEffect);

                        LeanTween.rotateX(firstTeethTF.gameObject, 60f, 0.25f).setEase(LeanTweenType.easeInOutSine);
                        LeanTween.rotateX(secondTeethTF.gameObject, -60f, 0.25f).setEase(LeanTweenType.easeInOutSine);

                        break;
                }
            }

            transform.rotation = Quaternion.Euler(Rotation);

            transform.position = Position;
        }
    }

    public override void FixedUpdateNetwork()
    {
        DetectPlayer();
    }

    private void DamageStart() => CanDamage = true;

    private void DetectPlayer()
    {
        if (!Object.IsValid) return;

        if (!CanDamage) return;

        if (CloseTrap) return;

        int hitCount = Runner.LagCompensation.OverlapSphere(
            transform.position,
            detectRadius,
            Object.InputAuthority,
            hitsFirstFist,
            enemyLayerMask,
            HitOptions.SubtickAccuracy
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hitsFirstFist[i].Hitbox;

            if (hitbox == null)
                continue;

            GameObject hitObject = hitbox.transform.root.gameObject;

            if (hitObject == null)
                continue;

            if (!HasStateAuthority) return;

            if (hitObject.tag == "Bot")
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();

                if (tempdata.IsStagger) return;
                if (tempdata.IsGettingUp) return;
                if (tempdata.IsDead) return;

                CanDamage = false;

                tempdata.IsHit = true;

                tempdata.ApplyDamage(30f, SpawnedBy, hitObject.GetComponent<NetworkObject>());

                Invoke(nameof(DespawnObject), 2f);

                CloseTrap = true;

                break;
            }
            else
            {
                PlayerHealthV2 health = hitObject.GetComponent<PlayerHealthV2>();

                if (health.IsStagger) return;
                if (health.IsGettingUp) return;
                if (health.IsDead) return;

                CanDamage = false;

                health.ApplyDamage(30f, SpawnedBy, hitObject.GetComponent<NetworkObject>());

                Invoke(nameof(DespawnObject), 2f);

                CloseTrap = true;

                break;
            }
        }
    }

    private void DespawnObject()
    {
        Debug.Log($"Start despawning. Is it valid ? {Object == null}");
        if (Object == null) return;

        Runner.Despawn(Object);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
