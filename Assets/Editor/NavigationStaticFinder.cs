using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationStaticFinder
{
    [MenuItem("Tools/DEBUG: Log All Static Flags")]
    private static void DebugAllStaticFlags()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        foreach (var root in roots)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                var flags = GameObjectUtility.GetStaticEditorFlags(go);

                if (flags != 0) // Only print if any static flag is set
                {
                    Debug.Log($"[{go.name}] Static Flags: {flags}", go);
                }
            }
        }

        Debug.Log("Finished logging all static flags.");
    }
}
