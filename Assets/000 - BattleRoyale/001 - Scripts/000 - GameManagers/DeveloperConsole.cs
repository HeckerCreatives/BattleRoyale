using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeveloperConsole : MonoBehaviour
{
    string myLog = "*begin log";
    string filename = "";
    public bool doShow = true;
    int kChars = 700;
    Vector2 scroll;
    float lineHeight = 20f;
    void OnEnable() { Application.logMessageReceived += Log; }
    void OnDisable() { Application.logMessageReceived -= Log; }
    public void Log(string logString, string stackTrace, LogType type)
    {
        myLog += "\n" + logString + "\n" + stackTrace;
    }

    void OnGUI()
    {
        if (!doShow) return;

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));

        // Calculate dynamic height based on number of lines
        int lineCount = myLog.Split('\n').Length;
        float contentHeight = lineCount * lineHeight;

        Rect viewRect = new Rect(10, 10, 600, 400);
        Rect contentRect = new Rect(0, 0, 580, contentHeight);

        scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);

        GUI.TextArea(new Rect(0, 0, 580, contentHeight), myLog);

        GUI.EndScrollView();
    }
}
