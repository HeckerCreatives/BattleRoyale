using Fusion;
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

    [Space]
    [SerializeField] private GameObject mapIndicator;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public string SpawnedBy { get; set; }
    [field: SerializeField][Networked] public bool CloseTrap { get; set; }
    [field: SerializeField][Networked] public Vector3 Rotation { get; set; }
    [field: SerializeField][Networked] public Vector3 Position { get; set; }
    [field: SerializeField][Networked] public bool CanDamage { get; set; }

    //  ====================

    private ChangeDetector _changeDetector;
    private static readonly Collider[] _overlapBuffer = new Collider[8];

    //  ====================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        mapIndicator.SetActive(HasInputAuthority);

        if (HasStateAuthority) return;

        trapAudioSource.PlayOneShot(trapPlaced);
    }

    public void Initialize(string spawnedby, Vector3 position, Vector3 rotation)
    {
        SpawnedBy = spawnedby;
        Position = position;
        Rotation = rotation;
        Invoke(nameof(StartDetection), 3f);
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

    private void StartDetection()
    {
        if (!HasStateAuthority) return;
        CanDamage = true;
        InvokeRepeating(nameof(DetectPlayer), 0f, 0.2f);
    }

    private void DetectPlayer()
    {
        if (!HasStateAuthority || CloseTrap)
        {
            CancelInvoke(nameof(DetectPlayer));
            return;
        }

        float radiusSq = detectRadius * detectRadius;
        Vector3 trapPos = transform.position;

        foreach (PlayerHealthV2 health in PlayerHealthV2.All)
        {
            if (health.Object == null) continue;
            if ((health.transform.position - trapPos).sqrMagnitude > radiusSq) continue;
            if (health.IsStagger || health.IsGettingUp || health.IsDead) continue;

            CanDamage = false;
            CancelInvoke(nameof(DetectPlayer));
            health.ApplyDamage(30f, SpawnedBy, health.Object, HitWeaponType.Trap);
            Invoke(nameof(DespawnObject), 2f);
            CloseTrap = true;
            return;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(trapPos, detectRadius, _overlapBuffer, enemyLayerMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitCount; i++)
        {
            GameObject hitObject = _overlapBuffer[i].transform.root.gameObject;
            if (hitObject == null || !hitObject.CompareTag("Bot")) continue;

            Botdata tempdata = hitObject.GetComponent<Botdata>();
            if (tempdata == null) continue;
            if (tempdata.IsStagger || tempdata.IsGettingUp || tempdata.IsDead) continue;

            CanDamage = false;
            CancelInvoke(nameof(DetectPlayer));
            tempdata.IsHit = true;
            tempdata.ApplyDamage(30f, SpawnedBy, hitObject.GetComponent<NetworkObject>(), HitWeaponType.Trap);
            Invoke(nameof(DespawnObject), 2f);
            CloseTrap = true;
            return;
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
