using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasUICameraSetter : MonoBehaviour
{
    public Canvas canvas;

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        canvas.worldCamera = GameManager.Instance.UICamera;
    }

    public void ChangeCanvasCamera(Camera cam) => canvas.worldCamera = cam;
}
