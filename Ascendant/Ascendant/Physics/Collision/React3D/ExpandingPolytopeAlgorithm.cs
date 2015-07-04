#region
/********************************************************************************
       * ReactPhysics3D physics library, http://code.google.com/p/reactphysics3d/      *
       * Copyright (c) 2011 Daniel Chappuis                                            *
       *********************************************************************************
       *                                                                               *
       * Permission is hereby granted, free of charge, to any person obtaining a copy  *
       * of this software and associated documentation files (the "Software"), to deal *
       * in the Software without restriction, including without limitation the rights  *
       * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell     *
       * copies of the Software, and to permit persons to whom the Software is         *
       * furnished to do so, subject to the following conditions:                      *
       *                                                                               *
       * The above copyright notice and this permission notice shall be included in    *
       * all copies or substantial portions of the Software.                           *
       *                                                                               *
       * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR    *
       * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,      *
       * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE   *
       * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER        *
       * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, *
       * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN     *
       * THE SOFTWARE.                                                                 *
       ********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Ascendant.Physics.Collision.React3D;
using System.Diagnostics;

namespace Ascendant.Physics.Collision {

    /*  -------------------------------------------------------------------
        Class EPAAlgorithm :
            This class is the implementation of the Expanding Polytope Algorithm (EPA).
            The EPA algorithm computes the penetration depth and contact points between
            two enlarged objects (with margin) where the original objects (without margin)
            intersect. The penetration depth of a pair of intersecting objects A and B is
            the length of a point on the boundary of the Minkowski sum (A-B) closest to the
            origin. The goal of the EPA algorithm is to start with an initial simplex polytope
            that contains the origin and expend it in order to find the point on the boundary
            of (A-B) that is closest to the origin. An initial simplex that contains origin
            has been computed wit GJK algorithm. The EPA Algorithm will extend this simplex
            polytope to find the correct penetration depth. The implementation of the EPA
            algorithm is based on the book "Collision Detection in 3D Environments".
        -------------------------------------------------------------------
    */
    class ExpandingPolytopeAlgorithm {

        // Constants
        const uint MAX_SUPPORT_POINTS = 100;    // Maximum number of support points of the polytope
        const uint MAX_FACETS = 200;            // Maximum number of facets of the polytope

        // Constants
        const double REL_ERROR = 1.0e-3;
        const double REL_ERROR_SQUARE = REL_ERROR * REL_ERROR;

        // Class TriangleComparison that allow the comparison of two triangles in the heap
        // The comparison between two triangles is made using their square distance to the closest
        // point to the origin. The goal is that in the heap, the first triangle is the one with the
        // smallest square distance.
        class TriangleComparison : IComparer<TriangleEPA> {
            public int Compare(TriangleEPA face1, TriangleEPA face2) {
                return (face1.getDistSquare().CompareTo(face2.getDistSquare()));
            }
        };


        TriangleComparison triangleComparison = new TriangleComparison();           // Triangle comparison operator


        // Add a triangle face in the candidate triangle heap in the EPA algorithm
        void addFaceCandidate(TriangleEPA triangle, SortedSet<TriangleEPA> heap, ref uint nbTriangles, double upperBoundSquarePenDepth) {

            // If the closest point of the affine hull of triangle points is internal to the triangle and
            // if the distance of the closest point from the origin is at most the penetration depth upper bound
            if (triangle.isClosestPointInternalToTriangle() && triangle.getDistSquare() <= upperBoundSquarePenDepth) {
                // Add the triangle face to the list of candidates
                heap.Add(triangle);
                nbTriangles++;
            }
        }

        // Decide if the origin is in the tetrahedron
        // Return 0 if the origin is in the tetrahedron and return the number (1,2,3 or 4) of
        // the vertex that is wrong if the origin is not in the tetrahedron
        int isOriginInTetrahedron(Vector3d p1, Vector3d p2, Vector3d p3, Vector3d p4) {

            // Check vertex 1
            Vector3d normal1 = Vector3d.Cross((p2 - p1), (p3 - p1));
            if (Vector3d.Dot(normal1, p1) > 0.0 == Vector3d.Dot(normal1, p4) > 0.0) {
                return 4;
            }

            // Check vertex 2
            Vector3d normal2 = Vector3d.Cross((p4 - p2), (p3 - p2));
            if (Vector3d.Dot(normal2, p2) > 0.0 == Vector3d.Dot(normal2, p1) > 0.0) {
                return 1;
            }

            // Check vertex 3
            Vector3d normal3 = Vector3d.Cross((p4 - p3), (p1 - p3));
            if (Vector3d.Dot(normal3, p3) > 0.0 == Vector3d.Dot(normal3, p2) > 0.0) {
                return 2;
            }

            // Check vertex 4
            Vector3d normal4 = Vector3d.Cross((p2 - p4), (p1 - p4));
            if (Vector3d.Dot(normal4, p4) > 0.0 == Vector3d.Dot(normal4, p3) > 0.0) {
                return 3;
            }

            // The origin is in the tetrahedron, we return 0
            return 0;
        }

        private int getMinAxis(Vector3d v) {
            return (v.X < v.Y ? (v.X < v.Z ? 0 : 2) : (v.Y < v.Z ? 1 : 2));
        }

        // Compute the penetration depth with the EPA algorithms
        // This method computes the penetration depth and contact points between two
        // enlarged objects (with margin) where the original objects (without margin)
        // intersect. An initial simplex that contains origin has been computed with
        // GJK algorithm. The EPA Algorithm will extend this simplex polytope to find
        // the correct penetration depth
        internal bool computePenetrationDepthAndContactPoints(Simplex simplex, MovableObject boundingVolume1, MovableObject boundingVolume2, out Vector3d contactPoint, out ContactInfo contactInfo) {

            var suppPointsA = new Vector3d[MAX_SUPPORT_POINTS];       // Support points of object A in local coordinates
            var suppPointsB = new Vector3d[MAX_SUPPORT_POINTS];       // Support points of object B in local coordinates
            var points = new Vector3d[MAX_SUPPORT_POINTS];            // Current points
            TriangleStore triangleStore = new TriangleStore();                   // Store the triangles
            SortedSet<TriangleEPA> triangleHeap = new SortedSet<TriangleEPA>(triangleComparison);          // Heap that contains the face candidate of the EPA algorithm

            // Get the simplex computed previously by the GJK algorithm
            uint nbVertices = simplex.getSimplex(suppPointsA, suppPointsB, points);

            // Compute the tolerance
            double tolerance = double.Epsilon * simplex.getMaxLengthSquareOfAPoint();

            // Number of triangles in the polytope
            uint nbTriangles = 0;

            // Clear the storing of triangles
            triangleStore.clear();

            contactPoint = Vector3d.Zero;
            contactInfo = null;
            // Select an action according to the number of points in the simplex
            // computed with GJK algorithm in order to obtain an initial polytope for
            // The EPA algorithm.
            switch (nbVertices) {
                case 1:
                    // Only one point in the simplex (which should be the origin). We have a touching contact
                    // with zero penetration depth. We drop that kind of contact. Therefore, we return false
                    return false;
                case 2: {
                        // The simplex returned by GJK is a line segment d containing the origin.
                        // We add two additional support points to construct a hexahedron (two tetrahedron
                        // glued together with triangle faces. The idea is to compute three different vectors
                        // v1, v2 and v3 that are orthogonal to the segment d. The three vectors are relatively
                        // rotated of 120 degree around the d segment. The the three new points to
                        // construct the polytope are the three support points in those three directions
                        // v1, v2 and v3.

                        // Direction of the segment
                        Vector3d d = (points[1] - points[0]).Normalized();

                        // Choose the coordinate axis from the minimal absolute component of the vector d
                        int minAxis = getMinAxis(new Vector3d(Math.Abs(d.X), Math.Abs(d.Y), Math.Abs(d.Z)));

                        // Compute sin(60)
                        double sin60 = Math.Sqrt(3.0) * 0.5;

                        // Create a rotation quaternion to rotate the vector v1 to get the vectors
                        // v2 and v3
                        Quaterniond rotationQuat = new Quaterniond(d.X * sin60, d.Y * sin60, d.Z * sin60, 0.5);

                        // Compute the vector v1, v2, v3

                        Vector3d v1 = Vector3d.Cross(d, new Vector3d(minAxis == 0 ? 1 : 0, minAxis == 1 ? 1 : 0, minAxis == 2 ? 1 : 0));
                        Vector3d v2 = Vector3d.Transform(v1, rotationQuat);
                        Vector3d v3 = Vector3d.Transform(v2, rotationQuat);

                        // Compute the support point in the direction of v1
                        suppPointsA[2] = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(v1);
                        suppPointsB[2] = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(-v1);
                        points[2] = suppPointsA[2] - suppPointsB[2];

                        // Compute the support point in the direction of v2
                        suppPointsA[3] = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(v2);
                        suppPointsB[3] = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(-v2);
                        points[3] = suppPointsA[3] - suppPointsB[3];

                        // Compute the support point in the direction of v3
                        suppPointsA[4] = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(v3);
                        suppPointsB[4] = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(-v3);
                        points[4] = suppPointsA[4] - suppPointsB[4];

                        // Now we have an hexahedron (two tetrahedron glued together). We can simply keep the
                        // tetrahedron that contains the origin in order that the initial polytope of the
                        // EPA algorithm is a tetrahedron, which is simpler to deal with.

                        // If the origin is in the tetrahedron of points 0, 2, 3, 4
                        if (isOriginInTetrahedron(points[0], points[2], points[3], points[4]) == 0) {
                            // We use the point 4 instead of point 1 for the initial tetrahedron
                            suppPointsA[1] = suppPointsA[4];
                            suppPointsB[1] = suppPointsB[4];
                            points[1] = points[4];
                        } else if (isOriginInTetrahedron(points[1], points[2], points[3], points[4]) == 0) {  // If the origin is in the tetrahedron of points 1, 2, 3, 4
                            // We use the point 4 instead of point 0 for the initial tetrahedron
                            suppPointsA[0] = suppPointsA[0];
                            suppPointsB[0] = suppPointsB[0];
                            points[0] = points[0];
                        } else {
                            // The origin is not in the initial polytope
                            return false;
                        }

                        // The polytope contains now 4 vertices
                        nbVertices = 4;

                        goto case 4;
                    }
                case 4: {
                        // The simplex computed by the GJK algorithm is a tetrahedron. Here we check
                        // if this tetrahedron contains the origin. If it is the case, we keep it and
                        // otherwise we remove the wrong vertex of the tetrahedron and go in the case
                        // where the GJK algorithm compute a simplex of three vertices.

                        // Check if the tetrahedron contains the origin (or wich is the wrong vertex otherwise)
                        int badVertex = isOriginInTetrahedron(points[0], points[1], points[2], points[3]);

                        // If the origin is in the tetrahedron
                        if (badVertex == 0) {
                            // The tetrahedron is a correct initial polytope for the EPA algorithm.
                            // Therefore, we construct the tetrahedron.

                            // Comstruct the 4 triangle faces of the tetrahedron
                            TriangleEPA face0 = triangleStore.newTriangle(points, 0, 1, 2);
                            TriangleEPA face1 = triangleStore.newTriangle(points, 0, 3, 1);
                            TriangleEPA face2 = triangleStore.newTriangle(points, 0, 2, 3);
                            TriangleEPA face3 = triangleStore.newTriangle(points, 1, 3, 2);

                            // If the constructed tetrahedron is not correct
                            if (!(face0 != null && face1 != null && face2 != null && face3 != null && face0.getDistSquare() > 0.0 &&
                                  face1.getDistSquare() > 0.0 && face2.getDistSquare() > 0.0 && face3.getDistSquare() > 0.0)) {
                                return false;
                            }

                            // Associate the edges of neighbouring triangle faces
                            TriangleEPA.link(new EdgeEPA(face0, 0), new EdgeEPA(face1, 2));
                            TriangleEPA.link(new EdgeEPA(face0, 1), new EdgeEPA(face3, 2));
                            TriangleEPA.link(new EdgeEPA(face0, 2), new EdgeEPA(face2, 0));
                            TriangleEPA.link(new EdgeEPA(face1, 0), new EdgeEPA(face2, 2));
                            TriangleEPA.link(new EdgeEPA(face1, 1), new EdgeEPA(face3, 0));
                            TriangleEPA.link(new EdgeEPA(face2, 1), new EdgeEPA(face3, 1));

                            // Add the triangle faces in the candidate heap
                            addFaceCandidate(face0, triangleHeap, ref nbTriangles, double.MaxValue);
                            addFaceCandidate(face1, triangleHeap, ref nbTriangles, double.MaxValue);
                            addFaceCandidate(face2, triangleHeap, ref nbTriangles, double.MaxValue);
                            addFaceCandidate(face3, triangleHeap, ref nbTriangles, double.MaxValue);

                            break;
                        }

                        // If the tetrahedron contains a wrong vertex (the origin is not inside the tetrahedron)
                        if (badVertex < 4) {
                            // Replace the wrong vertex with the point 5 (if it exists)
                            suppPointsA[badVertex - 1] = suppPointsA[4];
                            suppPointsB[badVertex - 1] = suppPointsB[4];
                            points[badVertex - 1] = points[4];
                        }

                        // We have removed the wrong vertex
                        nbVertices = 3;
                        goto case 3;
                    }
                case 3: {
                        // The GJK algorithm returned a triangle that contains the origin.
                        // We need two new vertices to obtain a hexahedron. The two new vertices
                        // are the support points in the "n" and "-n" direction where "n" is the
                        // normal of the triangle.

                        // Compute the normal of the triangle
                        Vector3d v1 = points[1] - points[0];
                        Vector3d v2 = points[2] - points[0];
                        Vector3d n = Vector3d.Cross(v1, v2);

                        // Compute the two new vertices to obtain a hexahedron
                        suppPointsA[3] = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(n);
                        suppPointsB[3] = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(-n);
                        points[3] = suppPointsA[3] - suppPointsB[3];
                        suppPointsA[4] = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(-n);
                        suppPointsB[4] = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(n);
                        points[4] = suppPointsA[4] - suppPointsB[4];


                        TriangleEPA face0 = null;
                        TriangleEPA face1 = null;
                        TriangleEPA face2 = null;
                        TriangleEPA face3 = null;

                        // If the origin is in the first tetrahedron
                        if (isOriginInTetrahedron(points[0], points[1],
                                                  points[2], points[3]) == 0) {
                            // The tetrahedron is a correct initial polytope for the EPA algorithm.
                            // Therefore, we construct the tetrahedron.

                            // Comstruct the 4 triangle faces of the tetrahedron
                            face0 = triangleStore.newTriangle(points, 0, 1, 2);
                            face1 = triangleStore.newTriangle(points, 0, 3, 1);
                            face2 = triangleStore.newTriangle(points, 0, 2, 3);
                            face3 = triangleStore.newTriangle(points, 1, 3, 2);
                        } else if (isOriginInTetrahedron(points[0], points[1],
                                                         points[2], points[4]) == 0) {

                            // The tetrahedron is a correct initial polytope for the EPA algorithm.
                            // Therefore, we construct the tetrahedron.

                            // Comstruct the 4 triangle faces of the tetrahedron
                            face0 = triangleStore.newTriangle(points, 0, 1, 2);
                            face1 = triangleStore.newTriangle(points, 0, 4, 1);
                            face2 = triangleStore.newTriangle(points, 0, 2, 4);
                            face3 = triangleStore.newTriangle(points, 1, 4, 2);
                        } else {
                            return false;
                        }

                        // If the constructed tetrahedron is not correct
                        if (!((face0 != null) && (face1 != null) && (face2 != null) && (face3 != null)
                           && face0.getDistSquare() > 0.0 && face1.getDistSquare() > 0.0
                           && face2.getDistSquare() > 0.0 && face3.getDistSquare() > 0.0)) {
                            return false;
                        }

                        // Associate the edges of neighbouring triangle faces
                        TriangleEPA.link(new EdgeEPA(face0, 0), new EdgeEPA(face1, 2));
                        TriangleEPA.link(new EdgeEPA(face0, 1), new EdgeEPA(face3, 2));
                        TriangleEPA.link(new EdgeEPA(face0, 2), new EdgeEPA(face2, 0));
                        TriangleEPA.link(new EdgeEPA(face1, 0), new EdgeEPA(face2, 2));
                        TriangleEPA.link(new EdgeEPA(face1, 1), new EdgeEPA(face3, 0));
                        TriangleEPA.link(new EdgeEPA(face2, 1), new EdgeEPA(face3, 1));

                        // Add the triangle faces in the candidate heap
                        addFaceCandidate(face0, triangleHeap, ref nbTriangles, double.MaxValue);
                        addFaceCandidate(face1, triangleHeap, ref nbTriangles, double.MaxValue);
                        addFaceCandidate(face2, triangleHeap, ref nbTriangles, double.MaxValue);
                        addFaceCandidate(face3, triangleHeap, ref nbTriangles, double.MaxValue);

                        nbVertices = 4;
                    }
                    break;
            }

            // At this point, we have a polytope that contains the origin. Therefore, we
            // can run the EPA algorithm.

            if (nbTriangles == 0) {
                return false;
            }

            TriangleEPA triangle = null;
            double upperBoundSquarePenDepth = double.MaxValue;

            do {
                triangle = triangleHeap.Min;
                // Get the next candidate face (the face closest to the origin)
                triangleHeap.Remove(triangle);
                nbTriangles--;

                // If the candidate face in the heap is not obsolete
                if (!triangle.getIsObsolete()) {
                    // If we have reached the maximum number of support points
                    if (nbVertices == MAX_SUPPORT_POINTS) {
                        Debug.Assert(false);
                        break;
                    }

                    // Compute the support point of the Minkowski difference (A-B) in the closest point direction
                    suppPointsA[nbVertices] = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(triangle.getClosestPoint());
                    suppPointsB[nbVertices] = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(-triangle.getClosestPoint());
                    points[nbVertices] = suppPointsA[nbVertices] - suppPointsB[nbVertices];

                    uint indexNewVertex = nbVertices;
                    nbVertices++;

                    // Update the upper bound of the penetration depth
                    double wDotv = Vector3d.Dot(points[indexNewVertex], triangle.getClosestPoint());
                    Debug.Assert(wDotv > 0.0);
                    double wDotVSquare = wDotv * wDotv / triangle.getDistSquare();
                    if (wDotVSquare < upperBoundSquarePenDepth) {
                        upperBoundSquarePenDepth = wDotVSquare;
                    }

                    // Compute the error
                    double error = wDotv - triangle.getDistSquare();
                    if (error <= Math.Max(tolerance, REL_ERROR_SQUARE * wDotv) ||
                        points[indexNewVertex] == points[triangle[0]] ||
                        points[indexNewVertex] == points[triangle[1]] ||
                        points[indexNewVertex] == points[triangle[2]]) {
                        break;
                    }

                    // Now, we compute the silhouette cast by the new vertex. The current triangle
                    // face will not be in the convex hull. We start the local recursive silhouette
                    // algorithm from the current triangle face.
                    uint i = triangleStore.nbTriangles;
                    if (!triangle.computeSilhouette(points, indexNewVertex, ref triangleStore)) {
                        break;
                    }

                    // Add all the new triangle faces computed with the silhouette algorithm
                    // to the candidates list of faces of the current polytope
                    while (i != triangleStore.nbTriangles) {
                        TriangleEPA newTriangle = triangleStore[i];
                        addFaceCandidate(newTriangle, triangleHeap, ref nbTriangles, upperBoundSquarePenDepth);
                        i++;
                    }
                }

            } while (nbTriangles > 0 && triangleHeap.Min.getDistSquare() <= upperBoundSquarePenDepth);

            // Compute the contact info
            contactPoint = triangle.getClosestPoint();
            Vector3d pA = triangle.computeClosestPointOfObject(suppPointsA);
            Vector3d pB = triangle.computeClosestPointOfObject(suppPointsB);
            Vector3d normal = contactPoint.Normalized();
            double penetrationDepth = contactPoint.Length;
            Debug.Assert(penetrationDepth > 0.0);
            contactInfo = new ContactInfo(boundingVolume1, boundingVolume2, normal, penetrationDepth, pA, pB);

            return true;
        }
    }
}