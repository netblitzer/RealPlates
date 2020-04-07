using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneTest : MonoBehaviour
{

    PoissonRandom poisson;
    public GameObject pointPrefab;
    List<GameObject> randomPoints;

    public float Width = 10f;
    public float Height = 10f;
    public float MinimumDistance = 0.5f;
    public int MaxAttemptsPerPoint = 10;

    // Start is called before the first frame update
    void Initialize ()
    {
        if (this.poisson == null)
            this.poisson = new PoissonRandom();

        if (this.randomPoints == null)
            this.randomPoints = new List<GameObject>();
        else {
            this.randomPoints.Clear();
            while (this.transform.childCount > 0) {
                Transform point = this.transform.GetChild(0);
                DestroyImmediate(point.gameObject);
            }
        }
    }

    public void GeneratePoisson() {
        this.Initialize();
        List<Vector2> points = this.poisson.PoissonPlane(this.Width, this.Height, this.MinimumDistance, this.MaxAttemptsPerPoint);

        foreach (Vector2 point in points) {
            GameObject newRandom = Instantiate(this.pointPrefab);
            newRandom.transform.parent = this.transform;
            newRandom.transform.localPosition = point;
            newRandom.name = "Point #" + (this.randomPoints.Count + 1);
            this.randomPoints.Add(newRandom);
        }
    }


}
