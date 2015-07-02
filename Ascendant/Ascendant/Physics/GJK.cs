using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics;
using OpenTK;

namespace Ascendant.Physics {
    static class GilbertJohnsonKeerthi {
        const float tolerance = 0.001f;

        //static public void DistanceTest(PhysicsObject A, PhysicsObject B, out Vector3 distance) {
        //    // exactly like the previous post, use whatever 
        //    // initial direction you want, some are more optimal
        //    Vector3 d = Vector3.UnitX;// choose a direction

        //    // obtain the first Minkowski Difference point using
        //    // the direction and the support function
        //    var s = new List<Vector3>();
        //    s.Add(support(A, B, d));

        //    // like the previous post just negate the
        //    // the prevous direction to get the next point
        //    s.Add(support(A, B, -d));

        //    // start the loop
        //    while (true) {
        //        // obtain the point on the current simplex closest
        //        // to the origin (see above example)
        //        Vector3 p = ClosestPointToOrigin(s[0], s[1]);

        //        // check if p is the zero vector
        //        if (p == Vector3.Zero) {
        //            // then the origin is on the Minkowski Difference
        //            // I consider this touching/collision
        //            distance = Vector3.Zero;
        //            return;
        //        }

        //        // p.to(origin) is the new direction
        //        // we normalize here because we need to check the
        //        // projections along this vector later
        //        d = -1 * p.Normalized();

        //        // obtain a new Minkowski Difference point along
        //        // the new direction
        //        Vector3 c = support(A, B, d);

        //        // is the point we obtained making progress
        //        // towards the goal (to get the closest points
        //        // to the origin)
        //        double dc = Vector3.Dot(c, d);
        //        // you can use a or b here it doesn't matter
        //        double da = Vector3.Dot(s.get(0), d);

        //        // tolerance is how acurate you want to be
        //        if (dc - da < tolerance) {
        //            // if we haven't made enough progress, 
        //            // given some tolerance, to the origin, 
        //            // then we can assume that we are done
        //            distance = dc;
        //            return true;
        //        }

        //        // if we are still getting closer then only keep
        //        // the points in the simplex that are closest to
        //        // the origin (we already know that c is closer
        //        // than both a and b)
        //        // the magnitude is the same as distance(origin, a)
        //        if (s.get(0).LengthSquared < s.get(1).LengthSquared) {
        //            s.set(1, c);
        //        } else {
        //            s.set(0, c);
        //        }
        //    }
        //}

        //private static Vector3 ClosestPointToOrigin(Vector3 A, Vector3 B) {

        //}

        static public bool CollisionTest(MovableObject A, MovableObject B) {
            Vector3 d = Vector3.UnitY;// choose a search direction
            var simplex = new List<Vector3>();
            // get the first Minkowski Difference point
            simplex.Add(support(A, B, d));
            // negate d for the next point
            d *= -1;
            // start looping
            for (int i = 0; i < 200; ++i) {
                // add a new point to the simplex because we haven't terminated yet
                simplex.Add(support(A, B, d));
                // make sure that the last point we added actually passed the origin
                if (Vector3.Dot(simplex[simplex.Count - 1], d) <= 0) {
                    // if the point added last was not past the origin in the direction of d
                    // then the Minkowski Sum cannot possibly contain the origin since
                    // the last point added is on the edge of the Minkowski Difference
                    return false;
                } else {
                    // otherwise we need to determine if the origin is in
                    // the current simplex
                    if (containsOrigin(simplex, ref d)) {
                        // if it does then we know there is a collision
                        return true;
                    }
                }
            }
            throw new Exception("Timeout in GJK");
        }
        
        public static Vector3 support(MovableObject A, MovableObject B, Vector3 d) {
            // d is a vector direction (doesn't have to be normalized)
            // get points on the edge of the shapes in opposite directions
            Vector3 p1 = A.current.MaxPointAlongDirectionOfConvexHull(d, d);
            Vector3 p2 = B.current.MaxPointAlongDirectionOfConvexHull(-d, d);
            // perform the Minkowski Difference
            Vector3 p3 = p1 - p2;
            // p3 is now a point in Minkowski space on the edge of the Minkowski Difference
            return p3;
        }

