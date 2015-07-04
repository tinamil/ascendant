﻿#region
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
using System.Diagnostics;

namespace Ascendant.Physics.Collision.React3D {

    /*  -------------------------------------------------------------------
        Class TriangleEPA :
            This class represents a triangle face of the current polytope in the EPA
            algorithm.
        -------------------------------------------------------------------
    */
    class TriangleEPA {

        uint[] indicesVertices = new uint[3];    // Indices of the vertices y_i of the triangle
        EdgeEPA[] adjacentEdges = new EdgeEPA[3];   // Three adjacent edges of the triangle (edges of other triangles)
        bool isObsolete;            // True if the triangle face is visible from the new support point
        double det;                 // Determinant
        Vector3d closestPoint;      // Point v closest to the origin on the affine hull of the triangle
        double lambda1;             // Lambda1 value such that v = lambda0 * y_0 + lambda1 * y_1 + lambda2 * y_2
        double lambda2;             // Lambda1 value such that v = lambda0 * y_0 + lambda1 * y_1 + lambda2 * y_2
        double distSquare;          // Square distance of the point closest point v to the origin

        // Access operator
        internal uint this[uint i] {
            get {
                Debug.Assert(i >= 0 && i < 3);
                return indicesVertices[i];
            }
        }

        // Return an edge of the triangle
        internal EdgeEPA getAdjacentEdge(uint index) {
            Debug.Assert(index >= 0 && index < 3);
            return adjacentEdges[index];
        }

        // Set an adjacent edge of the triangle
        void setAdjacentEdge(uint index, EdgeEPA edge) {
            Debug.Assert(index >= 0 && index < 3);
            adjacentEdges[index] = edge;
        }

        // Return the square distance  of the closest point to origin
        internal double getDistSquare() {
            return distSquare;
        }

        // Set the isObsolete value
        internal void setIsObsolete(bool isObsolete) {
            this.isObsolete = isObsolete;
        }

        // Return true if the triangle face is obsolete
        internal bool getIsObsolete() {
            return isObsolete;
        }

        // Return the point closest to the origin
        internal Vector3d getClosestPoint() {
            return closestPoint;
        }

        // Return true if the closest point on affine hull is inside the triangle
        internal bool isClosestPointInternalToTriangle() {
            return (lambda1 >= 0.0 && lambda2 >= 0.0 && (lambda1 + lambda2) <= det);
        }

        // Return true if the triangle is visible from a given vertex
        internal bool isVisibleFromVertex(Vector3d[] vertices, uint index) {
            Vector3d closestToVert = vertices[index] - closestPoint;
            return Vector3d.Dot(closestPoint, closestToVert) > 0.0;
        }

        // Compute the point of an object closest to the origin
        internal Vector3d computeClosestPointOfObject(Vector3d[] supportPointsOfObject) {
            Vector3d p0 = supportPointsOfObject[indicesVertices[0]];
            return p0 + 1.0 / det * (lambda1 * (supportPointsOfObject[indicesVertices[1]] - p0) +
                                   lambda2 * (supportPointsOfObject[indicesVertices[2]] - p0));
        }

        // Constructor
        TriangleEPA() {

        }

        // Constructor
        internal TriangleEPA(uint indexVertex1, uint indexVertex2, uint indexVertex3) {
            isObsolete = false;
            indicesVertices[0] = indexVertex1;
            indicesVertices[1] = indexVertex2;
            indicesVertices[2] = indexVertex3;
        }

