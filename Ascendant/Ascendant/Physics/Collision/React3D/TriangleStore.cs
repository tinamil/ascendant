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
using System.Diagnostics;
using OpenTK;

namespace Ascendant.Physics.Collision.React3D {

    /*  -------------------------------------------------------------------
        Class TrianglesStore :
            This class stores several triangles of the polytope in the EPA
            algorithm.
        -------------------------------------------------------------------
    */
    class TriangleStore {


        // Constants
        const uint MAX_TRIANGLES = 200;     // Maximum number of triangles


        internal TriangleEPA[] triangles = new TriangleEPA[MAX_TRIANGLES];       // Triangles
        internal uint nbTriangles { get; set; }                            // Number of triangles

        public TriangleStore() {
            nbTriangles = 0;
        }

        // Clear all the storage
        internal void clear() {
            nbTriangles = 0;
        }

        // Return the last triangle
        TriangleEPA last() {
            Debug.Assert(nbTriangles > 0);
            return triangles[nbTriangles - 1];
        }

        // Create a new triangle
        internal TriangleEPA newTriangle(Vector3d[] vertices, uint v0, uint v1, uint v2) {
            TriangleEPA newTriangle = null;

            // If we have not reach the maximum number of triangles
            if (nbTriangles != MAX_TRIANGLES) {
                newTriangle = triangles[nbTriangles++] = new TriangleEPA(v0, v1, v2);
                if (!newTriangle.computeClosestPoint(vertices)) {
                    nbTriangles--;
                    triangles[nbTriangles] = newTriangle = null;
                }
            }

            // Return the new triangle
            return newTriangle;
        }

        // Access operator
        internal TriangleEPA this[uint i] {
            get {
                return triangles[i];
            }
        }

    }   // End of ReactPhysics3D namespace

}
