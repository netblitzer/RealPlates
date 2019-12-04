using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CubeBoundary {
    public Vector3 Center { get; private set; }
    public Vector3 Max { get; private set; }
    public Vector3 Min { get; private set; }
    public float SideLength { get; private set; }
    public float HalfLength { get; private set; }

    public CubeBoundary ( Vector3 _center, float _side ) {
        if (_center == null || _center.x == float.NegativeInfinity || _center.y == float.NegativeInfinity || _center.z == float.NegativeInfinity) {
            this.Center = Vector3.zero;
        }
        else {
            this.Center = _center;
        }
        if (_side <= 0) {
            this.SideLength = 1f;
        }
        else {
            this.SideLength = _side;
        }
        this.HalfLength = this.SideLength / 2f;
        Vector3 offset = new Vector3(this.HalfLength, this.HalfLength, this.HalfLength);
        this.Max = this.Center + offset;
        this.Min = this.Center - offset;
    }

    public bool ContainsPoint ( Vector3 _point ) {
        for (int i = 0; i < 3; i++) {
            if (_point[i] < this.Min[i] || _point[i] > this.Max[i]) {
                return false;
            }
        }

        return true;
    }

    public bool ContainsSphere ( Vector3 _location, float _radius ) {
        for (int i = 0; i < 3; i++) {
            if (_location[i] - _radius < this.Min[i] || _location[i] + _radius > this.Max[i]) {
                return false;
            }
        }

        return true;
    }

    public bool IntersectsCube ( CubeBoundary _other ) {
        return false;
    }

    public bool IntersectsSphere ( Vector3 _center, float _radius ) {
        float distanceSquared = 0f;
        float min, max, v;

        for (int i = 0; i < 3; i++) {
            min = this.Min[i];
            max = this.Max[i];
            v = _center[i];

            if (v < min) {
                distanceSquared += (min - v) * (min - v);
            }
            else if (v > max) {
                distanceSquared += (v - max) * (v - max);
            }
        }

        if (distanceSquared > (_radius * _radius)) {
            return false;
        }

        return true;
    }
}
