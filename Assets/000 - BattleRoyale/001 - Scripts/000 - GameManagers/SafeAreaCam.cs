using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeAreaCam : MonoBehaviour
{
    public Camera cam;

    void Awake()
    {
        
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // Convert Safe Area to Viewport Coordinates
        float xMin = safeArea.x / Screen.width;
        float yMin = safeArea.y / Screen.height;
        float width = safeArea.width / Screen.width;
        float height = safeArea.height / Screen.height;

        // Apply to Camera Viewport Rect
        cam.rect = new Rect(xMin, yMin, width, height);
    }
}
