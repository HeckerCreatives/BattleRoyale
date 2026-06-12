using UnityEngine;

// Drives the bow string LineRenderer each frame:
//   index 0 = top tip (anchored to the bow's top notch)
//   index 1 = middle point — follows the pulling hand while drawn, otherwise
//             sits at the natural rest midpoint
//   index 2 = bottom tip (anchored to the bow's bottom notch)
//
// Runs in LateUpdate so the Animator / IK / Cinemachine have already
// positioned the hand bone for the frame; the string visual never lags.
//
// The two anchor Transforms (top/bottom/restMid) live on the bow prefab so
// they always travel with the bow. PullPoint is wired at runtime by the
// equip code in SecondaryWeaponItem to a Transform parented under the
// player's left-hand bone — when null (bow not equipped) we fall back to
// restMid so the string reads as undrawn.
public class BowStringFollower : MonoBehaviour
{
    [Header("References")]
    [Tooltip("LineRenderer with PositionCount = 3 and UseWorldSpace = true. Material/width match the original string mesh.")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("Top notch of the bow (child of the bow prefab).")]
    [SerializeField] private Transform topTip;
    [Tooltip("Bottom notch of the bow (child of the bow prefab).")]
    [SerializeField] private Transform bottomTip;
    [Tooltip("Natural midpoint of the string at rest (child of the bow prefab). Used when IsDrawn is false or PullPoint is null.")]
    [SerializeField] private Transform restMid;

    // Set by SecondaryWeaponItem when the bow is equipped — a child Transform
    // of the player's pulling-hand bone. Null when the bow is unequipped /
    // dropped, in which case the string falls back to restMid.
    public Transform PullPoint { get; set; }

    // Set by the bow-aim state machines (Draw / Charge / Shot) on Enter/Exit.
    public bool IsDrawn { get; set; }

    private void LateUpdate()
    {
        if (lineRenderer == null || topTip == null || bottomTip == null) return;

        // Anchors stay locked to the bow.
        lineRenderer.SetPosition(0, topTip.position);
        lineRenderer.SetPosition(2, bottomTip.position);

        // Midpoint: hand while drawn, rest position otherwise.
        Vector3 mid;
        if (IsDrawn && PullPoint != null)
            mid = PullPoint.position;
        else if (restMid != null)
            mid = restMid.position;
        else
            mid = (topTip.position + bottomTip.position) * 0.5f;

        lineRenderer.SetPosition(1, mid);
    }
}
