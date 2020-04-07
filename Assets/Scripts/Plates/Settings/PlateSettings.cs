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

    [Range(0f, 5f)]
    public float PlateStartingThicknessVariance = 1f;
    [Range(0f, 1f)]
    public float PlateStartingDensityVariance = 0.5f;



    // ----- Simulated Plate Properties Settings ----- //

    /// <summary>
    /// The base density of new plate material created from a divergence.
    /// </summary>
    [Range(2.5f, 5f)]
    public float NewPlateMaterialDensity = 4f;
    /// <summary>
    /// The variation DOWNWARDS from the base new plate density. Makes new plates
    /// be varied in how dense they will be created rather than uniform.
    /// </summary>
    [Range(0f, 0.25f)]
    public float NewPlateMaterialDensityVariation = 4f;
    /// <summary>
    /// The base thickness of new plate material created from a divergence.
    /// </summary>
    [Range(2.5f, 6f)]
    public float NewPlateMaterialThickness = 3.5f;
    /// <summary>
    /// The variation DOWNWARDS from the base new plate thickness. Makes new plates
    /// be varied in how thick they will be created rather than uniform.
    /// </summary>
    [Range(0f, 0.5f)]
    public float NewPlateMaterialThicknessVariation = 0.2f;

    /// <summary>
    /// A modifier of density to increase it for cooler plates. Plates cool over time
    /// making them gain density and sink. A value of 2 will cause density to be twice
    /// as great.
    /// </summary>
    [Range(1f, 2f)]
    public float CooledPlateDensityModifier = 1.2f;
    /// <summary>
    /// The age at which a point will gain the maximum density modifier from being cooled
    /// down. Adding new material will decrease the average age, bringing the modifier
    /// down and lowering density.
    /// </summary>
    [Range(1000, 1000000)]
    public int CooledPlateMaxDensityAge = 100;

    /// <summary>
    /// The age at which two triangles moving in similar directions will connect at their
    /// shared halfsides.
    /// </summary>
    [Range(100, 100000)]
    public int HalfSideConnectionAge = 100;
    /// <summary>
    /// The age at which two triangles moving in similar directions will connect at their
    /// shared halfsides.
    /// </summary>
    [Range(100, 100000)]
    public int HalfSideConnectionMaxStrengthAge = 200;
    /// <summary>
    /// The base strength that a connected halfside will have when it's first connected.
    /// Lower values will mean plates break up more easily.
    /// </summary>
    [Range(1, 10)]
    public float HalfSideConnectionStartingStrength = 5f;
    /// <summary>
    /// The maximum strength that a connected halfside will have when it's at the age for
    /// maximum strength.
    /// Lower values will mean plates break up more easily.
    /// </summary>
    [Range(1, 10)]
    public float HalfSideConnectionEndStrength = 8f;



    // ----- Subduction Settings ----- //

    /// <summary>
    /// The percent that a triangle's velocity must be heading towards another triangle's
    /// edge to be able to subduct. Less than this means the triangle is not acting enough 
    /// in the direction towards the edge to cause a potential subduction to occur.
    /// </summary>
    [Range(0f, 1.0f)]
    public float SubductionDirectionRequirement = 0.7f;
    /// <summary>
    /// The minumum amount of difference in density a triangle must have to be able to subduct
    /// under another one.
    /// </summary>
    [Range(0f, 0.5f)]
    public float SubductionDensityDifferenceRequirement = 0.25f;
    /// <summary>
    /// The minumum amount of difference in thickness a triangle must have to be able to
    /// subduct under another one.
    /// </summary>
    [Range(0f, 1.5f)]
    public float SubductionThicknessDifferenceRequirement = 0.5f;
    /// <summary>
    /// The minumum amount of difference in material a triangle must have to be able to subduct
    /// under another one. Material is defined as thickness*density.
    /// </summary>
    [Range(0f, 0.5f)]
    public float SubductionMaterialDifferenceRequirement = 0.25f;
    /// <summary>
    /// The minumum amount of difference in elevation a triangle must have to be able to subduct
    /// under another one. Elevation is defined as height above sea level. Should the elevation be
    /// too similar, the minimum density is used.
    /// </summary>
    [Range(0f, 1f)]
    public float SubductionElevationifferenceRequirement = 0.25f;
    /// <summary>
    /// The bias density has towards calculating material for subduction. The higher the bias,
    /// the more likely a plate with high density will subduct under another plate even if it
    /// is also thicker.
    /// </summary>
    [Range(0f, 1.0f)]
    public float SubductionMaterialDensityBias = 0.5f;
    /// <summary>
    /// The thickness where a triangle is no longer able to subduct under another.
    /// </summary>
    [Range(10f, 25.0f)]
    public float SubductionThicknessLimit = 12f;
}
