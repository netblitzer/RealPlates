using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class TectonicPlate {

    private Planet parentPlanet;

    private List<TectonicTriangle> triangles;

    public float age;

    public bool CanGrow;

    public int PlateIndex;

    private List<TectonicTriangle> edgeTriangles;

    public void Initialize (Planet _parent, TectonicTriangle _rootTriangle, int _index) {
        this.parentPlanet = _parent;
        this.PlateIndex = _index;

        this.triangles = new List<TectonicTriangle>();
        this.edgeTriangles = new List<TectonicTriangle>();

        this.triangles.Add(_rootTriangle);
        this.edgeTriangles.Add(_rootTriangle);
        _rootTriangle.SetParentPlate(this);

        this.age = 0;
        this.CanGrow = true;

        // Create a new color for the plate.
        Color tempColor =  new Color(Random.Range(100, 255) / 255f, Random.Range(100, 255) / 255f, Random.Range(100, 255) / 255f);
        // Send the color to the submesh.
        this.parentPlanet.UpdateTectonicPlateMaterial(tempColor, this.PlateIndex);
    }

    public bool GrowPlate ( ) {
        // Set up a queue of all the triangles to go through.
        List<TectonicTriangle> queue = new List<TectonicTriangle>(this.edgeTriangles);

        // Go through each edge triangle to see if we can grow to it.
        while(queue.Count > 0) {
            int nextTriangle = Random.Range(0, queue.Count);

            // Get the neighbors of the triangle to grow from.
            List<TectonicTriangle> trianglesToCheck = new List<TectonicTriangle>(queue[nextTriangle].GetNeighborTriangles());

            // Go through each neighbor triangle and see if we can grow.
            while(trianglesToCheck.Count > 0) {
                int randomNeighbor = Random.Range(0, trianglesToCheck.Count);

                // If the neighbor isn't set to a plate yet, add it.
                if (trianglesToCheck[randomNeighbor].parentPlate == null) {
                    trianglesToCheck[randomNeighbor].SetParentPlate(this);

                    // Add the newly added triangle to our lists.
                    this.edgeTriangles.Add(trianglesToCheck[randomNeighbor]);
                    this.triangles.Add(trianglesToCheck[randomNeighbor]);

                    // Check if the root triangle is now completely surrounded.
                    bool surrounded = true;
                    for (int i = 0; i < 2; i++) {
                        if (trianglesToCheck[((i + randomNeighbor + 1) % trianglesToCheck.Count)].parentPlate == null) {
                            surrounded = false;
                        }
                    }

                    // If it is, remove it from future growth.
                    if (surrounded) {
                        this.edgeTriangles.Remove(queue[nextTriangle]);
                    }

                    return true;
                }
                else {
                    trianglesToCheck.RemoveAt(randomNeighbor);
                }
            }

            // If we can't grow, remove it from the list of edge triangles (it's no longer on an incomplete edge) and the queue.
            this.edgeTriangles.Remove(queue[nextTriangle]);
            queue.RemoveAt(nextTriangle);
        }

        this.CanGrow = false;
        return false;
    }

    public void UpdateMesh ( ) {

        Profiler.BeginSample("Plate: Updating Submesh Indices");
        List<int> indices = new List<int>();

        for (int i = 0; i < this.triangles.Count; i++) {
            indices.AddRange(this.triangles[i].GetPointIndices());
        }

        this.parentPlanet.UpdateTectonicPlateMesh(indices, this.PlateIndex);
        Profiler.EndSample();
    }
}

//  Plate:
//      Variables:
//          Triangles (List of Triangles): the triangles that make up the plate.
//          Age (float): how old the plate is.
//      Methods:
//          GrowPlate () {}: grows the plate one outwards to an unclaimed triangle.
//