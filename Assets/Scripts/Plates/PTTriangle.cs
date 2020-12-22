using UnityEngine;
using System.Collections;
using Boo.Lang;

public class PTTriangle {

    private Planet parent;

    public PTPoint[] Points { get; private set; }

    public PTHalfSide[] Sides { get; private set; }

    private Vector3 center;

    /*
    public PTTriangle (PTPoint _a, PTPoint _b, PTPoint _c) {
        this.Points = new PTPoint[] { _a, _b, _c };
    }
    */
    public PTTriangle (Planet _parent, PTHalfSide[] _sides) :
        this(_parent, _sides[0], _sides[1], _sides[2]) { }

    public PTTriangle (Planet _parent, PTHalfSide _a, PTHalfSide _b, PTHalfSide _c) {
        this.parent = _parent;

        this.Points = new PTPoint[] { _a.Start, _b.Start, _c.Start };
        this.Sides = new PTHalfSide[] { _a, _b, _c };

        foreach (PTHalfSide side in this.Sides) {
            side.SetParentTriangle(this);
        }
    }

    public PTHalfSide GetNextHalfSide (PTHalfSide _initial) {
        PTHalfSide nextSide = null;

        for (int i = 0; i < 3; i++) {
            if (this.Sides[i] == _initial) {
                nextSide = this.Sides[(i + 1) % 3];
                break;
            }
        }

        return nextSide;
    }
    public PTHalfSide GetPreviousHalfSide (PTHalfSide _initial) {
        PTHalfSide prevSide = null;

        for (int i = 0; i < 3; i++) {
            if (this.Sides[i] == _initial) {
                prevSide = this.Sides[(i + 2) % 3];
                break;
            }
        }

        return prevSide;
    }

    public void GetCenter () {
        for (int i = 0; i < 3; i++) {
            this.center += this.Points[i].Location;
        }

        this.center /= 3f;
        this.center.Normalize();
    }

    public void CalculateTriangleState () {
        // Calculate if the triangle needs to split.

        // See how many sides are too large and need to be split (in case all need to be split in the same frame).
        int tooLarge = 0;
        for (int i = 0; i < 3; i++) {
            if (this.Sides[i].CurrentLength > this.parent.averageSideLength * 2f) {
                tooLarge++;
            }
        }

        // If any of the triangles are too large and need to be split.
        if (tooLarge > 0) {
            List<PTHalfSide> sidesToSplit = new List<PTHalfSide>(3);
            for (int i = 0; i < 3; i++) {
                if (this.Sides[i].CurrentLength > this.parent.averageSideLength * 2f) {
                    sidesToSplit.Add(this.Sides[i]);
                }
            }

            this.parent.SplitTriangle(this, sidesToSplit.ToArray());
        }
    }

    public void ContractPointsTest (float _percent) {
        for (int i = 0; i < 3; i++) {
            this.Points[i].SetSphereLocation((this.Points[i].Location * (1 - _percent)) + (this.center * _percent));
        }
    }

    public void ExpandTriangleTest (float expandScale, float _timestep) {
        for (int i = 0; i < 3; i++) {
            float force = (this.parent.averageSideLength * expandScale) - this.Sides[i].CurrentLength;
            Vector3 forceVec = this.Sides[i].Cross * force * _timestep * 25f;

            this.Sides[i].Start.AddTorque(-forceVec, 1f);
            this.Sides[i].End.AddTorque(forceVec, 1f);
        }
    }
}
