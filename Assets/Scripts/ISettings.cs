using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ISettings : ScriptableObject
{
    public abstract void ValidateValues();
}
