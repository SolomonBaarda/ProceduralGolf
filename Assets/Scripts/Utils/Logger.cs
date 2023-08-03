using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Logger : MonoBehaviour
{
    private static ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
    public static UnityEvent<string> OnLogMessage = new UnityEvent<string>();

    public static void Log(string message)
    {
        // Store messages in a queue
        messages.Enqueue(message);
    }

    private void Update()
    {
        // Handle messages in the main thread
        while (messages.TryDequeue(out string message))
        {
            Debug.Log(message);
            OnLogMessage.Invoke(message);
        }
    }
}
