using Cinemachine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

public enum Ground
{
    DIRT,
    WOOD,
    STONE,
    WATER,
    TERRAIN,
    GRASS
}
public class PlayerPlayables : NetworkBehaviour
{
    private const int RECONCILE_DELAY_TICKS = 10;

    //  =====================

    public PlayerStamina stamina;
    public PlayerInventoryV2 inventory;
    public PlayerOwnObjectEnabler ownObjectEnabler;
    public PlayerCameraRotation cameraRotation;
    public MeleeSoundController fistSoundController;
    public PlayerMovementV2 playerMovementV2;

    [Space]
    [SerializeField] private CinemachineVirtualCamera vCam;
    [SerializeField] private CinemachineVirtualCamera aimVCam;
    [SerializeField] private CinemachineBasicMultiChannelPerlin vcamShaker;
    [SerializeField] private float changeFOVSpeed;

    [Space]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform playerObj;
    [SerializeField] private Transform bone;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 boneRotationCorrection;
    [SerializeField] private float maxPitchUp = 25f;
    [SerializeField] private float maxPitchDown = 20f;
    [SerializeField] private bool enableRoll = false;
    [SerializeField] private float rollDeg = 0f;   // set this from your input / leaning logic
    [SerializeField] private float maxRoll = 12f;

    [Space]
    public PlayerHealthV2 healthV2;
    public PlayerUpperMovement upperBodyMovement;
    public PlayerBasicMovement lowerBodyMovement;
    //public Transform muzzlePoint;
    [SerializeField] private ArrowController[] localArrowPool;
    [SerializeField] private BulletController[] localBulletPool;
    [SerializeField] private LayerMask arrowRaycastMask;
    [SerializeField] private PlayerNetworkLoader networkLoader;

    [Space]
    [SerializeField] private AvatarMask upperBodyMask;
    [SerializeField] private AvatarMask lowerBodyMask;

    [Space]
    public float enterSpeed;
    public float exitSpeed;

    [Space]
    [SerializeField] private SimpleKCC characterController;
    [SerializeField] private Transform groundDetector;
    [SerializeField] private LayerMask groundMask;

    [field: Space] 
    [field: SerializeField] public ParticleSystem WarpDrive { get; private set; }

