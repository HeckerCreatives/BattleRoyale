using TMPro;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [Header("Optional Reference Aspect (Design Resolution)")]
    [SerializeField] private Vector2 referenceAspect = new Vector2(16, 9);

    [Header("Camera Type")]
    [SerializeField] private bool isOrthographicCamera = false;

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI debugger;

    private Camera cameraPlayer;

    private int lastScreenWidth;
    private int lastScreenHeight;

    void Awake()
    {
        cameraPlayer = GetComponent<Camera>();
        cameraPlayer.orthographic = isOrthographicCamera;

        ApplyScaling();
        CacheScreenSize();
    }

    void Update()
    {
        // Only recalculate if screen size actually changed
        ApplyScaling();
        CacheScreenSize();
    }

    private void CacheScreenSize()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void ApplyScaling()
    {
        float targetAspect = referenceAspect.x / referenceAspect.y;
        float windowAspect = (float)Screen.width / Screen.height;

        float scaleHeight = windowAspect / targetAspect;

        Rect rect = cameraPlayer.rect;

        if (scaleHeight < 1f)
        {
            // Letterbox (top & bottom)
            rect.width = 1f;
            rect.height = windowAspect;
            rect.x = 0f;
            rect.y = (1f - windowAspect) / 2f;
        }
        else
        {
            // Pillarbox (left & right)
            float scaleWidth = 1f / windowAspect;
            rect.width = windowAspect;
            rect.height = 1f;
            rect.x = (1f - windowAspect) / 2f;
            rect.y = 0f;
        }

        cameraPlayer.rect = rect;

        //UpdateDebugger(windowAspect);
    }

    private void UpdateDebugger(float windowAspect)
    {
        if (debugger == null) return;

        debugger.text =
            $"Resolution: {Screen.width} x {Screen.height}\n" +
            $"Aspect Ratio: {windowAspect:F2}\n" +
            $"Reference: {referenceAspect.x}:{referenceAspect.y}";
    }
}
