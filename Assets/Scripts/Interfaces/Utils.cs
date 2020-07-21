using UnityEngine;

public static class Utils
{




    /// <summary>
    /// Returns true if the attached GameObject has a script that implements the class T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toCheck"></param>
    /// <returns></returns>
    public static bool GameObjectExtendsClass<T>(in GameObject toCheck) where T : class
    {
        return GetClass<T>(toCheck) != null;
    }



    /// <summary>
    /// Returns the monobehaviour that implements class T in the GameObject. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toCheck"></param>
    /// <returns></returns>
    public static T GetClass<T>(GameObject toCheck) where T : class
    {
        // Get all monobehaviours 
        MonoBehaviour[] all = toCheck.GetComponents<MonoBehaviour>();

        // Loop through each
        foreach (MonoBehaviour behaviour in all)
        {
            // If the monobehaviour implements the interface
            if (behaviour is T t)
            {
                // Return it
                return t;
            }
        }

        return null;
    }




}
