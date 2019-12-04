using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplexPlanet : MonoBehaviour {

    [Range(2, 256)]
    public int resolution = 10;
    public bool autoUpdate = true;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    [HideInInspector]
    public bool shapeSettingsFoldout = true;
    [HideInInspector]
    public bool colorSettingsFoldout = true;

    private ShapeGenerator shapeGenerator; 

    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;

    /// <summary>
    /// Initializes all the data for the planet.
    /// </summary>
    void Initialize ( ) {

        this.shapeGenerator = new ShapeGenerator(this.shapeSettings);

        if (this.meshFilters == null || this.meshFilters.Length == 0) {
            // Assign arrays.
            this.meshFilters = new MeshFilter[6];
        }

        if (this.terrainFaces == null || this.terrainFaces.Length == 0) {
            // Assign arrays.
            this.terrainFaces = new TerrainFace[6];
        }

        Vector3[] directions = { Vector3.forward, Vector3.left, Vector3.up, Vector3.back, Vector3.right, Vector3.down };

        // Go through each cube face and assign its properties.
        for (int i = 0; i < 6; i++) {
            // Only create a new mesh if the mesh currently doesn't exist.
            if (this.meshFilters[i] == null) {
                GameObject meshObj = new GameObject("Mesh");
                meshObj.transform.parent = this.transform;

                meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
                this.meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                this.meshFilters[i].sharedMesh = new Mesh();
            }

            this.terrainFaces[i] = new TerrainFace(this.shapeGenerator, this.meshFilters[i].sharedMesh, this.resolution, directions[i]);
        }
    }

    public void GeneratePlanet ( ) {
        this.Initialize();
        this.GenerateMesh();
        this.GenerateColors();
    }

    public void OnShapeSettingsUpdated ( ) {
        if (this.autoUpdate) {
            this.Initialize();
            this.GenerateMesh();
        }
    }

    public void OnColorSettingsUpdated ( ) {
        if (this.autoUpdate) {
            this.Initialize();
            this.GenerateColors();
        }
    }

    /// <summary>
    /// Creates the mesh on each face of the sphere/cube.
    /// </summary>
    void GenerateMesh ( ) {
        foreach (TerrainFace face in this.terrainFaces) {
            face.ConstructMesh();
        }
    }

    void GenerateColors ( ) {
        foreach(MeshFilter mesh in this.meshFilters) {
            mesh.GetComponent<MeshRenderer>().sharedMaterial.color = this.colorSettings.planetBaseColor;
        }
    }
}