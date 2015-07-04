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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using OpenTK;

namespace Ascendant.Physics.Collision.React3D {
    class EdgeEPA {

        internal TriangleEPA ownerTriangle { get; private set; }       // Pointer to the triangle that contains this edge

        internal uint index { get; private set; }                          // Index of the edge in the triangle (between 0 and 2). 
        // The edge with index i connect triangle vertices i and (i+1 % 3)


        // Return the index of the next counter-clockwise edge of the ownver triangle
        uint indexOfNextCounterClockwiseEdge(uint i) {
            return (i + 1) % 3;
        }

        // Return the index of the previous counter-clockwise edge of the ownver triangle
        uint indexOfPreviousCounterClockwiseEdge(uint i) {
            return (i + 2) % 3;
        }

        // Constructor
        internal EdgeEPA(TriangleEPA ownerTriangle, uint index) {
            Debug.Assert(index >= 0 && index < 3);
            this.ownerTriangle = ownerTriangle;
            this.index = index;
        }

        // Return the index of the source vertex of the edge (vertex starting the edge)
        internal uint getSourceVertexIndex() {
            return ownerTriangle[index];
        }

        // Return the index of the target vertex of the edge (vertex ending the edge)
        internal uint getTargetVertexIndex() {
            return ownerTriangle[indexOfNextCounterClockwiseEdge(index)];
        }

        // Execute the recursive silhouette algorithm from this edge
        internal bool computeSilhouette(Vector3d[] vertices, uint indexNewVertex, ref TriangleStore triangleStore) {
            // If the edge has not already been visited
            if (!ownerTriangle.getIsObsolete()) {
                // If the triangle of this edge is not visible from the given point
                if (!ownerTriangle.isVisibleFromVertex(vertices, indexNewVertex)) {
                    TriangleEPA triangle = triangleStore.newTriangle(vertices, indexNewVertex, getTargetVertexIndex(), getSourceVertexIndex());

                    // If the triangle has been created
                    if (triangle != null) {
                        TriangleEPA.halfLink(new EdgeEPA(triangle, 1), this);
                        return true;
                    }

                    return false;
                } else {
                    // The current triangle is visible and therefore obsolete
                    ownerTriangle.setIsObsolete(true);

                    uint backup = triangleStore.nbTriangles;

                    if (!ownerTriangle.getAdjacentEdge(indexOfNextCounterClockwiseEdge(this.index)).computeSilhouette(vertices, indexNewVertex, ref triangleStore)) {
                        ownerTriangle.setIsObsolete(false);

                        TriangleEPA triangle = triangleStore.newTriangle(vertices, indexNewVertex, getTargetVertexIndex(), getSourceVertexIndex());

                        // If the triangle has been created
                        if (triangle != null) {
                            TriangleEPA.halfLink(new EdgeEPA(triangle, 1), this);
                            return true;
                        }

                        return false;
                    } else if (!ownerTriangle.getAdjacentEdge(indexOfPreviousCounterClockwiseEdge(this.index)).computeSilhouette(vertices, indexNewVertex, ref triangleStore)) {
                        ownerTriangle.setIsObsolete(false);

                        triangleStore.nbTriangles = backup;

                        TriangleEPA triangle = triangleStore.newTriangle(vertices, indexNewVertex, getTargetVertexIndex(), getSourceVertexIndex());

                        if (triangle != null) {
                            TriangleEPA.halfLink(new EdgeEPA(triangle, 1), this);
                            return true;
                        }

                        return false;
                    }
                }
            }
            return true;
        }
    }
}
