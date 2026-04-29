using TMPro;
using UnityEngine;

public class HitIndicatorController : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;

    private Camera _cam;

    private void Awake() => _cam = Camera.main;

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
            transform.forward = _cam.transform.forward;
    }

    public void Show(Vector3 worldPos, string label)
    {
        if (_cam == null) _cam = Camera.main;

        LeanTween.cancel(gameObject);
        transform.position = worldPos;
        text.text = label;
        text.alpha = 1f;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        Vector3 camRight = _cam != null ? _cam.transform.right : Vector3.right;
        float side = (Random.value > 0.5f ? 1f : -1f) * Random.Range(0.3f, 0.55f);
        const float arcHeight = 1.2f;
        const float duration  = 1.2f;

        // Parabolic arc: Sin(t * PI) rises then falls back to origin height
        LeanTween.value(gameObject, 0f, 1f, duration)
            .setEase(LeanTweenType.linear)
            .setOnUpdate((float t) =>
            {
                float y = Mathf.Sin(t * Mathf.PI) * arcHeight;
                float x = side * t;
                transform.position = worldPos + Vector3.up * y + camRight * x;
            });

        // Scale: normal → zero over the arc
        LeanTween.scale(gameObject, Vector3.zero, duration)
            .setEase(LeanTweenType.easeInQuad);

        // Fade: opaque → transparent over the arc
        LeanTween.value(gameObject, 1f, 0f, duration)
            .setEase(LeanTweenType.easeInQuad)
            .setOnUpdate((float val) => { if (text != null) text.alpha = val; })
            .setOnComplete(() => gameObject.SetActive(false));
    }
}
