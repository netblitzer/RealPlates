using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GenerationSettings
{

    // ---- General Generation Parameters ---- //

    /// <summary>
    /// The age (in arbitrary millions of years) per second that the simulation will take.
    /// </summary>
    [Range(0.1f, 10f)]
    public float AgeStepPerSecond = 1f;



    // ---- Plate Generation ---- //

    public int TrianglesPerStep = 5;
}
