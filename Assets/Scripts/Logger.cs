using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Logger : MonoBehaviour
{
    private static Queue<string> messages = new Queue<string>();
    public static UnityEvent<string> OnLogMessage = new UnityEvent<string>();

    public static void Log(string message)
    {
        // Store messages in a queue
        messages.Enqueue(message);
    }

    public static void LogTerrainGenerationStartPass(int pass, string message)
    {

    }

    public static void LogTerrainGenerationFinishPass(int pass, double totalDurationSeconds)
    {
        Log($"* Completed pass {pass} in {totalDurationSeconds:0.0} seconds.");
    }

    private void Update()
    {
        // Handle messages in the main thread
        while (messages.Count > 0)
        {
            string message = messages.Dequeue();

            Debug.Log(message);
            OnLogMessage.Invoke(message);
        }
    }
}
