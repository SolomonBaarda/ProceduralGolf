﻿using UnityEngine;


[System.Serializable]
public abstract class VariablePreset : ScriptableObject
{
    public abstract void ValidateValues();
}
