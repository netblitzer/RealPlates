using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISphereShape
{
    List<Vector3> Points { get; }

    List<int> Indices { get; }

    List<Triangle> Triangles { get; }

    int VerticesCount { get; }

    int TrianglesCount { get; }

    void ResetSphere ();

    void GenerateSphere ();

    void RenderSphere (Mesh _meshToRender);
}
