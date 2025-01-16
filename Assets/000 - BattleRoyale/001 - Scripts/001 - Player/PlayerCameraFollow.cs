using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
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
        if (transform != null && playerTarget != null)
            transform.position = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
    }
}