        // Compute the point v closest to the origin of this triangle
        internal bool computeClosestPoint(Vector3d[] vertices) {
            Vector3d p0 = vertices[indicesVertices[0]];

            Vector3d v1 = vertices[indicesVertices[1]] - p0;
            Vector3d v2 = vertices[indicesVertices[2]] - p0;
            double v1Dotv1 = Vector3d.Dot(v1, v1);
            double v1Dotv2 = Vector3d.Dot(v1, v2);
            double v2Dotv2 = Vector3d.Dot(v2, v2);
            double p0Dotv1 = Vector3d.Dot(p0, v1);
            double p0Dotv2 = Vector3d.Dot(p0, v2);

            // Compute determinant
            det = v1Dotv1 * v2Dotv2 - v1Dotv2 * v1Dotv2;

            // Compute lambda values
            lambda1 = p0Dotv2 * v1Dotv2 - p0Dotv1 * v2Dotv2;
            lambda2 = p0Dotv1 * v1Dotv2 - p0Dotv2 * v1Dotv1;

            // If the determinant is positive
            if (det > 0.0) {
                // Compute the closest point v
                closestPoint = p0 + 1.0 / det * (lambda1 * v1 + lambda2 * v2);

                // Compute the square distance of closest point to the origin
                distSquare = Vector3d.Dot(closestPoint, closestPoint);

                return true;
            }
            return false;
        }

        // Link an edge with another one (meaning that the current edge of a triangle will
        // be associated with the edge of another triangle in order that both triangles
        // are neighbour along both edges)
        static internal bool link(EdgeEPA edge0, EdgeEPA edge1) {
            bool isPossible = (edge0.getSourceVertexIndex() == edge1.getTargetVertexIndex() &&
                               edge0.getTargetVertexIndex() == edge1.getSourceVertexIndex());

            if (isPossible) {
                edge0.ownerTriangle.adjacentEdges[edge0.index] = edge1;
                edge1.ownerTriangle.adjacentEdges[edge1.index] = edge0;
            }

            return isPossible;
        }

        // Make an half link of an edge with another one from another triangle. An half-link
        // between an edge "edge0" and an edge "edge1" represents the fact that "edge1" is an
        // adjacent edge of "edge0" but not the opposite. The opposite edge connection will
        // be made later.
        internal static void halfLink(EdgeEPA edge0, EdgeEPA edge1) {
            Debug.Assert(edge0.getSourceVertexIndex() == edge1.getTargetVertexIndex() &&
                   edge0.getTargetVertexIndex() == edge1.getSourceVertexIndex());

            // Link
            edge0.ownerTriangle.adjacentEdges[edge0.index] = edge1;
        }

        // Execute the recursive silhouette algorithm from this triangle face
        // The parameter "vertices" is an array that contains the vertices of the current polytope and the
        // parameter "indexNewVertex" is the index of the new vertex in this array. The goal of the silhouette algorithm is
        // to add the new vertex in the polytope by keeping it convex. Therefore, the triangle faces that are visible from the
        // new vertex must be removed from the polytope and we need to add triangle faces where each face contains the new vertex
        // and an edge of the silhouette. The silhouette is the connected set of edges that are part of the border between faces that
        // are seen and faces that are not seen from the new vertex. This method starts from the nearest face from the new vertex,
        // computes the silhouette and create the new faces from the new vertex in order that we always have a convex polytope. The
        // faces visible from the new vertex are set obselete and will not be considered as being a candidate face in the future.
        internal bool computeSilhouette(Vector3d[] vertices, uint indexNewVertex, ref TriangleStore triangleStore) {

            uint first = triangleStore.nbTriangles;

            // Mark the current triangle as obsolete because it
            setIsObsolete(true);

            // Execute recursively the silhouette algorithm for the ajdacent edges of neighbouring
            // triangles of the current triangle
            bool result = adjacentEdges[0].computeSilhouette(vertices, indexNewVertex, ref triangleStore) &&
                          adjacentEdges[1].computeSilhouette(vertices, indexNewVertex, ref triangleStore) &&
                          adjacentEdges[2].computeSilhouette(vertices, indexNewVertex, ref triangleStore);

            if (result) {
                uint i, j;

                // For each triangle face that contains the new vertex and an edge of the silhouette
                for (i = first, j = triangleStore.nbTriangles - 1; i != triangleStore.nbTriangles; j = i++) {
                    TriangleEPA triangle = triangleStore[i];
                    halfLink(triangle.getAdjacentEdge(1), new EdgeEPA(triangle, 1));

                    if (!link(new EdgeEPA(triangle, 0), new EdgeEPA(triangleStore[j], 2))) {
                        return false;
                    }
                }

            }

            return result;
        }
    }   // End of ReactPhysics3D namespace

}

