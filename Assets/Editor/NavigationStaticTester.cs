using UnityEditor;
using UnityEngine;

public class NavigationStaticTester
{
    [MenuItem("Tools/Set Selected Object to Navigation Static")]
    private static void SetNavigationStatic()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        GameObject go = Selection.activeGameObject;
        var flags = GameObjectUtility.GetStaticEditorFlags(go);
        flags |= StaticEditorFlags.NavigationStatic;
        GameObjectUtility.SetStaticEditorFlags(go, flags);

        Debug.Log($"Set {go.name} to Navigation Static.", go);
    }
}