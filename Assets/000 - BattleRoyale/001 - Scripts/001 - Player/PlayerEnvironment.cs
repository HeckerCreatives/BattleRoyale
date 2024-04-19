using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEnvironment : MonoBehaviour
{
    public Vector2 CurrentVelocity
    {
        get => currentVelocity;
        set => currentVelocity = value;
    }

    public Vector2 Workspace
    {
        get => workspace;
        set => workspace = value;
    }

    public Rigidbody PlayerRB
    {
        get => playerRB;
        set => playerRB = value;
    }

    //  ============================

    [SerializeField] private GameplayController gameplayController;

    [Header("PLAYER SETTINGS")]
    [SerializeField] private MovementData movementData;
    [SerializeField] private Rigidbody playerRB;
    [SerializeField] private CapsuleCollider playerCollider;

    [Header("GROUND DETECTION")]
    [SerializeField] private Transform centerTF;
    [SerializeField] private float groundDetectionHeight;
    [SerializeField] private LayerMask ground;

    [Header("DEBUGGER")]
    [SerializeField] private Vector3 currentVelocity;
    [SerializeField] private Vector3 workspace;
    [SerializeField] private float groundAngle;
    [SerializeField] private Vector3 forward;

    //  ==============================

    RaycastHit hitInfo;

    //  ==============================

    #region VELOCITY MOVEMENTS

    public void SetVelocityZero()
    {
        playerRB.velocity = Vector2.zero;
        CurrentVelocity = Vector2.zero;
    }
    public void SetVelocityMovement(Vector3 velocity)
    {
        workspace.Set(velocity.x, playerRB.velocity.y, velocity.z);
        playerRB.velocity = workspace;
        currentVelocity = workspace;
    }
    public void SetVelocityY(float value)
    {
        workspace.Set(currentVelocity.x, value, currentVelocity.z);
        playerRB.velocity = workspace;
        currentVelocity = workspace;
    }

    #endregion

    #region GROUND MOVEMENTS

    public bool Grounded
    {
        get
        {
            if (Physics.Raycast(centerTF.position, Vector3.down, out hitInfo, groundDetectionHeight, ground))
                return true;
            else
                return false;
        }
    }

    public void CalculateFoward()
    {
        if (!Grounded)
        {
            forward = transform.forward;
            return;
        }

        forward = Vector3.Cross(hitInfo.normal, -transform.right);
    }

    public void CalculateGroundAngle()
    {
        if (!Grounded)
        {
            groundAngle = 90;
            return;
        }

        groundAngle = Vector3.Angle(hitInfo.normal, transform.forward);
    }

    public void ApplySlopeGravity()
    {
        if (groundAngle < 90 && gameplayController.MovementDirection != Vector2.zero)
        {
            playerRB.AddForce(Vector3.down * playerCollider.height / 2 * movementData.SlopeForce * Time.fixedDeltaTime);
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        Debug.DrawLine(centerTF.position, centerTF.position + forward * groundDetectionHeight * 2, Color.cyan);
        Debug.DrawLine(centerTF.position, centerTF.position + Vector3.down * groundDetectionHeight, Color.yellow);
    }
}
