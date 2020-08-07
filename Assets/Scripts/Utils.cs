using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utils
{

    /// <summary>
    /// Returns true if the GameObject extends class T. Sets output as reference to that class or Null otherwise.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toCheck"></param>
    /// <param name="extendedClass"></param>
    /// <returns></returns>
    public static bool GameObjectExtendsClass<T>(in GameObject toCheck, out T extendedClass) where T : class
    {
        // Get all monobehaviours 
        MonoBehaviour[] all = toCheck.GetComponents<MonoBehaviour>();

        // Loop through each
        foreach (MonoBehaviour behaviour in all)
        {
            // If the monobehaviour implements the interface
            if (behaviour is T output)
            {
                extendedClass = output;

                // Return it
                return true;
            }
        }

        extendedClass = null;
        return false;
    }



    public static bool IsWithinArrayBounds<T>(int x, int y, in T[,] array)
    {
        if (array != null)
        {
            int width = array.GetLength(0), height = array.GetLength(1);

            return x >= 0 && y >= 0 && x < width && y < height;
        }

        return false;
    }



    public static bool ContainsAll<T>(List<T> a, List<T> b)
    {
        return !b.Except(a).Any();
    }



    public static void EMPTY() { }
    public static void EMPTY<T>(T _) { }


}
