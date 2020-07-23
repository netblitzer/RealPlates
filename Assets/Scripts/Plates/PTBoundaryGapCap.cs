using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTBoundaryGapCap
{

    private Planet parentPlanet;

    public List<PTPoint> Points { get; private set; }

    public List<PTBoundary> Boundaries { get; private set; }


    public PTBoundaryGapCap (Planet _parentP, List<PTHalfSide> _sides) {
        this.parentPlanet = _parentP;

        this.Boundaries = new List<PTBoundary>();
        this.Points = new List<PTPoint>();

        for (int i = 0; i < _sides.Count; i += 2) {
            this.Boundaries.Add(_sides[i].ParentBoundary);
            this.Points.Add(_sides[i].End);
        }
    }

    public List<int> GetBoundaryCapIndices () {
        List<int> indices = new List<int>();

        int forward = 0;
        int reverse = 1;
        for (int i = 0; i < this.Points.Count - 2; i++) {
            if (i % 2 == 0) {
                indices.Add(this.Points[forward + 1].RenderIndex);
                indices.Add(this.Points[forward].RenderIndex);
                indices.Add(this.Points[this.Points.Count - reverse].RenderIndex);
                forward++;
            }
            else {
                indices.Add(this.Points[forward].RenderIndex);
                indices.Add(this.Points[this.Points.Count - reverse].RenderIndex);
                indices.Add(this.Points[this.Points.Count - reverse - 1].RenderIndex);
                reverse++;
            }
        }

        return indices;
    }
}
