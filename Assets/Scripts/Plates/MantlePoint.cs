using UnityEngine;
using System.Collections;

public class MantlePoint : MonoBehaviour {

    public Vector3 location;

    public float radius;

    public float temperature;

    public float density;

    public int octDepth;

    public Color heatColor;

    public int gridID = -1;

    public ParticlePoint point;

    public Mantle parentMantle;

    private Renderer renderer;

    public void InitializePoint ( Vector3 _location, float _radius, float _temperature, float _density ) {
        this.location = _location;
        this.radius = _radius;
        this.temperature = _temperature;
        this.density = _density;

        this.transform.position = this.location;

        this.point = new ParticlePoint {
            pos = this.location,
            rad = this.radius,
            vel = new Vector3(),
            dx = 0,
            dy = 0,
            dz = 0,
            temp = this.temperature,
            density = this.density
        };

        this.parentMantle = FindObjectOfType<Mantle>();
        this.renderer = this.GetComponent<Renderer>();

        this.heatColor = Color.Lerp(Color.red, Color.yellow, this.MapRange(this.point.temp, this.parentMantle.coreTemperature, this.parentMantle.surfaceTemperature, 1, 0));
        this.renderer.material.color = this.heatColor;
    }

    public void RotatePoint ( Vector3 _axis, float _rotationSpeed ) {
        this.location = this.RotateVector(this.location, _axis, Mathf.Deg2Rad * _rotationSpeed);
        this.transform.position = this.location;
    }

    public void UpdatePoint ( ParticlePoint _point ) {
        this.location = _point.pos;
        this.temperature = _point.temp;
        this.transform.position = _point.pos;
        this.point = _point;
        this.heatColor = Color.Lerp(Color.red, Color.yellow, this.MapRange(this.point.temp, this.parentMantle.coreTemperature, this.parentMantle.surfaceTemperature, 1, 0));
        this.renderer.material.color = this.heatColor;
    }

    private Vector3 RotateVector ( Vector3 _original, Vector3 _axis, float _angle ) {
        // Find the cross and dot products from the original vector and the rotation axis.
        Vector3 cross = Vector3.Cross(_axis, _original);
        float dot = Vector3.Dot(_axis, _original);

        // Rotate based on Rodrigues' Rotation Formula.
        Vector3 rotatedVector = (_original * Mathf.Cos(_angle))
            + (cross * Mathf.Sin(_angle))
            + (_axis * dot * (1 - Mathf.Cos(_angle)));
        return rotatedVector;
    }

    private float MapRange (float _value, float _originalMax, float _originalMin, float _newMax, float _newMin) {
        return ((_value - _originalMin) / (_originalMax - _originalMin) * (_newMax - _newMin)) + _newMin;
    }
}


// 64 bytes total, 512 bits
public struct ParticlePoint {
    public Vector3 pos;     // 12 bytes
    public Vector3 vel;     // 12 bytes
    public float rad;       // 4 bytes

    public int dx;          // 12 bytes
    public int dy;
    public int dz;

    public float temp;      // 12 bytes
    public int dtemp;
    public float density;

    public int blank1;      // 12 bytes
    public int blank2;
    public int blank3;
}