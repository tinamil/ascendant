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
/*  -------------------------------------------------------------------
    Class GJKAlgorithm :
        This class implements a narrow-phase collision detection algorithm. This
        algorithm uses the ISA-GJK algorithm and the EPA algorithm. This
        implementation is based on the implementation discussed in the book
        "Collision Detection in 3D Environments".
        This method implements the Hybrid Technique for calculating the
        penetration depth. The two objects are enlarged with a small margin. If
        the object intersection, the penetration depth is quickly computed using
        GJK algorithm on the original objects (without margin). If the
        original objects (without margin) intersect, we run again the GJK
        algorithm on the enlarged objects (with margin) to compute simplex
        polytope that contains the origin and give it to the EPA (Expanding
        Polytope Algorithm) to compute the correct penetration depth between the
        enlarged objects. 
    -------------------------------------------------------------------
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;

namespace Ascendant.Physics.Collision.React3D {
    class React3D_GJK {

        // Constants
        const double REL_ERROR = 1.0e-3;
        const double REL_ERROR_SQUARE = REL_ERROR * REL_ERROR;

        const double OBJECT_MARGIN = 0.04;          // Object margin for collision detection


        ExpandingPolytopeAlgorithm algoEPA = new ExpandingPolytopeAlgorithm();             // EPA Algorithm

        // Return true and compute a contact info if the two bounding volume collide.
        // This method implements the Hybrid Technique for computing the penetration depth by
        // running the GJK algorithm on original objects (without margin).
        // If the objects don't intersect, this method returns false. If they intersect
        // only in the margins, the method compute the penetration depth and contact points
        // (of enlarged objects). If the original objects (without margin) intersect, we
        // call the computePenetrationDepthForEnlargedObjects() method that run the GJK
        // algorithm on the enlarged object to obtain a simplex polytope that contains the
        // origin, they we give that simplex polytope to the EPA algorithm which will compute
        // the correct penetration depth and contact points between the enlarged objects.
        internal bool testCollision(MovableObject boundingVolume1, MovableObject boundingVolume2, out ContactInfo contactInfo) {
            Vector3d suppA;             // Support point of object A
            Vector3d suppB;             // Support point of object B
            Vector3d w;                 // Support point of Minkowski difference A-B
            Vector3d pA = Vector3d.Zero;                // Closest point of object A
            Vector3d pB = Vector3d.Zero;                // Closest point of object B
            double vDotw;
            double prevDistSquare;

            Debug.Assert(boundingVolume1 != boundingVolume2);

            // Initialize the margin (sum of margins of both objects)
            double margin = 2 * OBJECT_MARGIN;
            double marginSquare = margin * margin;
            Debug.Assert(margin > 0.0);

            // Create a simplex set
            Simplex simplex = new Simplex();

            // Get the last point V (last separating axis)
            // TODO : Implement frame coherence. For each pair of body, store
            //        the last separating axis and use it to initialize the v vector
            Vector3d v = Vector3d.One;

            contactInfo = null;
            // Initialize the upper bound for the square distance
            double distSquare = Double.MaxValue;

            do {
                // Compute the support points for original objects (without margins) A and B
                suppA = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(-v);
                suppB = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(v);

                // Compute the support point for the Minkowski difference A-B
                w = suppA - suppB;

                vDotw = Vector3d.Dot(v, w);

                // If the enlarge objects (with margins) do not intersect
                if (vDotw > 0.0 && vDotw * vDotw > distSquare * marginSquare) {
                    // No intersection, we return false
                    return false;
                }

                // If the objects intersect only in the margins
                if (simplex.isPointInSimplex(w) || distSquare - vDotw <= distSquare * REL_ERROR_SQUARE) {
                    // Compute the closet points of both objects (without the margins)
                    simplex.computeClosestPointsOfAandB(ref pA, ref pB);

                    // Project those two points on the margins to have the closest points of both
                    // object with the margins
                    double dist = Math.Sqrt(distSquare);
                    Debug.Assert(dist > 0.0);
                    pA = pA - (OBJECT_MARGIN / dist) * v;
                    pB = pB + (OBJECT_MARGIN / dist) * v;

                    // Compute the contact info
                    Vector3d normal = -v.Normalized();
                    double penetrationDepth = margin - dist;
                    contactInfo = new ContactInfo(boundingVolume1, boundingVolume2, normal, penetrationDepth, pA, pB);

                    // There is an intersection, therefore we return true
                    return true;
                }

                // Add the new support point to the simplex
                simplex.addPoint(w, suppA, suppB);

                // If the simplex is affinely dependent
                if (simplex.isAffinelyDependent()) {
                    // Compute the closet points of both objects (without the margins)
                    simplex.computeClosestPointsOfAandB(ref pA, ref pB);

                    // Project those two points on the margins to have the closest points of both
                    // object with the margins
                    double dist = Math.Sqrt(distSquare);
                    Debug.Assert(dist > 0.0);
                    pA = pA - (OBJECT_MARGIN / dist) * v;
                    pB = pB + (OBJECT_MARGIN / dist) * v;

                    // Compute the contact info
                    Vector3d normal = -v.Normalized();
                    double penetrationDepth = margin - dist;
                    contactInfo = new ContactInfo(boundingVolume1, boundingVolume2,
                                                  normal, penetrationDepth, pA, pB);

                    // There is an intersection, therefore we return true
                    return true;
                }

                // Compute the point of the simplex closest to the origin
                // If the computation of the closest point fail
                if (!simplex.computeClosestPoint(ref v)) {
                    // Compute the closet points of both objects (without the margins)
                    simplex.computeClosestPointsOfAandB(ref pA, ref pB);

                    // Project those two points on the margins to have the closest points of both
                    // object with the margins
                    double dist = Math.Sqrt(distSquare);
                    Debug.Assert(dist > 0.0);
                    pA = pA - (OBJECT_MARGIN / dist) * v;
                    pB = pB + (OBJECT_MARGIN / dist) * v;

                    // Compute the contact info
                    Vector3d normal = -v.Normalized();
                    double penetrationDepth = margin - dist;
                    contactInfo = new ContactInfo(boundingVolume1, boundingVolume2,
                                                  normal, penetrationDepth, pA, pB);

                    // There is an intersection, therefore we return true
                    return true;
                }

                // Store and update the squared distance of the closest point
                prevDistSquare = distSquare;
                distSquare = Vector3d.Dot(v, v);

                // If the distance to the closest point doesn't improve a lot
                if (prevDistSquare - distSquare <= double.Epsilon * prevDistSquare) {
                    simplex.backupClosestPointInSimplex(v);

                    // Get the new squared distance
                    distSquare = Vector3d.Dot(v, v);

                    // Compute the closet points of both objects (without the margins)
                    simplex.computeClosestPointsOfAandB(ref pA, ref pB);

                    // Project those two points on the margins to have the closest points of both
                    // object with the margins
                    double dist = Math.Sqrt(distSquare);
                    Debug.Assert(dist > 0.0);
                    pA = pA - (OBJECT_MARGIN / dist) * v;
                    pB = pB + (OBJECT_MARGIN / dist) * v;

                    // Compute the contact info
                    Vector3d normal = -v.Normalized();
                    double penetrationDepth = margin - dist;
                    contactInfo = new ContactInfo(boundingVolume1, boundingVolume2,
                                                  normal, penetrationDepth, pA, pB);

                    // There is an intersection, therefore we return true
                    return true;
                }

                double test = simplex.getMaxLengthSquareOfAPoint(); // TODO : Remove this
                test = 4.5;

            } while (!simplex.isFull() && distSquare > Double.Epsilon * simplex.getMaxLengthSquareOfAPoint());

            // The objects (without margins) intersect. Therefore, we run the GJK algorithm again but on the
            // enlarged objects to compute a simplex polytope that contains the origin. Then, we give that simplex
            // polytope to the EPA algorithm to compute the correct penetration depth and contact points between
            // the enlarged objects.
            return computePenetrationDepthForEnlargedObjects(boundingVolume1, boundingVolume2, out contactInfo, out v);
        }

        // This method runs the GJK algorithm on the two enlarged objects (with margin)
        // to compute a simplex polytope that contains the origin. The two objects are
        // assumed to intersect in the original objects (without margin). Therefore such
        // a polytope must exist. Then, we give that polytope to the EPA algorithm to
        // compute the correct penetration depth and contact points of the enlarged objects.
        bool computePenetrationDepthForEnlargedObjects(MovableObject boundingVolume1, MovableObject boundingVolume2, out ContactInfo contactInfo, out Vector3d v) {
            Simplex simplex = new Simplex();
            Vector3d suppA;
            Vector3d suppB;
            Vector3d w;
            double vDotw;
            double distSquare = double.MaxValue;
            double prevDistSquare;
            contactInfo = null;
            v = Vector3d.Zero;
            do {
                // Compute the support points for the enlarged object A and B
                suppA = boundingVolume1.current.MaxPointAlongDirectionOfConvexHull(-v);
                suppB = boundingVolume2.current.MaxPointAlongDirectionOfConvexHull(v);

                // Compute the support point for the Minkowski difference A-B
                w = suppA - suppB;

                vDotw = Vector3d.Dot(v, w);

                // If the enlarge objects do not intersect
                if (vDotw > 0.0) {
                    // No intersection, we return false
                    return false;
                }

                // Add the new support point to the simplex
                simplex.addPoint(w, suppA, suppB);

                if (simplex.isAffinelyDependent()) {
                    return false;
                }

                if (!simplex.computeClosestPoint(ref v)) {
                    return false;
                }

                // Store and update the square distance
                prevDistSquare = distSquare;
                distSquare = Vector3d.Dot(v, v);

                if (prevDistSquare - distSquare <= double.Epsilon * prevDistSquare) {
                    return false;
                }

            } while (!simplex.isFull() && distSquare > double.Epsilon * simplex.getMaxLengthSquareOfAPoint());

            // Give the simplex computed with GJK algorithm to the EPA algorithm which will compute the correct
            // penetration depth and contact points between the two enlarged objects
            return algoEPA.computePenetrationDepthAndContactPoints(simplex, boundingVolume1, boundingVolume2, out v, out contactInfo);
        }

    }
}