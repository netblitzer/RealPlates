using UnityEngine;
using System.Collections;

public class PTTriangle {

    private Planet parent;

    public PTPoint[] Points { get; private set; }

    public PTHalfSide[] Sides { get; private set; }

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


    private Vector3 center;
    public void GetCenter () {
        for (int i = 0; i < 3; i++) {
            this.center += this.Points[i].Location;
        }

        this.center /= 3f;
        this.center.Normalize();
    }

    public void ContractPointsTest (float _percent) {
        for (int i = 0; i < 3; i++) {
            this.Points[i].SetSphereLocation((this.Points[i].Location * (1 - _percent)) + (this.center * _percent));
        }
    }
}
