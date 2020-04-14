using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PlanetSettings : ScriptableObject
{

    [Range(1, 10)]
    public int SubDivisions = 2;
    [Range(0, 4f)]
    public float Jitter = 0f;
    [Range(1, 100)]
    public float PlanetRadius = 10f;

    public bool GenerateRandomAxis = true;
    [Range(0, 100)]
    public float RotationSpeed = 15f;           // 15 degrees per second.

    public PlateSettings plateSettings;
    public GenerationSettings generationSettings;
    public InitialPlanetGenerationSettings initalGenerationSettings;
}