using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseFilter {

    NoiseSettings settings;
    Noise noise = new Noise();

    public NoiseFilter ( NoiseSettings _settings ) {
        this.settings = _settings;
    }

    public float Evaluate ( Vector3 _point ) {
        float noiseValue = 0;
        float frequency = this.settings.baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < this.settings.numLayers; i++) {
            Vector3 p = (_point * frequency) + this.settings.offset;
            float v = (float) this.noise.Evaluate(p.x, p.y, p.z);
            noiseValue += (v + 1) * 0.5f * amplitude;

            frequency *= this.settings.roughness;
            amplitude *= this.settings.persistance;
        }

        return noiseValue * this.settings.strength;
    }
}
