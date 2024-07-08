using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [ReadOnly] public Transform playerCharacterTF;

    private void LateUpdate()
    {
        if (playerCharacterTF != null)
        {
            transform.position = new Vector3(playerCharacterTF.position.x, transform.position.y, playerCharacterTF.position.z);
        }
    }
}
