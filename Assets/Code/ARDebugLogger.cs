using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARDebugLogger : MonoBehaviour
{
    public TMP_Text consoleOutput;
    public int maxLines = 5;

    private Queue<string> logQueue = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    public void PrintDebugMessage(string message)
    {
        logQueue.Enqueue(message);
        if (logQueue.Count > maxLines)
        {
            logQueue.Dequeue();
        }

        consoleOutput.text = string.Join("\n", logQueue);
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type != LogType.Error)
            return;
 
        logQueue.Enqueue(logString);
        logQueue.Enqueue(stackTrace);
        

        if (logQueue.Count > maxLines)
        {
            logQueue.Dequeue();
        }

        consoleOutput.text = string.Join("\n", logQueue);
    }
}