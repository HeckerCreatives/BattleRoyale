using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class NavigationStaticInspector
{
    [MenuItem("Tools/Find Objects with Navigation Statics")]
    private static void FindObjectsWithNavigationStatic()
    {
        int count = 0;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                SerializedObject so = new SerializedObject(go);
                SerializedProperty prop = so.FindProperty("m_StaticEditorFlags");

                if (prop == null) continue;

                int flags = prop.intValue;

                const int NavigationStaticBit = 1 << 1;

                if ((flags & NavigationStaticBit) != 0)
                {
                    string flagNames = DecodeStaticFlags(flags);
                    string path = GetFullPath(t);
                    Debug.Log($"✅ Navigation Static FOUND: {path} | Flags: {flagNames}", go);
                    count++;
                }
            }
        }

        Debug.Log($"🎯 Done. Found {count} GameObjects with 'Navigation Static'.");
    }

    private static string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static string DecodeStaticFlags(int flagValue)
    {
        var names = Enum.GetValues(typeof(StaticEditorFlags));
        string result = "";
        foreach (StaticEditorFlags flag in names)
        {
            if ((int)flag == 0) continue;
            if ((flagValue & (int)flag) != 0)
            {
                result += flag.ToString() + ", ";
            }
        }
        return result.TrimEnd(',', ' ');
    }
}