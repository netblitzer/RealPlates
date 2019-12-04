using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace {

    ShapeGenerator shapeGenerator;
    Mesh mesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;

    public TerrainFace ( ShapeGenerator _generator, Mesh _mesh, int _resolution, Vector3 _localUp ) {
        this.shapeGenerator = _generator;
        this.mesh = _mesh;
        this.resolution = _resolution;
        this.localUp = _localUp;

        this.axisA = new Vector3(_localUp.y, _localUp.z, _localUp.x);
        this.axisB = Vector3.Cross(_localUp, this.axisA);
    }

    public void ConstructMesh ( ) {

        // Mesh information.
        Vector3[] vertices = new Vector3[this.resolution * this.resolution];
        Vector3[] normals = new Vector3[this.resolution * this.resolution];
        int[] triangles = new int[(this.resolution - 1) * (this.resolution - 1) * 6];
        int triIndex = 0;

        // Loop through each point on the mesh and find the spherical point to map to.
        for (int y = 0; y < this.resolution; y++) {
            for (int x = 0; x < this.resolution; x++) {
                // Get the vertice index.
                int i = (y * this.resolution) + x;
                // Find the percent of the side that we've gone through.
                Vector2 percent = new Vector2(x, y) / (this.resolution - 1);
                // Find the location where this point would be on a cube.
                Vector3 pointOnCube = this.localUp + ((percent.x - 0.5f) * 2 * this.axisA) + ((percent.y - 0.5f) * 2 * this.axisB);
                // Find the location where this point would be on a sphere.
                Vector3 pointOnUnitSphere = pointOnCube.normalized;
                // Set the point to that location.
                vertices[i] = this.shapeGenerator.CalculatePointOnPlanet(pointOnUnitSphere);

                // Set the normal for the point.
                normals[i] = vertices[i].normalized;

                // Map the triangle indexes.
                if (x != (this.resolution - 1) && y != (this.resolution - 1)) {
                    // First triangle.
                    triangles[triIndex++] = i;
                    triangles[triIndex++] = i + this.resolution + 1;
                    triangles[triIndex++] = i + this.resolution;

                    // Second triangle.
                    triangles[triIndex++] = i;
                    triangles[triIndex++] = i + 1;
                    triangles[triIndex++] = i + this.resolution + 1;
                }
            }
        }

        // Clear mesh information.
        this.mesh.Clear();

        // Set the mesh information.
        this.mesh.vertices = vertices;
        this.mesh.triangles = triangles;
        this.mesh.normals = normals;
    }
}
