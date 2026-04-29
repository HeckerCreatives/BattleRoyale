using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Collections;
using System.Threading.Tasks;




#if UNITY_EDITOR
using UnityEditor;
#endif
public enum LocalAxis
{
    X,
    Y,
    Z,
    NegativeX,
    NegativeY,
    NegativeZ
}

[ExecuteAlways]
public class MeshMapScatterTool : NetworkBehaviour
{
    [System.Serializable]
    public class ScatterRule
    {
        [Header("Info")]
        public string ruleName = "New Rule";
        public bool enabled = true;

        [Header("Prefabs")]
        public NetworkObject[] prefabs;

        [Header("Spawn Count")]
        [Tooltip("Spawn count at max players (see scaling settings below).")]
        public int spawnCount = 100;
        public int maxAttempts = 3000;

        [Header("Spawn Count Scaling (by Player Count)")]
        public bool scaleSpawnCountByPlayers = true;
        [Min(1)] public int maxPlayersForScaling = 30;
        [Tooltip("Minimum spawn count when player count is 0 (or very low).")]
        [Min(0)] public int minSpawnCount = 0;

        [Header("Allowed Surface")]
        public LayerMask groundLayerMask;
        public string[] allowedTags;

        [Header("Scale")]
        public Vector2 randomScaleRange = new Vector2(1f, 1f);

        [Header("Rotation")]
        public bool randomYRotation = true;
        public bool alignToSurfaceNormal = false;
        public Vector3 surfaceNormalDirection;

        [Header("Random Rotation (Advanced)")]
        public bool useRandomAxisRotation = false;
        public LocalAxis randomRotationAxis = LocalAxis.Y;
        public float randomRotationMin = 0f;
        public float randomRotationMax = 360f;

        [Header("Three Point Surface Alignment")]
        public bool useThreePointSurfaceAlignment = false;
        public LocalAxis objectLengthAxis = LocalAxis.Z;
        public float backSampleDistance = 0.5f;
        public float frontSampleDistance = 0.5f;
        public float sampleRayStartHeight = 2f;
        public float sampleRayDistance = 10f;

        [Header("Rotation Offset")]
        public Vector3 rotationOffsetEuler = Vector3.zero;

        [Header("Slope")]
        [Range(0f, 90f)] public float minSlope = 0f;
        [Range(0f, 90f)] public float maxSlope = 35f;

        [Header("Spacing")]
        public float minSpacing = 2f;

        [Header("Height Limit")]
        public bool useHeightLimit = false;
        public float minHeight = -9999f;
        public float maxHeight = 9999f;

        [Header("Raycast")]
        public float rayStartHeight = 200f;
        public float rayDistance = 500f;

        [Header("Offset")]
        public float yOffset = 0f;

        [Header("Parent")]
        public string parentFolderName = "Spawned";
    }

