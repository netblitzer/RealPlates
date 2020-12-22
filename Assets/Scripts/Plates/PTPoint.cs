using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;

public class PTPoint {

    private Planet parent;


    /// <summary>
    /// The true 3D location on the sphere that the point is at.
    /// </summary>
    private Vector3 sphereLocation;
    public Vector3 Location => this.sphereLocation;
    public Vector3 SphereLocation => this.sphereLocation;


    /// <summary>
    /// The location using Longitude/Latitude of a sphere.
    /// </summary>
    private Vector2 projectedLocation;
    public Vector2 ProjectedLocation => this.projectedLocation;

    public float Longitude => this.projectedLocation.x;
    public float Latitude => this.projectedLocation.y;


    private Matrix4x4 rotationMatrix;
    public Matrix4x4 RotationMatrix => this.rotationMatrix;

    private int directionAdjust;


    public int RenderIndex;

    public Vector3 MomentumTorque => this.momentumTorque;


    private Vector3 netTorque;

    private Vector3 momentumTorque;



    public PTPoint (Planet _parent, Vector3 _location) {
        this.parent = _parent;
        this.sphereLocation = _location;
        this.CalculateRotationMatrix();
    }

    public void SetSphereLocation (Vector3 _location) {
        this.sphereLocation = _location;
    }

    public void CalculateMovement (float _timestep, float _friction = -0.98f) {
        this.momentumTorque += (this.netTorque * _timestep);
        
        if (Mathf.Abs(this.momentumTorque.x) > 0.000005f || Mathf.Abs(this.momentumTorque.y) > 0.000005f || Mathf.Abs(this.momentumTorque.z) > 0.000005f) {
            this.momentumTorque += (this.momentumTorque * _friction * _timestep);
            this.sphereLocation = PTFunctions.RotateVectorQuaternion(this.sphereLocation, this.momentumTorque.normalized, this.momentumTorque.magnitude);
        }
        else {
            this.momentumTorque = Vector3.zero;
        }

        this.netTorque = Vector3.zero;
    }
    
    public void CalculateRotationMatrix () {
        float x = Vector3.Dot(Vector3.right, this.SphereLocation) * 180 / Mathf.PI;
        float y = Vector3.Dot(Vector3.up, this.SphereLocation) * 180 / Mathf.PI;
        Quaternion rotation = Quaternion.Euler(x, y, 0);
        this.rotationMatrix = Matrix4x4.Rotate(rotation);
    }

    public void AddTorque (Vector3 _torque, float damping = 0f) {
        if (damping > 0f) {
            this.netTorque.x += _torque.x - (this.momentumTorque.x * damping);//Mathf.Max(0f, Mathf.Abs(_torque.x - (this.momentumTorque.x * damping))) * Mathf.Sign(_torque.x);
            this.netTorque.y += _torque.y - (this.momentumTorque.y * damping);//Mathf.Max(0f, Mathf.Abs(_torque.y - (this.momentumTorque.y * damping))) * Mathf.Sign(_torque.y);
            this.netTorque.z += _torque.z - (this.momentumTorque.z * damping);//Mathf.Max(0f, Mathf.Abs(_torque.z - (this.momentumTorque.z * damping))) * Mathf.Sign(_torque.z);
        }
        else {
            this.netTorque += _torque;
        }
    }
}
