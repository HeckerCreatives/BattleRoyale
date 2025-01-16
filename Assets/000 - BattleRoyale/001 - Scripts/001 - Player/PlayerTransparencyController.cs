using Cinemachine;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTransparencyController : NetworkBehaviour
{
    [SerializeField] private bool isSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer; // The SkinnedMeshRenderer of the player
    [SerializeField] private MeshRenderer meshRenderer; // The SkinnedMeshRenderer of the player
    [SerializeField] private float minDistance = 5f; // Minimum distance for transparency
    [SerializeField] private float maxDistance = 15f; // Maximum distance for full visibility
    [SerializeField] private float transparencyAtMinDistance = 0.3f; // Alpha value when closest

    private Material materialInstance;
    private Transform camera; // Assign the Cinemachine Virtual Camera

    private void Start()
    {
        if (Camera.main != null)
        {
            camera = Camera.main.transform;

            if (isSkinnedMeshRenderer)
            {
                if (skinnedMeshRenderer != null)
                    materialInstance = skinnedMeshRenderer.material; // Create a unique instance of the material
            }
            else
            {
                if (meshRenderer != null)
                    materialInstance = meshRenderer.material;
            }
        }
    }

    private void Update()
    {
        if (camera == null || materialInstance == null || !HasInputAuthority) return;

        // Calculate the distance between the camera and the player
        float distance = Vector3.Distance(camera.transform.position, transform.position);

        // Set the alpha value on the material
        materialInstance.SetFloat("_CameraDistance", distance);
    }
}
