using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class PoissonRandom {

    private long seed;

    public PoissonRandom ( )
        : this(System.DateTime.Now.Ticks) {
    }

    public PoissonRandom ( long _seed ) {
        this.seed = _seed;
    }

    public List<MantlePoint> PoissonMantle ( float _minDistanceBetweenPoints, int _newPointAttempts, Vector3 _center, float _mantleRadius, float _surfaceTemperature, float _coreTemperature,
        out Octree tree, GameObject _mantlePointPrefab, float _pointRadius, float _startingDensity, float _coreRadius = 0 ) {

        // Generate the octTree.
        tree = new Octree(_center, _mantleRadius * 2f, _pointRadius * 2f);

        // Create the lists.
        List<MantlePoint> finalPoints = new List<MantlePoint>();
        List<Vector3> processList = new List<Vector3>();

        float mantleThickness = _mantleRadius - _coreRadius;
        float temperature, distanceFromCenter, mantlePlacement;

        // Create the first point.
        float halfRadius = _mantleRadius / 2f;
        Vector3 point = new Vector3(Random.Range(-halfRadius, halfRadius), Random.Range(-halfRadius, halfRadius), Random.Range(-halfRadius, halfRadius));

        // Calculate initial values of the point.
        distanceFromCenter = Vector3.Distance(point, _center);
        mantlePlacement = (distanceFromCenter - _coreRadius) / mantleThickness;
        temperature = ((1 - mantlePlacement) * _coreTemperature) + (mantlePlacement * _surfaceTemperature);

        GameObject createdObject = GameObject.Instantiate(_mantlePointPrefab);
        MantlePoint createdPoint = createdObject.GetComponent<MantlePoint>();
        createdPoint.InitializePoint(point, _pointRadius, temperature, _startingDensity);

        // Insert the first point into the lists.
        finalPoints.Add(createdPoint);
        processList.Add(point);

        // Insert point into the octTree.
        tree.InsertObject(createdPoint);

        // Begin processing points.
        while (processList.Count > 0) {
            Profiler.BeginSample("Processing mantle points");
            // Get a random point out of the processList.
            int sample = Mathf.FloorToInt(Random.Range(0, processList.Count));
            Vector3 samplePoint = processList[sample];
            processList.RemoveAt(sample);

            // Try to create new points around the samplePoint.
            for (int i = 0; i < _newPointAttempts; i++) {
                // Create a new point to test.
                point = this.GenerateNewPointInsideSphere(samplePoint, _minDistanceBetweenPoints);

                // Make sure the point is within the sphere. If not, try to create another point.
                distanceFromCenter = Vector3.Distance(point, _center);
                if (distanceFromCenter > _mantleRadius - (_pointRadius * 1.1f)) {
                    continue;
                }

                // Make sure the point is the desired distance away from the sphere.
                if (_coreRadius > 0 && distanceFromCenter < _coreRadius + _pointRadius) {
                    continue;
                }

                // See if there are any points too close to the new point in the octTree.
                if (tree.SphereIntersectsContainedPoints(point, _minDistanceBetweenPoints - _pointRadius) != null) {
                    continue;
                }

                // Calculate initial values of the point.
                mantlePlacement = (distanceFromCenter - _coreRadius) / mantleThickness;
                temperature = ((1 - mantlePlacement) * _coreTemperature) + (mantlePlacement * _surfaceTemperature);

                // Create the mantle object off of the point.
                createdObject = GameObject.Instantiate(_mantlePointPrefab);
                createdPoint = createdObject.GetComponent<MantlePoint>();
                createdPoint.InitializePoint(point, _pointRadius, temperature, _startingDensity);

                // Otherwise insert it into our lists and tree.
                if (tree.InsertObject(createdPoint)) {
                    finalPoints.Add(createdPoint);
                    processList.Add(point);
                }
                else {
                    // If it somehow doesn't fit into the tree, remove the object and try again.
                    GameObject.DestroyImmediate(createdPoint);
                }
            }
            Profiler.EndSample();
        }

        // After all points are processed, return the final list.
        return finalPoints;
    }

    public List<Vector3> PoissonInsideSphere (float _minDistanceBetweenPoints, int _newPointAttempts, Vector3 _center, float _radius, out PointOctree tree, float _minRadius = float.NegativeInfinity) {
        // Generate the octTree.
        tree = new PointOctree(_center, _radius * 2f);

        // Create the lists.
        List<Vector3> finalPoints = new List<Vector3>();
        List<Vector3> processList = new List<Vector3>();

        // Create the first point.
        float halfRadius = _radius / 2f;
        Vector3 point = new Vector3(Random.Range(-halfRadius, halfRadius), Random.Range(-halfRadius, halfRadius), Random.Range(-halfRadius, halfRadius));

        // Insert the first point into the lists.
        finalPoints.Add(point);
        processList.Add(point);

        // Insert point into the octTree.
        tree.InsertPoint(point);

        // Begin processing points.
        while (processList.Count > 0) {
            Profiler.BeginSample("Processing point");
            // Get a random point out of the processList.
            int sample = Mathf.FloorToInt(Random.Range(0, processList.Count));
            Vector3 samplePoint = processList[sample];
            processList.RemoveAt(sample);

            // Try to create new points around the samplePoint.
            for (int i = 0; i < _newPointAttempts; i++) {
                // Create a new point to test.
                point = this.GenerateNewPointInsideSphere(samplePoint, _minDistanceBetweenPoints);

                // Make sure the point is within the sphere. If not, try to create another point.
                float distanceFromCenter = Vector3.Distance(point, _center);
                if (distanceFromCenter > _radius) {
                    continue;
                }

                // Make sure the point is the desired distance away from the sphere.
                if (_minRadius > 0 && distanceFromCenter < _minRadius) {
                    continue;
                }

                // See if there are any points too close to the new point in the octTree.
                if (tree.SphereIntersectsContainedPoints(point, _minDistanceBetweenPoints)) {
                    continue;
                }

                // Otherwise insert it into our lists and tree.
                processList.Add(point);
                finalPoints.Add(point);
                tree.InsertPoint(point);
            }
            Profiler.EndSample();
        }

        // After all points are processed, return the final list.
        return finalPoints;
    }

    public List<Vector2> PoissonPlane ( float _xMax, float _yMax, float _minDistanceBetweenPoints, int _newPointAttempts ) {
        // Create the grid.
        float gridSize = _minDistanceBetweenPoints / Mathf.Sqrt(2f);
        int gridWidth = Mathf.CeilToInt(_xMax / gridSize);
        int gridHeight = Mathf.CeilToInt(_yMax / gridSize);
        Vector2[,] grid = new Vector2[gridWidth + 1, gridHeight + 1];

        // Set up the lists for points.
        List<Vector2> finalPoints = new List<Vector2>();
        List<Vector2> processList = new List<Vector2>();

        // Create the first point.
        Vector2 point = new Vector2(Random.Range(0, _xMax), Random.Range(0, _yMax));

        // Insert the point into the lists.
        finalPoints.Add(point);
        processList.Add(point);

        // Add point to the grid.
        Vector2 gridPoint = this.PointToGridPlane(point, gridSize);
        grid[(int) gridPoint.x, (int) gridPoint.y] = point;

        // Begin processing points.
        while (processList.Count > 0) {
            // Get a new sample point and pop it from the list.
            int sample = Mathf.FloorToInt(Random.Range(0, processList.Count));
            Vector2 samplePoint = processList[sample];
            processList.RemoveAt(sample);

            // Limit the number of attempts to create a new point based on the current point.
            for (int i = 0; i < _newPointAttempts; i++) {
                // Create a random point.
                point = this.GenerateNewPointPlane(samplePoint, _minDistanceBetweenPoints);

                // See if the point has any neighbors or is outside the plane.
                if (this.InBoundsPlane(_xMax, _yMax, point) && !this.PointsInNeighborhoodPlane(grid, gridWidth, gridHeight, gridSize, point, _minDistanceBetweenPoints)) {
                    // If it's not too close and within bounds, add it to the lists.
                    finalPoints.Add(point);
                    processList.Add(point);
                    gridPoint = this.PointToGridPlane(point, gridSize);
                    grid[(int) gridPoint.x, (int) gridPoint.y] = point;
                }
            }
        }

        // Finally return the points found.
        return finalPoints;
    }

    #region SphereInsideHelperFunctions

    private Vector3 GenerateNewPointInsideSphere (Vector3 _sourcePoint, float _minDistanceBetweenPoints) {
        float distance = _minDistanceBetweenPoints + Random.Range(0, _minDistanceBetweenPoints);
        float latitude = Random.Range(0, Mathf.PI);
        float longitude = Random.Range(0, 2 * Mathf.PI);

        Vector3 newPoint = new Vector3(Mathf.Sin(longitude), Mathf.Cos(longitude), Mathf.Cos(latitude)) * distance;
        newPoint += _sourcePoint;
        return newPoint;
    }

    #endregion

    #region PlaneHelperFunctions

    private Vector2 PointToGridPlane ( Vector2 _point, float _gridSize ) {
        return new Vector2(Mathf.FloorToInt(_point.x / _gridSize), Mathf.FloorToInt(_point.y / _gridSize));
    }

    private Vector2 GenerateNewPointPlane ( Vector2 _sourcePoint, float _minDistanceBetweenPoints ) {
        float randomDistance = Random.Range(0, _minDistanceBetweenPoints);
        float randomAngle = Random.Range(0, 2 * Mathf.PI);

        float distance = _minDistanceBetweenPoints + randomDistance;
        Vector2 newPoint = new Vector2(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle)) * distance;
        newPoint += _sourcePoint;
        return newPoint;
    }

    private bool PointsInNeighborhoodPlane (Vector2[ , ] _grid, int _gridWidth, int _gridHeight, float _gridSize, Vector2 _checkPoint, float _minDistanceBetweenPoints) {
        // Find the grid point for the point to check.
        Vector2 gridPoint = this.PointToGridPlane(_checkPoint, _gridSize);

        // Get the neighbors to check.
        List<Vector2> neighbors = this.GetNeighborhoodPlane(_grid, _gridWidth, _gridHeight, gridPoint, 5);

        // Go through each neighbor and find out if it's occupied.
        foreach(Vector2 cell in neighbors) {
            Vector2 neighborPoint = _grid[(int) cell.x, (int) cell.y];
            // See if there is a point at the neighbor cell.
            if (neighborPoint != null) {
                // Get the distance to that point from our new check point.
                float distance = Vector2.Distance(_checkPoint, neighborPoint);
                // See if the distance is closer than our minimum allowed distance.
                if (distance < _minDistanceBetweenPoints) {
                    return true;
                }
            }
        }

        // If we never found a point too close, we have no nearby neighbor.
        return false;
    }

    private List<Vector2> GetNeighborhoodPlane ( Vector2[,] _grid, int _gridWidth, int _gridHeight, Vector2 _gridPoint, int _neighborhoodSize ) {
        List<Vector2> gridCellsToCheck = new List<Vector2>();

        int radius = Mathf.FloorToInt(_neighborhoodSize / 2f);
        for (int i = 0; i < _neighborhoodSize; i++) {
            for (int j = 0; j < _neighborhoodSize; j++) {
                // Get the neighbor cell.
                int gridX = ((int) _gridPoint.x) - 2 + i;
                int gridY = ((int) _gridPoint.y) - 2 + j;

                // Check if it's outside the bounds. If it isn't, add it to the list.
                if (!(gridX < 0 || gridX > _gridWidth) && !(gridY < 0 || gridY > _gridHeight)) {
                    gridCellsToCheck.Add(new Vector2(gridX, gridY));
                }
            }
        }

        return gridCellsToCheck;
    }

    private bool InBoundsPlane (float _xMax, float _yMax, Vector2 _checkPoint) {
        return ((_checkPoint.x >= 0 && _checkPoint.x < _xMax) && (_checkPoint.y >= 0 && _checkPoint.y < _yMax));
    }

    #endregion
}
