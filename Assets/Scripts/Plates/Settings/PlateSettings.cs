using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlateSettings
{
    [Range(1, 10)]
    public int MinSeedPlateCount = 5;
    [Range(5, 20)]
    public int MaxSeedPlateCount = 10;
    [Range(0, 0.1f)]
    public float NewSeedPlateChance = 0.005f;

    [Range(0f, 1.0f)]
    public float PlateStartingRoughness = 0.1f;
}
