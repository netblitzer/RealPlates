using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings {

    public float minValue = 0;
    public float maxValue = 5;

    [Range(0, 5)]
    public float strength = 1;
    public float baseRoughness = 1f;
    [Range(0, 5)]
    public float roughness = 2f;
    public Vector3 offset;

    [Range(1, 10)]
    public int numLayers;
    [Range(0.1f, 0.99f)]
    public float persistance = 0.5f;
}