    [Header("Scatter Area")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(200f, 50f, 200f);

    [Header("Settings")]
    public int randomSeed = 12345;
    public bool clearBeforeGenerate = true;

    [Header("Rules")]
    public List<ScatterRule> rules = new List<ScatterRule>();

    [Header("Debug")]
    public bool drawScatterArea = true;
    public bool drawRaycastHits = false;
    public bool doneInitialize = false;

    private readonly List<Vector3> _debugHitPoints = new List<Vector3>();

    public async void Generate()
    {
        if (Runner == null)
        {
            Debug.LogError("MeshMapScatterTool: NetworkRunner is not assigned.");
            return;
        }

        if (!Application.isPlaying)
        {
            Debug.LogError("MeshMapScatterTool: Runner.Spawn() only works in Play Mode.");
            return;
        }

        if (!Runner.IsRunning)
        {
            Debug.LogError("MeshMapScatterTool: Runner is not running.");
            return;
        }

        if (!Runner.IsServer)
        {
            Debug.LogWarning("MeshMapScatterTool: Only the server/host should generate Fusion spawns.");
            return;
        }

        if (clearBeforeGenerate)
            ClearSpawnedObjects();

        _debugHitPoints.Clear();
        Random.InitState(randomSeed);

        int playerCount = GetCurrentPlayerCount();

        foreach (var rule in rules)
        {
            if (!rule.enabled)
                continue;

            if (rule.prefabs == null || rule.prefabs.Length == 0)
            {
                Debug.LogWarning($"Rule '{rule.ruleName}' has no prefabs.");
                continue;
            }

            Transform parent = GetOrCreateParent(rule.parentFolderName);

            int spawned = 0;
            int attempts = 0;
            List<Vector3> localPositions = new List<Vector3>();
            int effectiveSpawnCount = GetEffectiveSpawnCount(rule, playerCount);

            while (spawned < effectiveSpawnCount && attempts < rule.maxAttempts)
            {
                attempts++;

                if (!TryGetValidPoint(rule, localPositions, out Vector3 spawnPosition, out Vector3 normal))
                    continue;

                NetworkObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
                if (prefab == null)
                    continue;

                NetworkObject instance = Runner.Spawn(prefab);

                if (instance == null) continue;

                instance.transform.position = spawnPosition + normal * rule.yOffset;

                Vector3 finalPosition = spawnPosition + normal * rule.yOffset;
                Quaternion rotation = Quaternion.identity;

                if (rule.useThreePointSurfaceAlignment)
                {
                    if (!TryGetThreePointRotation(rule, finalPosition, normal, out rotation))
                    {
                        if (rule.alignToSurfaceNormal)
                        {
                            Vector3 normalAxis = rule.surfaceNormalDirection == Vector3.zero
                                ? Vector3.up
                                : rule.surfaceNormalDirection.normalized;

                            rotation = Quaternion.FromToRotation(normalAxis, normal) * Quaternion.Euler(rule.rotationOffsetEuler);
                        }
                        else
                        {
                            rotation = Quaternion.Euler(rule.rotationOffsetEuler);
                        }
                    }
                }
                else
                {
                    if (rule.alignToSurfaceNormal)
                    {
                        Vector3 normalAxis = rule.surfaceNormalDirection == Vector3.zero
                            ? Vector3.up
                            : rule.surfaceNormalDirection.normalized;

                        rotation = Quaternion.FromToRotation(normalAxis, normal) * Quaternion.Euler(rule.rotationOffsetEuler);
                    }
                    else
                    {
                        rotation = Quaternion.Euler(rule.rotationOffsetEuler);
                    }

                    if (!rule.useRandomAxisRotation && rule.randomYRotation)
                    {
                        rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * rotation;
                    }
                }

                if (rule.useRandomAxisRotation)
                {
                    Vector3 axis = GetLocalAxisVector(rule.randomRotationAxis);

                    // IMPORTANT: convert axis to world space based on current rotation
                    axis = rotation * axis;

                    float angle = Random.Range(rule.randomRotationMin, rule.randomRotationMax);

                    rotation = Quaternion.AngleAxis(angle, axis) * rotation;
                }

                instance.transform.rotation = rotation;

                float scale = Random.Range(rule.randomScaleRange.x, rule.randomScaleRange.y);
                //instance.transform.localScale = Vector3.one * scale;

                instance.TryGetComponent<PrimaryWeaponItem>(out PrimaryWeaponItem primaryweapon);
                instance.TryGetComponent<SecondaryWeaponItem>(out SecondaryWeaponItem secondaryweapon);
                instance.TryGetComponent<ArmorItem>(out ArmorItem armor);
                instance.TryGetComponent<HealWeaponItem>(out HealWeaponItem heal);
                instance.TryGetComponent<RepairWeaponItem>(out RepairWeaponItem repair);
                instance.TryGetComponent<AmmoRifleWeaponItem>(out AmmoRifleWeaponItem ammo);
                instance.TryGetComponent<MagazineContainerItem>(out MagazineContainerItem bowammo);

                if (primaryweapon != null) primaryweapon.InitializeItemOnSpawn(finalPosition, rotation, 1);
                if (secondaryweapon != null)
                {
                    int tempammot = Random.Range(1, 11);
                    secondaryweapon.InitializeItemOnSpawn(finalPosition, rotation, tempammot);
                }
                if (armor != null) armor.InitializeItemOnSpawn(finalPosition, rotation);
                if (heal != null) heal.InitializeItem(finalPosition, rotation, 1);
                if (repair != null) repair.InitializeItem(finalPosition, rotation, 1);
                if (bowammo != null)
                {
                    int tempammot = Random.Range(1, 11);
                    bowammo.InitializeOnStart(finalPosition, rotation, tempammot);
                }
                if (ammo != null)
                {
                    int tempammot = Random.Range(1, 11);
                    ammo.InitializeOnStart(finalPosition, rotation, tempammot);
                }

                localPositions.Add(spawnPosition);
                spawned++;

                await Task.Yield();
            }

            Debug.Log($"Rule '{rule.ruleName}' spawned {spawned}/{effectiveSpawnCount} (players={playerCount}) after {attempts} attempts.");

            await Task.Yield();
        }

        doneInitialize = true;
    }

    private int GetCurrentPlayerCount()
    {
        if (Runner == null || !Runner.IsRunning)
            return 0;

        int count = 0;
        foreach (var _ in Runner.ActivePlayers)
            count++;

        return count;
    }

    private int GetEffectiveSpawnCount(ScatterRule rule, int playerCount)
    {
        if (rule == null)
            return 0;

        int maxPlayers = Mathf.Max(1, rule.maxPlayersForScaling);
        int targetMax = Mathf.Max(0, rule.spawnCount);

        if (!rule.scaleSpawnCountByPlayers)
            return targetMax;

        float t = Mathf.Clamp01(playerCount / (float)maxPlayers);
        int target = Mathf.RoundToInt(Mathf.Lerp(rule.minSpawnCount, targetMax, t));
        return Mathf.Clamp(target, 0, targetMax);
    }

    private bool TryGetThreePointRotation(
    ScatterRule rule,
    Vector3 centerPosition,
    Vector3 centerNormal,
    out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        Vector3 lengthAxis = GetLocalAxisVector(rule.objectLengthAxis).normalized;

        Vector3 normalAxis = rule.surfaceNormalDirection == Vector3.zero
            ? Vector3.up
            : rule.surfaceNormalDirection.normalized;

        Quaternion baseRotation = Quaternion.FromToRotation(normalAxis, centerNormal);

        Vector3 backOffset = (baseRotation * lengthAxis) * -rule.backSampleDistance;
        Vector3 frontOffset = (baseRotation * lengthAxis) * rule.frontSampleDistance;

        Vector3 backWorld = centerPosition + backOffset;
        Vector3 middleWorld = centerPosition;
        Vector3 frontWorld = centerPosition + frontOffset;

        if (!RaycastSamplePoint(backWorld, rule, out RaycastHit backHit))
            return false;

        if (!RaycastSamplePoint(middleWorld, rule, out RaycastHit middleHit))
            return false;

        if (!RaycastSamplePoint(frontWorld, rule, out RaycastHit frontHit))
            return false;

        Vector3 forward = (frontHit.point - backHit.point).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            return false;

        Vector3 averagedNormal = (backHit.normal + middleHit.normal + frontHit.normal).normalized;
        if (averagedNormal.sqrMagnitude < 0.0001f)
            return false;

        rotation = Quaternion.LookRotation(forward, averagedNormal) * Quaternion.Euler(rule.rotationOffsetEuler);
        return true;
    }

    private Vector3 GetLocalAxisVector(LocalAxis axis)
    {
        switch (axis)
        {
            case LocalAxis.X: return Vector3.right;
            case LocalAxis.Y: return Vector3.up;
            case LocalAxis.Z: return Vector3.forward;
            case LocalAxis.NegativeX: return Vector3.left;
            case LocalAxis.NegativeY: return Vector3.down;
            case LocalAxis.NegativeZ: return Vector3.back;
            default: return Vector3.forward;
        }
    }

    private bool RaycastSamplePoint(Vector3 sampleWorldPosition, ScatterRule rule, out RaycastHit hit)
    {
        Vector3 rayOrigin = sampleWorldPosition + Vector3.up * rule.sampleRayStartHeight;

        return Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out hit,
            rule.sampleRayDistance,
            rule.groundLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    public void ClearSpawnedObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(child.gameObject);
#else
            DestroyImmediate(child.gameObject);
#endif
        }
    }

