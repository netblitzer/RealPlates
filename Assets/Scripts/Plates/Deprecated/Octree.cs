using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// This octree is built with only points needing to intersect a tree to be inserted. This
/// improves performance for collision checks.
/// </summary>
public class Octree {

    // How deep the tree is allowed to go.
    const int maxDepth = 7;

    // How many objects to contain before subdividing.
    const int maxObjects = 8;

    // The current list of gameobjects contained by this division.
    //public List<MantlePoint> containedObjects;

    // The depth of this division.
    public int depth;

    // The parent division of this tree (if there is one).
    Octree parent;

    // The immediate subdivisions of this octree.
    Octree[] subDivisions;

    float maxParticleSize;

    bool canBeSubdivided = true;

    // The boundary that this octTree is defined by.
    public CubeBoundary Boundary { get; private set; }

    public Octree ( float _side ) : this(Vector3.zero, _side, 1f) { }

    public Octree ( Vector3 _center, float _side, float _particleSize, int _depth = 1 ) {
        this.Boundary = new CubeBoundary(_center, _side);
        this.depth = _depth;
        this.maxParticleSize = _particleSize;

        if (_side < _particleSize * 4f) {
            this.canBeSubdivided = false;
        }
    }

    public Octree ( CubeBoundary _boundary, float _particleSize, int _depth = 1, Octree _parent = null ) {
        this.Boundary = _boundary;
        this.parent = _parent;
        this.depth = _depth;
        this.maxParticleSize = _particleSize;

        if (_boundary.SideLength < _particleSize * 4f) {
            this.canBeSubdivided = false;
        }
    }

    public int GetNodeCount () {
        int nodes = 1;

        if (this.subDivisions != null) {
            foreach (Octree tree in this.subDivisions) {
                nodes += tree.GetNodeCount();
            }
        }

        return nodes;
    }

    /// <summary>
    /// Attempts to insert a point into the octree. If it has to, it will subdivide and insert the point
    /// into a lower subdivision.
    /// </summary>
    /// <param name="_object">The object to insert into the octree.</param>
    /// <returns>True if the point is inserted into the octree successfully.</returns>
    /*
    public bool InsertObject ( MantlePoint _object ) {
        // Make sure the point intersects with the octree.
        if (!this.Boundary.IntersectsSphere(_object.location, _object.radius)) {
            return false;
        }

        // If our list doesn't exist yet, create it.
        if (this.containedObjects == null) {
            this.containedObjects = new List<MantlePoint>();
        }

        // See if we're under our limit of objects to contain and we haven't subdivided yet.
        //  Otherwise we'll need to insert it into a child division if possible.
        if (this.subDivisions == null && this.containedObjects.Count < maxObjects) {
            this.containedObjects.Add(_object);
            _object.octDepth = this.depth;
            return true;
        }

        // If we're at our limit for contained objects and we haven't subdivided yet, divide the tree.
        //  Only do this if we're not at the max depth.
        if (this.subDivisions == null && this.depth < maxDepth && this.canBeSubdivided) {
            this.Subdivide();
        }
        else if (this.depth >= maxDepth || !this.canBeSubdivided) {
            // If we're as deep as the tree can go, insert it into our tree even if we're past the limit.
            this.containedObjects.Add(_object);
            _object.octDepth = this.depth;
            return true;
        }

        // If we've subdivided, try to insert it into one of our children.
        bool wasInserted = false;
        for (int i = 0; i < 8; i++) {
            if (this.subDivisions[i].InsertObject(_object)) {
                wasInserted = true;
            }
        }
        if (wasInserted) {
            return true;
        }

        // If a child couldn't hold the object, return false.
        return false;
    }
    

    private void Subdivide ( ) {
        // Create the array of subdivisions.
        this.subDivisions = new Octree[8];

        // Easy access values.
        Vector3 ourCenter = this.Boundary.Center;
        float ourSide = this.Boundary.SideLength;
        float newSide = ourSide / 2f;

        // Create new octrees at each corner of the cube.
        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 2; j++) {
                for (int k = 0; k < 2; k++) {
                    Vector3 octCenter = new Vector3(ourCenter.x - (newSide / 2f) + (newSide * i),
                        ourCenter.y - (newSide / 2f) + (newSide * j),
                        ourCenter.z - (newSide / 2f) + (newSide * k));
                    CubeBoundary newBoundary = new CubeBoundary(octCenter, newSide);
                    this.subDivisions[(i * 4) + (j * 2) + k] = new Octree(newBoundary, this.maxParticleSize, this.depth + 1, this);
                }
            }
        }

        // Try to insert the contained objects of this tree into our new children if possible.
        //  At least one child should be able to hold the object.
        for (int i = 0; i < this.containedObjects.Count; i++) {
            MantlePoint _object = this.containedObjects[i];
            for (int j = 0; j < 8; j++) {
                if (this.subDivisions[j].InsertObject(_object)) {
                    break;
                }
            }

            // Once it's in our children, we can remove it from the parent.
            this.containedObjects.RemoveAt(i);
            i--;
        }
    }

    public List<MantlePoint> SphereIntersectsContainedPoints ( Vector3 _center, float _radius ) {
        if (this.Boundary.IntersectsSphere(_center, _radius)) {
            // Create a list of points that may be intersected.
            List<MantlePoint> intersectedPoints = new List<MantlePoint>();

            // Go through this tree's contained points and add them to the list if possible.
            if (this.containedObjects != null) {
                for (int i = 0; i < this.containedObjects.Count; i++) {
                    float dist = Vector3.Distance(_center, this.containedObjects[i].location);

                    if (dist < _radius + this.containedObjects[i].radius) {
                        intersectedPoints.Add(this.containedObjects[i]);
                    }
                }
            }

            // If we've subdivided, do the same with our children.
            if (this.subDivisions != null) {
                for (int i = 0; i < 8; i++) {
                    List<MantlePoint> childPoints = this.subDivisions[i].SphereIntersectsContainedPoints(_center, _radius);

                    if (childPoints != null) {
                        intersectedPoints.AddRange(childPoints);
                    }
                }
            }

            // If nothing in our tree or our children intersected, return null.
            if (intersectedPoints.Count == 0) {
                return null;
            }

            // Otherwise return the list of intersected points.
            return intersectedPoints;
        }

        // If we have no points to intersect, by default return null.
        return null;
    }

    /*
    public void UpdateOctree ( ) {
        // Go through each of our points to see if they have moved out of this tree and into other trees.
        if (this.containedObjects != null && this.containedObjects.Count > 0) {
            for (int i = 0; i < this.containedObjects.Count; i++) {
                MantlePoint _object = this.containedObjects[i];

                // See if the point has already moved through the tree yet.
                if (!_object.movedThroughTree) {
                    // If it hasn't, move it through the tree.
                    this.UpdatePointInTree(_object);
                }
                else {
                    // If the object already has moved, make sure we intersect with it still. If we don't, remove it from our objects list.
                    if (!this.Boundary.IntersectsSphere(_object.location, _object.radius)) {
                        this.containedObjects.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        // If this tree has children, update them now.
        if (this.subDivisions != null) {
            foreach (Octree tree in this.subDivisions) {
                tree.UpdateOctree();
            }
        }
    }
    */
    /*
    private void UpdatePointInTree (MantlePoint _object) {
        // First see if the object is fully contained by the parent. If it's not, we need to keep
        //  moving up until we find the tree that does fully contain it, then go back down.
        if (this.parent != null) {
            if (this.parent.Boundary.ContainsSphere(_object.location, _object.radius)) {
                // If it fits in the parent, try to insert it into all the children of that tree.
                for (int j = 0; j < 8; j++) {
                    if (this.parent.subDivisions[j].UpdateInsert(_object)) {
                        break;
                    }
                }
            }
            else {
                // If it doesn't fit into the tree, keep going up until we find a tree that completely contains the object.
                this.parent.UpdatePointInTree(_object);
            }

            // Make sure to mark that the object has been placed into the trees that should contain it.
            _object.movedThroughTree = true;
        }
        else {
            // If we're the root tree and still contain objects, then we need to see if we need to subdivide and insert the
            //  object into any children we get.
            if (this.containedObjects.Count >= maxObjects) {
                this.Subdivide();

                // Make sure to mark that the object has been placed into the trees that should contain it.
                _object.movedThroughTree = true;
            }
        }
    }
    */

