using UnityEditor;
using UnityEngine;

public class DebugRawStaticFlags
{
    [MenuItem("Tools/Debug Raw m_StaticEditorFlags")]
    public static void DebugRawStaticEditorFlags()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        SerializedObject so = new SerializedObject(Selection.activeGameObject);
        var prop = so.FindProperty("m_StaticEditorFlags");

        if (prop != null)
        {
            Debug.Log($"Raw static flags for {Selection.activeGameObject.name}: {prop.intValue}");
        }
        else
        {
            Debug.Log("Could not find m_StaticEditorFlags");
        }
    }
}

