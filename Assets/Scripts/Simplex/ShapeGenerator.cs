using UnityEngine;
using System.Collections;

public class ShapeGenerator {

    ShapeSettings settings;
    NoiseFilter noiseFilter;

    public ShapeGenerator ( ShapeSettings _settings ) {
        this.settings = _settings;
        this.noiseFilter = new NoiseFilter(_settings.noiseSettings);
    }

    public Vector3 CalculatePointOnPlanet ( Vector3 _pointOnUnitSphere ) {
        float elevation = this.noiseFilter.Evaluate(_pointOnUnitSphere);
        return _pointOnUnitSphere * this.settings.planetBaseRadius * (1 + elevation);
    }
}
