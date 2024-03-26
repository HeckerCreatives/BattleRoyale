using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MovementData", menuName = "Battle Royale/Player/MovementData")]
public class MovementData : ScriptableObject
{
    [field: Header("MOVEMENT")]
    [field: SerializeField] public float MovementSpeed { get; private set; }
    [field: SerializeField] public float SlopeForce { get; private set; }
}
