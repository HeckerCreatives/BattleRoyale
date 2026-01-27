using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorScheme", menuName = "RiseOfFearless/Player/Skins")]
public class ColorSchemes : ScriptableObject
{
    [field: SerializeField] public List<Color> Colors { get; set; }
}
