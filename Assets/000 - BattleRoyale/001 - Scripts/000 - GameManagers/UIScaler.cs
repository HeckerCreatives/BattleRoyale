using UnityEngine;

public class UIScaler : MonoBehaviour
{
    void Start()
    {
        float aspectRatio     = (float)Screen.width / Screen.height;
        float referenceAspect = 19.5f / 9f;

        // Max scale is 16/19.5 ≈ 0.82 — exactly what a 16:9 phone produces naturally
        // from the formula. Capping here means no phone wider than 16:9 can ever
        // receive a larger scale than a 16:9 phone, which was the root of the
        // "UI gets bigger on 20:9" bug.
        float maxScale    = 16f / 19.5f;
        float scaleFactor = Mathf.Clamp(aspectRatio / referenceAspect, 0.5f, maxScale);

        Debug.Log($"UIScaler — {Screen.width}x{Screen.height}  aspect={aspectRatio:F4}  scale={scaleFactor:F4}");

        GetComponent<RectTransform>().localScale = Vector3.one * scaleFactor;
    }
}