        /*
    private bool UpdateInsert (MantlePoint _object) {
        // Make sure the point intersects with the octree.
        if (!this.Boundary.IntersectsSphere(_object.location, _object.radius)) {
            return false;
        }

        // If our list doesn't exist yet, create it.
        if (this.containedObjects == null) {
            this.containedObjects = new List<MantlePoint>();
        }

        // See if the object is already in this tree.
        if (this.containedObjects.Contains(_object)) {
            return false;
        }

        // See if we're under our limit of objects to contain and we haven't subdivided yet.
        //  Otherwise we'll need to insert it into a child division if possible.
        if (this.subDivisions == null && this.containedObjects.Count < maxObjects) {
            this.containedObjects.Add(_object);
            _object.octDepth = this.depth;
            return true;
        }
        else if (this.depth >= maxDepth || !this.canBeSubdivided) {
            // If we're as deep as the tree can go, insert it into our tree even if we're past the limit.
            this.containedObjects.Add(_object);
            _object.octDepth = this.depth;
            return true;
        }

        // If we're at our limit for contained objects and we haven't subdivided yet, divide the tree.
        //  Only do this if we're not at the max depth.
        if (this.subDivisions == null && this.depth < maxDepth) {
            this.Subdivide();
        }
        else if (this.depth >= maxDepth || !this.canBeSubdivided && this.canBeSubdivided) {
            // If we're as deep as the tree can go, insert it into our tree even if we're past the limit.
            this.containedObjects.Add(_object);
            _object.octDepth = this.depth;
            return true;
        }

        // If we've subdivided, try to insert it into one of our children.
        bool wasInserted = false;
        for (int i = 0; i < 8; i++) {
            if (this.subDivisions[i].UpdateInsert(_object)) {
                wasInserted = true;
            }
        }
        if (wasInserted) {
            return true;
        }

        // If a child couldn't hold the object, return false.
        return false;
    }

    public bool InsertIntoParent (MantlePoint _object) {
        if (this.Boundary.ContainsSphere(_object.location, _object.radius) || this.depth == 1) {
            return this.InsertObject(_object);
        }
        else {
            return this.parent.InsertIntoParent(_object);
        }
    }
    */
}