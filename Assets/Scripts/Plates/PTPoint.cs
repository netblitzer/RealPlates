using UnityEngine;
using System.Collections;

public class PTPoint {

    private Planet parent;

    private Vector3 location;
    public Vector3 Location => this.location;
    public Vector3 SphereLocation => this.location;


    private Vector2 projectedLocation;
    public Vector2 ProjectedLocation => this.projectedLocation;

    public float Longitude => this.projectedLocation.x;
    public float Latitude => this.projectedLocation.y;


    private int directionAdjust;


    public int RenderIndex;




    public PTPoint (Planet _parent, Vector3 _location) {
        this.parent = _parent;
        this.location = _location;
    }

    public void SetSphereLocation (Vector3 _location) {
        this.location = _location;
    }

    public void AddForce (Vector3 _force) {

    }

    public void CalculateMovement (float _timestep) {

    }
}
