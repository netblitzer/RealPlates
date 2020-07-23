using UnityEngine;
using System.Collections;

public class PTHalfSide {

    private Planet parentPlanet;

    public PTPoint Start;
    public PTPoint End;
    
    public PTTriangle ParentTriangle { get; private set; }

    public PTBoundary ParentBoundary { get; private set; }

    public PTHalfSide (Planet _parentP, PTPoint _start, PTPoint _end) {
        this.parentPlanet = _parentP;
        this.Start = _start;
        this.End = _end;
    }

    public void SetParentTriangle (PTTriangle _parentT) {
        this.ParentTriangle = _parentT;
    }

    public void SetParentBoundary (PTBoundary _parentB) {
        this.ParentBoundary = _parentB;
    }
}
