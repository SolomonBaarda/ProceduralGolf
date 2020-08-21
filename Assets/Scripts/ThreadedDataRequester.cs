using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    // Credit to:
    // https://github.com/SebLague/Procedural-Landmass-Generation/blob/master/Proc%20Gen%20E21/Assets/Scripts/ThreadedDataRequester.cs


    public int Threads;
    public static ThreadedDataRequester Instance;
    private Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    private void Awake()
    {
        Instance = FindObjectOfType<ThreadedDataRequester>();
    }


    private void OnDestroy()
    {
        Clear();
    }


    public static void Clear()
    {
        Instance.dataQueue.Clear();
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        // Method for the thread to run
        void threadStart()
        {
            Instance.DataThread(generateData, callback);
        }

        new Thread(threadStart).Start();
    }




    private void DataThread(Func<object> function, Action<object> callback)
    {
        DateTime t = DateTime.Now;
        object returnValue = function();

        lock (dataQueue)
        {
            dataQueue.Enqueue(new ThreadInfo(callback, returnValue));
        }

        //Debug.Log("Took " + (DateTime.Now - t).Milliseconds + " ms to complete");
    }


    void Update()
    {
        if (dataQueue.Count > 0)
        {
            for (int i = 0; i < dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        Threads = dataQueue.Count;
    }

    private struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }

    }
}