    [Space]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] grassClip;
    [SerializeField] private AudioClip[] dirtClip;
    [SerializeField] private AudioClip[] stoneClip;
    [SerializeField] private AudioClip[] woodClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip rollClip;

    [Space]
    [SerializeField] private Image crosshairImg;
    [SerializeField] private Image animationProgress;
    [SerializeField] private TextMeshProUGUI status;

    [Space]
    [SerializeField] private CanvasGroup hitsPanelCG;
    [SerializeField] private TextMeshProUGUI hitsTextTMP;

    [Space]
    [SerializeField] private HitIndicatorController[] hitIndicatorPool;
    
    [field: Space]
    [field: SerializeField] public ParticleSystem[] SwordSlashes { get; private set; }
    [field: SerializeField] public ParticleSystem SwordImpact { get; private set; }
    [field: SerializeField] public ParticleSystem[] PunchSlashes { get; private set; }
    [field: SerializeField] public ParticleSystem PunchImpact { get; private set; }
    [field: SerializeField] public ParticleSystem[] SpearSlashes { get; private set; }

    [Header("DEBUGGER")]
    [SerializeField] private int _lastProcessedTickUpper = -1;
    [SerializeField] private int _lastProcessedTickLower = -1;
    [SerializeField] private float[] textureValues;
    [SerializeField] Vector3 terrainPosition;
    [SerializeField] Vector3 mapPosition;
    [SerializeField] float xCoord;
    [SerializeField] float zCoord;
    [SerializeField] int posX;
    [SerializeField] int posZ;
    [SerializeField] AudioClip selectedClip;
    [SerializeField] AudioClip previousClip;
    [SerializeField] private int audioClipIndex;
    [SerializeField] private float _upperMismatchTime;
    [SerializeField] private float _lowerMismatchTime;
    [SerializeField] private int _upperMismatchStartTick = -1;
    [SerializeField] private int _lowerMismatchStartTick = -1;
    [SerializeField] private int _pendingUpperTick = -1;
    [SerializeField] private int _pendingLowerTick = -1;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][field: SerializeField] public float TickRateAnimation { get; set; }
    [Networked][field: SerializeField] public int PlayableUpperBoddyAnimationIndex { get; set; }
    [Networked][field: SerializeField] public int PlayableLowerBoddyAnimationIndex { get; set; }
    [Networked][field: SerializeField] public int PlayableUpperBodyAnimationTick { get; set; }
    [Networked][field: SerializeField] public int PlayableLowerBodyAnimationTick { get; set; }
    [Networked][field: SerializeField] public string PlayableState { get; set; }
    [Networked][field: SerializeField] public bool FinalAttack { get; set; }
    [Networked][field: SerializeField] public Ground CurrentGround { get; set; }

    [Networked] public int ArrowFiredTick { get; set; }
    [Networked] public Vector3 ArrowStart { get; set; }
    [Networked] public Vector3 ArrowTarget { get; set; }
    [Networked] public int BulletFiredTick { get; set; }
    [Networked] public Vector3 BulletStart { get; set; }
    [Networked] public Vector3 BulletTarget { get; set; }
    [Networked] public Vector3 FireRayDbgOrigin { get; set; }
    [Networked] public Vector3 FireRayDbgDir { get; set; }

    //  =======================

    public PlayableGraph playableGraph;
    public UpperBodyChanger upperBodyChanger;
    public PlayablesChanger lowerBodyChanger;
    private UpperBodyAnimations _pendingUpperState;
    private AnimationPlayable _pendingLowerState;
    public AnimationLayerMixerPlayable finalMixer;
    public AnimationScriptPlayable lookAtPlayable;
    public LookAtJobBoneIK job { get; set; }
    LagCompensatedHit hit = new LagCompensatedHit();

    private ChangeDetector _changeDetector;

    private Vector3 rollAxisLocal;
    private Vector3 _currentPitchAxisLocal = Vector3.forward;
    private int _localArrowIndex;
    private int _lastArrowSpawnTick = -1;
    private int _localBulletIndex;
    private int _lastBulletSpawnTick = -1;
    private int _comboCount;
    private int _hitIndicatorIndex;

    int FovChanger;

    //Coroutine ShakerCoroutine;

    //  =======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);


        vcamShaker = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void OnEnable()
    {
        InitializePlayables();
    }

    private void OnDisable()
    {
        playableGraph.Destroy();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.V))
        {
            Debug.Log("DESTROY");
            playableGraph.Destroy();
        }
        else if (Input.GetKeyUp(KeyCode.B))
        {
            Debug.Log("RE INIT");
            InitializePlayables();
        }
    }