        private static bool containsOrigin(List<Vector3> s, ref Vector3 d) {
            // get the last point added to the simplex
            Vector3 a = s[s.Count - 1];
            // compute AO (same thing as -A)
            Vector3 ao = -a;
            if (s.Count == 3) {
                // then its the triangle case
                // get b and c
                Vector3 b = s[1];
                Vector3 c = s[2];
                // compute the edges
                Vector3 ab = b - a;
                Vector3 ac = c - a;
                // compute the normals
                Vector3 abPerp = tripleProduct(ac, ab, ab);
                Vector3 acPerp = tripleProduct(ab, ac, ac);
                // is the origin in R4
                if (Vector3.Dot(abPerp, ao) > 0) {
                    // remove point c
                    s.Remove(c);
                    // set the new direction to abPerp
                    d = abPerp;
                } else {
                    // is the origin in R3
                    if (Vector3.Dot(acPerp, ao) > 0) {
                        // remove point b
                        s.Remove(b);
                        // set the new direction to acPerp
                        d = acPerp;
                    } else {
                        // otherwise we know its in R5 so we can return true
                        return true;
                    }
                }
            } else {
                // then its the line segment case
                Vector3 b = s[1];
                // compute AB
                Vector3 ab = b - a;
                // get the perp to AB in the direction of the origin
                Vector3 abPerp = tripleProduct(ab, ao, ab);
                // set the direction to abPerp
                d = abPerp;
            }
            return false;
        }

        public static Vector3 tripleProduct(Vector3 A, Vector3 B, Vector3 C) {
            return Vector3.Subtract((B * Vector3.Dot(A, C)), (C * Vector3.Dot(A, B)));
        }
    }

    class ExpandingPolytope {
        private const float TOLERANCE = 0.0001f;
        static void FindCollision(List<Vector3> s, MovableObject A, MovableObject B, out Vector3 normal, out float depth) {
            // loop to find the collision information
            while (true) {
                // obtain the feature (edge for 2D, plane for 3D) closest to the 
                // origin on the Minkowski Difference
                Plane e = findClosestPlane(s);
                // obtain a new support point in the direction of the edge normal
                Vector3 p = GilbertJohnsonKeerthi.support(A, B, e.normal);
                // check the distance from the origin to the edge against the distance p is along e.normal
                float d = Vector3.Dot(p, e.normal);
                if (d - e.distance < TOLERANCE) {
                    // the tolerance should be something positive close to zero (ex. 0.00001)

                    // if the difference is less than the tolerance then we can
                    // assume that we cannot expand the simplex any further and
                    // we have our solution
                    normal = e.normal;
                    depth = d;
                } else {
                    // we haven't reached the edge of the Minkowski Difference
                    // so continue expanding by adding the new point to the simplex
                    // in between the points that made the closest edge
                    s.Insert(e.index, p);
                }
            }
        }

        private static Plane findClosestPlane(List<Vector3> s) {
            Plane closest = new Plane();
            // prime the distance of the edge to the max
            closest.distance = float.MaxValue;
            // s is the passed in simplex
            for (int i = 0; i < s.Count; i++) {
                // compute the next points index
                int j = i + 1 == s.Count ? 0 : i + 1;
                // get the current point and the next one
                Vector3 a = s[i];
                Vector3 b = s[j];
                // create the edge vector
                Vector3 e = Vector3.Subtract(b, a); // or a.to(b);
                // get the vector from the origin to a
                Vector3 oa = a; // or a - ORIGIN
                // get the vector from the edge towards the origin
                Vector3 n = GilbertJohnsonKeerthi.tripleProduct(e, oa, e);
                // normalize the vector
                n.Normalize();
                // calculate the distance from the origin to the edge
                float d = Vector3.Dot(n, a); // could use b or a here
                // check the distance against the other distances
                if (d < closest.distance) {
                    // if this edge is closer then use it
                    closest.distance = d;
                    closest.normal = n;
                    closest.index = j;
                }
            }
            // return the closest edge we found
            return closest;
        }

        private const bool CLOCKWISE = true;
    }

    class Plane {
        //Vector3 point1, point2, point3;
        internal Vector3 normal;
        internal float distance;
        internal int index;
    }
}
