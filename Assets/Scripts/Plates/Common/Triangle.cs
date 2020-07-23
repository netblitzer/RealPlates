using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Triangle {

    public int[] Indices { get; private set; }

    public Triangle ( int a, int b, int c ) {
        this.Indices = new int[] { a, b, c };
    }
}