public override void Render()
    {
        if (lowerBodyChanger.CurrentState == null || upperBodyChanger.CurrentState == null) return;
        if (HasStateAuthority) return;

        upperBodyChanger.CurrentState.NetworkLocalUpdate();
        lowerBodyChanger.CurrentState.NetworkLocalUpdate();

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayableUpperBoddyAnimationIndex):
                case nameof(PlayableUpperBodyAnimationTick):

                    if (PlayableUpperBodyAnimationTick != _lastProcessedTickUpper)
                    {
                        _pendingUpperTick = PlayableUpperBodyAnimationTick;
                        _pendingUpperState = upperBodyMovement.GetPlayableAnimation(PlayableUpperBoddyAnimationIndex);
                        _upperMismatchStartTick = Runner.Tick;
                    }

                    break;

                case nameof(PlayableLowerBoddyAnimationIndex):
                case nameof(PlayableLowerBodyAnimationTick):

                    if (PlayableLowerBodyAnimationTick != _lastProcessedTickLower)
                    {
                        _pendingLowerTick = PlayableLowerBodyAnimationTick;
                        _pendingLowerState = lowerBodyMovement.GetPlayableAnimation(PlayableLowerBoddyAnimationIndex);
                        _lowerMismatchStartTick = Runner.Tick;
                    }

                    break;

                case nameof(ArrowFiredTick):
                    if (!HasInputAuthority)
                        SpawnLocalArrowAtPosition(ArrowStart, ArrowTarget);
                    break;

                case nameof(BulletFiredTick):
                    if (!HasInputAuthority)
                        SpawnLocalBulletAtPosition(BulletStart, BulletTarget);
                    break;
            }
        }

        ResolveUpperReplication();
        ResolveLowerReplication();
    }

    public override void FixedUpdateNetwork()
    {
        if (lowerBodyChanger.CurrentState == null || upperBodyChanger.CurrentState == null) return;

        upperBodyChanger.CurrentState.NetworkUpdate();
        lowerBodyChanger.CurrentState.NetworkUpdate();
    }

    private void ResolveUpperReplication()
    {
        if (_pendingUpperTick == -1)
            return;

        if (!HasInputAuthority)
        {
            upperBodyChanger.ChangeState(_pendingUpperState);
            _lastProcessedTickUpper = _pendingUpperTick;
            _pendingUpperTick = -1;
            return;
        }

        if (upperBodyChanger.CurrentState == _pendingUpperState)
        {
            _lastProcessedTickUpper = _pendingUpperTick;
            _pendingUpperTick = -1;
            _upperMismatchStartTick = -1;
            return;
        }

        if (Runner.Tick - _upperMismatchStartTick < RECONCILE_DELAY_TICKS)
            return;

        upperBodyChanger.ChangeState(_pendingUpperState);

        _lastProcessedTickUpper = _pendingUpperTick;
        _pendingUpperTick = -1;
        _upperMismatchStartTick = -1;
    }

    public void ChangeCamera(bool isAiming)
    {
        aimVCam.Priority = isAiming ? 11 : 0;
        vCam.Priority = isAiming ? 0 : 11;
    }

    public void ChangeFOV(float fovvalue)
    {
        if (FovChanger > 0) LeanTween.cancel(FovChanger);

        FovChanger = LeanTween.value(vCam.gameObject, vCam.m_Lens.FieldOfView, fovvalue, changeFOVSpeed).setEase(LeanTweenType.easeInOutCirc).setOnUpdate(val =>
        {
            vCam.m_Lens.FieldOfView = val;
        }).id;
    }

    private void ResolveLowerReplication()
    {
        if (_pendingLowerTick == -1)
            return;

        if (!HasInputAuthority)
        {
            lowerBodyChanger.ChangeState(_pendingLowerState);
            _lastProcessedTickLower = _pendingLowerTick;
            _pendingLowerTick = -1;
            return;
        }

        if (lowerBodyChanger.CurrentState == _pendingLowerState)
        {
            _lastProcessedTickLower = _pendingLowerTick;
            _pendingLowerTick = -1;
            _lowerMismatchStartTick = -1;
            return;
        }

        if (Runner.Tick - _lowerMismatchStartTick < RECONCILE_DELAY_TICKS)
            return;

        lowerBodyChanger.ChangeState(_pendingLowerState);

        _lastProcessedTickLower = _pendingLowerTick;
        _pendingLowerTick = -1;
        _lowerMismatchStartTick = -1;
    }

    public void HitAnimation()
    {
        upperBodyChanger.ChangeState(upperBodyMovement.HitPlayable, true);
        lowerBodyChanger.ChangeState(lowerBodyMovement.HitPlayable, true);
    }

    public void StaggerAnimation()
    {
        healthV2.IsStagger = true;
        upperBodyChanger.ChangeState(upperBodyMovement.StaggerHitPlayable, false);
        lowerBodyChanger.ChangeState(lowerBodyMovement.StaggerHitPlayable, false);
    }

    public void InitializePlayables()
    {
        // NOW create changers after mixers are valid
        lowerBodyChanger = new PlayablesChanger();
        upperBodyChanger = new UpperBodyChanger();

        playableGraph = PlayableGraph.Create("MyPlayableGraph");

        // Build your animation graph
        finalMixer = AnimationLayerMixerPlayable.Create(playableGraph, 2);

        // Initialize movement first so their mixers get created
        var lowerPlayable = lowerBodyMovement.Initialize();
        lowerBodyChanger.Initialize(lowerBodyMovement.IdlePlayable);
        var upperPlayable = upperBodyMovement.Initialize();
        upperBodyChanger.Initialize(upperBodyMovement.IdlePlayables);

        // Connect animation playables into the mixer
        playableGraph.Connect(lowerPlayable, 0, finalMixer, 0);
        finalMixer.SetInputWeight(0, 1f);

        playableGraph.Connect(upperPlayable, 0, finalMixer, 1);
        finalMixer.SetInputWeight(1, 1f);
        finalMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);


        Transform parentT = bone.parent;

        rollAxisLocal = PickLocalAxisClosestToWorldDir(bone, parentT.forward);

        job = new LookAtJobBoneIK
        {
            bone = playerAnimator.BindStreamTransform(bone),
            weight = 0f,
            pitchAxisLocal = Vector3.forward,
            pitchAxisSign = -1,
            pitchDeg = 0f
        };

        lookAtPlayable = AnimationScriptPlayable.Create(playableGraph, job);
        lookAtPlayable.SetInputCount(1);
        lookAtPlayable.ConnectInput(0, finalMixer, 0);
        lookAtPlayable.SetInputWeight(0, 1f);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", playerAnimator);
        playableOutput.SetSourcePlayable(lookAtPlayable);

        playableGraph.Play();
    }

    public void SlashSwordParticles(int index) => SwordSlashes[index].Play();

    public void SlashPunchParticles(int index) => PunchSlashes[index].Play();

    public void SlashPunchParticlesStop(int index)
    {
        if (PunchSlashes[index].isPlaying) PunchSlashes[index].Stop();
    }

    public void SlashSwordParticlesStop(int index)
    {
        if (SwordSlashes[index].isPlaying) SwordSlashes[index].Stop();
    }

    public void SlashSpearParticles(int index) => SpearSlashes[index].Play();

    public void SlashSpearParticlesStop(int index)
    {
        if (SpearSlashes[index].isPlaying) SpearSlashes[index].Stop();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySwordHit() => SwordImpact.Play();

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayPunchHit() => PunchImpact.Play();

    static Vector3 PickLocalAxisClosestToWorldDir(Transform bone, Vector3 desiredWorldDir)
    {
        Vector3[] axes =
        {
            Vector3.right, Vector3.up, Vector3.forward,
            -Vector3.right, -Vector3.up, -Vector3.forward
        };

        desiredWorldDir.Normalize();

        float best = -999f;
        Vector3 bestAxis = Vector3.forward;

        foreach (var a in axes)
        {
            Vector3 worldA = bone.TransformDirection(a).normalized;
            float d = Mathf.Abs(Vector3.Dot(worldA, desiredWorldDir));
            if (d > best)
            {
                best = d;
                bestAxis = a;
            }
        }
        return bestAxis;
    }

    public void SetLookAtWeight(float newWeight)
    {
        if (!lookAtPlayable.IsValid())
        {
            return;
        }

        if (Runner == null) return;

        var currentJob = lookAtPlayable.GetJobData<LookAtJobBoneIK>();
        currentJob.pitchAxisLocal = GetPitchAxisForCurrentState();
        currentJob.weight = newWeight;
        currentJob.pitchAxisSign = -1;
        currentJob.pitchDeg = -cameraRotation._cinemachineTargetPitch;
        currentJob.rotationCorrection = boneRotationCorrection;
        lookAtPlayable.SetJobData(currentJob);
    }

    private Vector3 GetPitchAxisForCurrentState()
    {
        var upperState = upperBodyChanger?.CurrentState;
        var lowerState = lowerBodyChanger?.CurrentState;

        bool isBowState = IsBowState(upperState) || IsBowState(lowerState);

        if (isBowState)
        {
            _currentPitchAxisLocal = Vector3.forward;
            return _currentPitchAxisLocal;
        }

        bool isRifleState = IsRifleState(upperState) || IsRifleState(lowerState);

        if (isRifleState)
        {
            _currentPitchAxisLocal = Vector3.right;
            return _currentPitchAxisLocal;
        }

        return _currentPitchAxisLocal;
    }

    private static bool IsBowState(object state)
    {
        return state is
            PlayerUpperBowIdle or PlayerUpperBowRun or PlayerUpperBowSprint or
            PlayerUpperBowDrawArrow or PlayerUpperBowCharge or PlayerUpperBowDraw or PlayerUpperBowShot or
            BowIdle or BowRun or BowSprint or BowDrawArrow or BowCharge or BowShot or BowDrawIdle or BowShootingMove;
    }

    private static bool IsRifleState(object state)
    {
        return state is
            PlayerUpperRifleIdle or PlayerUpperRifleRun or PlayerUpperRifleSprint or
            PlayerUpperRifleShoot or PlayerUpperRifleCocking or PlayerUpperRifleReload or PlayerUpperRifleAim or
            RifleIdleState or RifleRunState or RifleSprintState or RifleSShootState or
            RifleCockingState or RifleReloadState or RifleAimIdle or RifleAimMove;
    }

public void SetAnimationUpperTick() => PlayableUpperBodyAnimationTick = Runner.Tick;

    public void SetAnimationLowerTick() => PlayableLowerBodyAnimationTick = Runner.Tick;

    public void ShowReloadProgress()
    {
        animationProgress.gameObject.SetActive(true);
        animationProgress.fillAmount = 0f;
        status.gameObject.SetActive(true);
    }

    public void HideReloadProgress()
    {
        animationProgress.gameObject.SetActive(false);
        status.gameObject.SetActive(false);
    }

    public void SetReloadProgress(float value)
    {
        animationProgress.fillAmount = value;
    }

    public void FireBullet()
    {
        Transform rifleMuzzle = inventory.SecondaryWeapon?.ImpactPoint?.transform;

        if (HasStateAuthority)
        {
            if (BulletFiredTick == Runner.Tick) return;

            string killerName = networkLoader != null && !string.IsNullOrWhiteSpace(networkLoader.Username)
                ? networkLoader.Username
                : (ownObjectEnabler != null ? ownObjectEnabler.Username.ToString() : "PLAYER");

            Vector3 shooterOrigin = cameraRotation.ShooterOrigin;
            Vector3 aimPoint = playerMovementV2.AimPoint;

            // Aim-assist: snap to the locked enemy's chest if within tolerance.
            Vector3 aimChest = cameraRotation.GetAimAssistChestPosition();
            if (aimChest != Vector3.zero &&
                Vector3.Angle(aimPoint - shooterOrigin, aimChest - shooterOrigin) < 25f)
                aimPoint = aimChest;

            // Authoritative shot: deterministic origin -> the crosshair world
            // point the client resolved from the real camera. No camera needed
            // server-side, and no 3rd-person over-the-shoulder parallax.
            Ray ray = new Ray(shooterOrigin, (aimPoint - shooterOrigin).normalized);
            Vector3 rayStart = shooterOrigin;

            LagCompensatedHit bulletHit = new LagCompensatedHit();

            float aimRange = cameraRotation.AimDistance;
            Vector3 muzzlePos = rifleMuzzle != null ? rifleMuzzle.position : transform.position;
            Vector3 targetPos = muzzlePos + ray.direction * aimRange;
            bool bodyHit = false;
            bool hitSomething = false;

            int safetyLimit = 10;

            while (safetyLimit-- > 0)
            {
                if (!Runner.LagCompensation.Raycast(rayStart, ray.direction, 999f, Object.InputAuthority, out bulletHit, arrowRaycastMask, HitOptions.IncludePhysX))
                    break;

                NetworkObject hitObj = bulletHit.Hitbox?.Root.Object;
                if (hitObj != null && hitObj.InputAuthority == Object.InputAuthority)
                {
                    rayStart = bulletHit.Point + ray.direction * 0.5f;
                    continue;
                }

                if (Vector3.Distance(muzzlePos, bulletHit.Point) <= aimRange)
                {
                    targetPos = bulletHit.Point;
                    hitSomething = true;

                    if (bulletHit.Hitbox != null)
                    {
                        var enemyHealth = bulletHit.Hitbox.Root.GetBehaviour<PlayerHealthV2>();
                        if (enemyHealth != null)
                        {
                            string tag = bulletHit.Hitbox.tag;
                            float damage = tag switch
                            {
                                "Head"    => 60f,
                                "Body"    => 45f,
                                "Thigh"   => 35f,
                                "Shin"    => 30f,
                                "Foot"    => 25f,
                                "Arm"     => 40f,
                                "Forearm" => 30f,
                                _         => 0f
                            };
                            enemyHealth.ApplyDamage(damage, killerName, Object);
                            bodyHit = true;
                        }
                        else
                        {
                            var botHealth = bulletHit.Hitbox.Root.GetBehaviour<Botdata>();
                            if (botHealth != null && !botHealth.IsDead)
                            {
                                string tag = bulletHit.Hitbox.tag;
                                float damage = tag switch
                                {
                                    "Head"    => 60f,
                                    "Body"    => 45f,
                                    "Thigh"   => 35f,
                                    "Shin"    => 30f,
                                    "Foot"    => 25f,
                                    "Arm"     => 40f,
                                    "Forearm" => 30f,
                                    _         => 0f
                                };
                                botHealth.ApplyDamage(damage, killerName, Object);
                                bodyHit = true;
                            }
                        }
                    }
                }
                break;
            }

            inventory.SecondaryWeapon.Supplies = Mathf.Max(0, inventory.SecondaryWeapon.Supplies - 1);

            BulletStart = muzzlePos;
            BulletTarget = targetPos;
            BulletFiredTick = Runner.Tick;

            if (HasInputAuthority)
            {
                SpawnLocalBullet(rifleMuzzle, muzzlePos, targetPos, bodyHit, hitSomething);
                if (bodyHit)
                    cameraRotation.FlashDamageCrosshair();
            }
            else if (bodyHit)
                RPC_NotifyBulletHit();
        }
        else if (HasInputAuthority)
        {
            // Visual prediction: spawn from the muzzle toward the same crosshair
            // point, so the tracer matches the reticle (no parallax).
            Vector3 muzzlePos = inventory.SecondaryWeapon != null ? inventory.SecondaryWeapon.ImpactPoint.position : transform.position;
            Vector3 aimPoint = playerMovementV2.AimPoint;
            Vector3 dir = (aimPoint - muzzlePos).normalized;
            float dist = Vector3.Distance(muzzlePos, aimPoint);

            bool hitSomething = Physics.Raycast(muzzlePos, dir, out RaycastHit physicsHit, dist, arrowRaycastMask);
            Vector3 targetPos = hitSomething ? physicsHit.point : aimPoint;

            SpawnLocalBullet(rifleMuzzle, muzzlePos, targetPos, false, hitSomething);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyBulletHit()
    {
        cameraRotation.FlashDamageCrosshair();
    }

    private void SpawnLocalBullet(Transform muzzle, Vector3 fallbackStart, Vector3 target, bool bodyHit, bool hitSomething)
    {
        if (localBulletPool == null || localBulletPool.Length == 0) return;

        int tick = Runner != null ? Runner.Tick : -1;
        if (tick == _lastBulletSpawnTick) return;
        _lastBulletSpawnTick = tick;

        var bullet = localBulletPool[_localBulletIndex];
        _localBulletIndex = (_localBulletIndex + 1) % localBulletPool.Length;

        if (muzzle != null)
        {
            bullet.Fire(muzzle, target, bodyHit, hitSomething);
            return;
        }

        bullet.FireFromPosition(fallbackStart, target, bodyHit, hitSomething);
    }

    private void SpawnLocalBulletAtPosition(Vector3 start, Vector3 target)
    {
        if (localBulletPool == null || localBulletPool.Length == 0) return;

        var bullet = localBulletPool[_localBulletIndex];
        _localBulletIndex = (_localBulletIndex + 1) % localBulletPool.Length;
        bullet.FireFromPosition(start, target, false, true);
    }

    public void CameraShaker(float amplitude)
    {
        vcamShaker.m_AmplitudeGain = amplitude;
    }

    //[Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    //public void RPC_CameraShaker(float shaker)
    //{
    //    if (ShakerCoroutine != null) StopCoroutine(ShakerCoroutine);

    //    ShakerCoroutine = StartCoroutine(Shaker(shaker));
    //}

    IEnumerator Shaker(float shaker)
    {
        CameraShaker(shaker);
        yield return new WaitForSecondsRealtime(0.15f);
        CameraShaker(0f);
    }

    public void FireArrow()
    {
        Transform bowMuzzle = inventory.SecondaryWeapon?.ImpactPoint?.transform;

        if (HasStateAuthority)
        {
            if (ArrowFiredTick == Runner.Tick) return;

            string killerName = networkLoader != null && !string.IsNullOrWhiteSpace(networkLoader.Username)
                ? networkLoader.Username
                : (ownObjectEnabler != null ? ownObjectEnabler.Username.ToString() : "PLAYER");

            Vector3 shooterOrigin = cameraRotation.ShooterOrigin;
            Vector3 aimPoint = playerMovementV2.AimPoint;

            // Aim-assist: snap to the locked enemy's chest if within tolerance.
            Vector3 aimChest = cameraRotation.GetAimAssistChestPosition();
            if (aimChest != Vector3.zero &&
                Vector3.Angle(aimPoint - shooterOrigin, aimChest - shooterOrigin) < 25f)
                aimPoint = aimChest;

            // Authoritative shot: deterministic origin -> the crosshair world
            // point the client resolved from the real camera. No camera needed
            // server-side, and no 3rd-person over-the-shoulder parallax.
            Ray ray = new Ray(shooterOrigin, (aimPoint - shooterOrigin).normalized);
            Vector3 rayStart = shooterOrigin;

            FireRayDbgOrigin = rayStart;
            FireRayDbgDir    = ray.direction;

            LagCompensatedHit arrowHit = new LagCompensatedHit();

            float aimRange   = cameraRotation.AimDistance;
            Vector3 arrowOrigin = bowMuzzle != null ? bowMuzzle.position : transform.position;
            Vector3 targetPos   = arrowOrigin + ray.direction * aimRange;
            bool bodyHit = false;
            bool hitSomething = false;

            int safetyLimit = 10;

            while (safetyLimit-- > 0)
            {
                if (!Runner.LagCompensation.Raycast(rayStart, ray.direction, 999f, Object.InputAuthority, out arrowHit, arrowRaycastMask, HitOptions.IncludePhysX))
                    break;

                NetworkObject hitObj = arrowHit.Hitbox?.Root.Object;
                if (hitObj != null && hitObj.InputAuthority == Object.InputAuthority)
                {
                    // Skip shooter's own hitboxes — advance 0.5m to clear thick box colliders
                    rayStart = arrowHit.Point + ray.direction * 0.5f;
                    continue;
                }

                float distToHit = Vector3.Distance(arrowOrigin, arrowHit.Point);

                if (distToHit <= aimRange)
                {
                    targetPos = arrowHit.Point;
                    hitSomething = true;

                    if (arrowHit.Hitbox != null)
                    {
                        var enemyHealth = arrowHit.Hitbox.Root.GetBehaviour<PlayerHealthV2>();
                        if (enemyHealth != null)
                        {
                            string tag = arrowHit.Hitbox.tag;
                            float damage = tag switch
                            {
                                "Head"     => 75f,
                                "Body"     => 55f,
                                "Thigh"    => 45f,
                                "Shin"     => 40f,
                                "Foot"     => 35f,
                                "Arm"      => 50f,
                                "Forearm"  => 40f,
                                _          => 0f
                            };
                                enemyHealth.ApplyDamage(damage, killerName, Object);
                            bodyHit = true;
                        }
                        else
                        {
                            var botHealth = arrowHit.Hitbox.Root.GetBehaviour<Botdata>();
                            if (botHealth != null && !botHealth.IsDead)
                            {
                                string tag = arrowHit.Hitbox.tag;
                                float damage = tag switch
                                {
                                    "Head"    => 75f,
                                    "Body"    => 55f,
                                    "Thigh"   => 45f,
                                    "Shin"    => 40f,
                                    "Foot"    => 35f,
                                    "Arm"     => 50f,
                                    "Forearm" => 40f,
                                    _         => 0f
                                };
                                    botHealth.ApplyDamage(damage, killerName, Object);
                                bodyHit = true;
                            }
                        }
                    }
                }
                // Beyond aimRange: arrow stops at range limit, no damage applied
                break;
            }

            ArrowStart = arrowOrigin;
            ArrowTarget = targetPos;
            ArrowFiredTick = Runner.Tick;

            inventory.ReduceBowAmmo();

            if (HasInputAuthority)
            {
                SpawnLocalArrow(bowMuzzle, targetPos, bodyHit, hitSomething);
                if (bodyHit)
                    cameraRotation.FlashDamageCrosshair();
            }
            else if (bodyHit)
                RPC_NotifyArrowHit();
        }
        else if (HasInputAuthority)
        {
            // Visual prediction: spawn from the bow muzzle toward the same
            // crosshair point so the arrow matches the reticle (no parallax).
            Vector3 arrowOrigin = bowMuzzle != null ? bowMuzzle.position : transform.position;
            Vector3 aimPoint = playerMovementV2.AimPoint;
            Vector3 dir = (aimPoint - arrowOrigin).normalized;
            float dist = Vector3.Distance(arrowOrigin, aimPoint);

            bool hitSomething = Physics.Raycast(arrowOrigin, dir, out RaycastHit physicsHit, dist, arrowRaycastMask);
            Vector3 targetPos = hitSomething ? physicsHit.point : aimPoint;

            SpawnLocalArrow(bowMuzzle, targetPos, false, hitSomething);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyArrowHit()
    {
        cameraRotation.FlashDamageCrosshair();
    }

    public void RegisterComboHit(Vector3 enemyPosition)
    {
        if (!HasInputAuthority) return;
        if (hitIndicatorPool == null || hitIndicatorPool.Length == 0) return;

        _comboCount++;
        string label = $"COMBO x {_comboCount}";

        var indicator = hitIndicatorPool[_hitIndicatorIndex];
        _hitIndicatorIndex = (_hitIndicatorIndex + 1) % hitIndicatorPool.Length;
        indicator.Show(enemyPosition, label);

        CancelInvoke(nameof(ResetCombo));
        Invoke(nameof(ResetCombo), 2f);
    }

    private void ResetCombo() => _comboCount = 0;

    private void SpawnLocalArrow(Transform muzzle, Vector3 target, bool bodyHit, bool hitSomething)
    {
        if (localArrowPool == null || localArrowPool.Length == 0) return;
        if (muzzle == null) return;

        int tick = Runner != null ? Runner.Tick : -1;
        if (tick == _lastArrowSpawnTick) return;
        _lastArrowSpawnTick = tick;

        var arrow = localArrowPool[_localArrowIndex];
        _localArrowIndex = (_localArrowIndex + 1) % localArrowPool.Length;
        arrow.Fire(muzzle, target, bodyHit, hitSomething);
    }

    private void SpawnLocalArrowAtPosition(Vector3 start, Vector3 target)
    {
        if (localArrowPool == null || localArrowPool.Length == 0) return;

        var arrow = localArrowPool[_localArrowIndex];
        _localArrowIndex = (_localArrowIndex + 1) % localArrowPool.Length;
        arrow.FireFromPosition(start, target);
    }

    public void PlayJumpSoundEffect()
    {
        if (HasStateAuthority) return;

        footstepSource.PlayOneShot(jumpClip);
    }

    public void PlayRollSoundEffect()
    {
        if (HasStateAuthority) return;

        footstepSource.PlayOneShot(rollClip);
    }

    public void CheckGround()
    {
        if (Runner.LagCompensation.Raycast(groundDetector.position, Vector3.down, 10f, Object.InputAuthority, out hit, groundMask, HitOptions.IncludePhysX))
        {
            if (hit.GameObject == null) return;

            GameObject g = hit.GameObject;
            if (g.CompareTag("BattleAreaStage") || g.CompareTag("WaitingAreaStage")) CurrentGround = Ground.TERRAIN;
            else if (g.CompareTag("Stone")) CurrentGround = Ground.STONE;
            else if (g.CompareTag("Dirt")) CurrentGround = Ground.DIRT;
            else if (g.CompareTag("Wood")) CurrentGround = Ground.WOOD;
            else if (g.CompareTag("Grass")) CurrentGround = Ground.GRASS;
        }
    }

    public void PlayFootstepSound()
    {
        if (HasStateAuthority) return;

        if (CurrentGround == Ground.DIRT)
            footstepSource.PlayOneShot(GetClip(dirtClip));
        else if (CurrentGround == Ground.STONE)
            footstepSource.PlayOneShot(GetClip(stoneClip));
        else if (CurrentGround == Ground.WOOD)
            footstepSource.PlayOneShot(GetClip(woodClip));
        else if (CurrentGround == Ground.GRASS)
            footstepSource.PlayOneShot(GetClip(grassClip));
    }

    AudioClip GetClip(AudioClip[] clipArray)
    {
        if (audioClipIndex > clipArray.Length)
            audioClipIndex = 0;

        selectedClip = clipArray[audioClipIndex];

        previousClip = selectedClip;
        return selectedClip;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (Runner == null) return;
        if (FireRayDbgDir == Vector3.zero) return;

        // Only show for ~2 seconds after the last shot (40 ticks at 20Hz)
        int ticksSinceFire = Runner.Tick - ArrowFiredTick;
        if (ticksSinceFire > 40 || ticksSinceFire < 0) return;

        // SA fire ray — magenta line from origin in direction
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(FireRayDbgOrigin, FireRayDbgOrigin + FireRayDbgDir * 50f);
        Gizmos.DrawWireSphere(FireRayDbgOrigin, 0.05f);

        // Hit point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ArrowTarget, 0.15f);
    }
}

public struct LookAtJobBoneIK : IAnimationJob
{
    public TransformStreamHandle bone;
    public float weight;

    public float pitchDeg;
    public Vector3 pitchAxisLocal;
    public float pitchAxisSign;

    public Vector3 rotationCorrection;

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (weight <= 0f) return;
        if (!bone.IsValid(stream)) return;

        Quaternion baseLocal = bone.GetRotation(stream);

        Quaternion pitchDelta = Quaternion.AngleAxis(pitchDeg * pitchAxisSign, pitchAxisLocal);
        Quaternion correction = Quaternion.Euler(rotationCorrection);
        bone.SetRotation(stream, baseLocal * Quaternion.Slerp(Quaternion.identity, pitchDelta, weight) * correction);
    }
}

