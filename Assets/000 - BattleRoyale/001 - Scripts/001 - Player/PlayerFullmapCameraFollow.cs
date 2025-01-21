using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFullmapCameraFollow : MonoBehaviour
{
    [SerializeField] private Slider mapSlider;
    [SerializeField] private Transform playerTarget;

    private void OnEnable()
    {
        transform.parent = null;
    }

    private void OnDisable()
    {
        playerTarget = null;
    }

    private void Update()
    {
        if (transform != null && playerTarget != null && mapSlider.value > 0)
            transform.position = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
    }
}