    private bool TryGetValidPoint(ScatterRule rule, List<Vector3> localPositions, out Vector3 spawnPosition, out Vector3 normal)
    {
        spawnPosition = Vector3.zero;
        normal = Vector3.up;

        Bounds bounds = new Bounds(areaCenter, areaSize);

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);
        float rayY = bounds.max.y + rule.rayStartHeight;

        Vector3 rayOrigin = new Vector3(randomX, rayY, randomZ);

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rule.rayDistance, rule.groundLayerMask, QueryTriggerInteraction.Ignore))
            return false;

        if (drawRaycastHits)
            _debugHitPoints.Add(hit.point);

        if (!IsTagAllowed(hit.collider.tag, rule.allowedTags))
            return false;

        normal = hit.normal;

        float slope = Vector3.Angle(normal, Vector3.up);
        if (slope < rule.minSlope || slope > rule.maxSlope)
            return false;

        if (rule.useHeightLimit)
        {
            if (hit.point.y < rule.minHeight || hit.point.y > rule.maxHeight)
                return false;
        }

        if (IsInsideExclusionZone(hit.point))
            return false;

        if (!HasEnoughSpacing(hit.point, rule.minSpacing, localPositions))
            return false;

        spawnPosition = hit.point;
        return true;
    }

    private bool IsTagAllowed(string hitTag, string[] allowedTags)
    {
        if (allowedTags == null || allowedTags.Length == 0)
            return true;

        for (int i = 0; i < allowedTags.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(allowedTags[i]) && hitTag == allowedTags[i])
                return true;
        }

        return false;
    }

    private bool HasEnoughSpacing(Vector3 point, float minSpacing, List<Vector3> localPositions)
    {
        float sqrMinSpacing = minSpacing * minSpacing;

        for (int i = 0; i < localPositions.Count; i++)
        {
            if ((localPositions[i] - point).sqrMagnitude < sqrMinSpacing)
                return false;
        }

        return true;
    }

    private bool IsInsideExclusionZone(Vector3 point)
    {
#if UNITY_2023_1_OR_NEWER
        MeshScatterExclusionZone[] zones = FindObjectsByType<MeshScatterExclusionZone>(FindObjectsSortMode.None);
#else
        MeshScatterExclusionZone[] zones = FindObjectsOfType<MeshScatterExclusionZone>();
#endif

        for (int i = 0; i < zones.Length; i++)
        {
            if (!zones[i].enabled)
                continue;

            if (zones[i].Contains(point))
                return true;
        }

        return false;
    }

    private Transform GetOrCreateParent(string parentName)
    {
        Transform existing = transform.Find(parentName);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(parentName);

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(go, "Create Scatter Parent");
#endif

        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        return go.transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (drawScatterArea)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(areaCenter, areaSize);
        }

        if (drawRaycastHits)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _debugHitPoints.Count; i++)
            {
                Gizmos.DrawSphere(_debugHitPoints[i], 0.3f);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MeshMapScatterTool))]
public class MeshMapScatterToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        MeshMapScatterTool tool = (MeshMapScatterTool)target;

        if (GUILayout.Button("Generate Scatter"))
        {
            tool.Generate();
            EditorUtility.SetDirty(tool);
        }

        if (GUILayout.Button("Clear Spawned"))
        {
            tool.ClearSpawnedObjects();
            EditorUtility.SetDirty(tool);
        }
    }
}
#endif