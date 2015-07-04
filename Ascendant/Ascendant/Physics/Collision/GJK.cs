
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics;
using OpenTK;
using Ascendant.Physics.Collision;
using System.Diagnostics;

namespace Ascendant.Physics {
    class Simplex {
        readonly List<Vector3d> list = new List<Vector3d>();

        public Vector3d A { get { return list[list.Count - 1]; } }
        public Vector3d B { get { return list[list.Count - 2]; } }
        public Vector3d C { get { return list[list.Count - 3]; } }
        public Vector3d D { get { return list[list.Count - 4]; } }

        public int Size { get { return list.Count; } }

        internal void Delete(Vector3d vector3d) {
            list.Remove(vector3d);
        }

        internal void Add(Vector3d vector3d) {
            list.Add(vector3d);
        }

        internal void Clear() {
            list.Clear();
        }
    }
    static class GilbertJohnsonKeerthi {

        const float tolerance = 0.001f;
        const double REL_ERROR_SQUARE = tolerance * tolerance;

        static public bool CollisionTest(MovableObject A, MovableObject B, out ContactInfo result) {
            Simplex s = new Simplex();
            //Check for a collision
            if (CollisionTest(A, B, ref s, 1.0)) {
                //Reduce the size of each object by a scaling factor and try again
                if (!CollisionTest(A, B, ref s, scalingFactor)) {
                    //No collision means we can test with the collision depth algorithm
                    //return CollisionDepth(A, B, ref s, out result);
                } else {
                    //Deep collision means we need a different algorithm
                    //ExpandingPolytope.FindCollision();
                }
            }
            result = null;
            return false;
        }

        static private bool CollisionTest(MovableObject A, MovableObject B, ref Simplex simplex, double scalingFactor) {
            Vector3d d = Vector3d.UnitY;// choose a search direction
            simplex.Clear();
            // get the first Minkowski Difference point
            simplex.Add(support(A, B, d));
            // negate d for the next point
            d = -simplex.A;
            // start looping, cap it at 200 to prevent infinite loops
            for (int i = 0; i < 200; ++i) {
                // add a new point to the simplex because we haven't terminated yet
                simplex.Add(support(A, B, d));
                // make sure that the last point we added actually passed the origin
                if (Vector3d.Dot(simplex.A, d) < 0) {
                    // if the point added last was not past the origin in the direction of d
                    // then the Minkowski Sum cannot possibly contain the origin since
                    // the last point added is on the edge of the Minkowski Difference
                    return false;
                } else {
                    // otherwise we need to determine if the origin is in
                    // the current simplex
                    if (containsOrigin(ref simplex, out d)) {
                        // if it does then we know there is a collision
                        return true;
                    }
                }
            }
            return false;
        }

        //private static bool CollisionDepth(MovableObject A, MovableObject B, ref Simplex simplex, out ContactInfo result) {
        //    Debug.Assert(simplex.Size == 4);
        //    Vector3d p, v;
        //    while(true) {
        //        //Compute point P of minimum norm in CH(Q)
        //        p = ClosestPointToOrigin(simplex);
                
        //        //If P is the origin, exit; 
        //        if(p == Vector3d.Zero){
        //            return ExpandingPolytope.FindCollision(A, B, simplex, out result);
        //        }
            
        //        //Reduce Q to the smallest subset Q’ of Q, such that P in CH(Q’)
                

        //        //Let V=SC(–P) be a supporting point in direction –P
        //        v = support(A, B, -p, scalingFactor);
            
        //        //If V no more extreme in direction –P than P itself, exit; return ||P||
        //        double vc = Vector3d.Dot(simplex.C, v);
        //        double va = Vector3d.Dot(simplex.A, v);
        //        // tolerance is how acurate you want to be
        //        if (vc - va < tolerance) {
        //            // if we haven't made enough progress, 
        //            // given some tolerance, to the origin, 
        //            // then we can assume that we are done
        //            result = new ContactInfo(A, B, ,vc);
        //            return true;
        //        }

        //        //Add V to simplex Q. Go to step 2
        //        simplex.Add(v);
        //    }
        //}

        const double scalingFactor = 0.95;

        private static Vector3d support(MovableObject A, MovableObject B, Vector3d d, double scalingFactor = 1.0) {
            // d is a vector direction (doesn't have to be normalized)
            // get points on the edge of the shapes in opposite directions
            Vector3d p1 = A.current.MaxPointAlongDirectionOfConvexHull(d, scalingFactor);
            Vector3d p2 = B.current.MaxPointAlongDirectionOfConvexHull(-d, scalingFactor);
            // perform the Minkowski Difference
            Vector3d p3 = p1 - p2;
            // p3 is now a point in Minkowski space on the edge of the Minkowski Difference
            return p3;
        }

        private static bool containsOrigin(ref Simplex s, out Vector3d d) {
            d = Vector3d.Zero;
            if (s.Size == 1) {
                //Degenerate, something wrong
                throw new Exception("Single point in simplex during GJK");
            }
            if (s.Size == 2) {
                Vector3d a = s.A;
                Vector3d ao = -a;

                Vector3d b = s.B;
                Vector3d ab = b - a;
                if (Vector3d.Dot(ab, ao) > 0) {
                    // get the perp to AB in the direction of the origin
                    Vector3d abPerp = tripleProduct(ab, ao, ab);
                    // set the direction to abPerp
                    d = abPerp;
                } else {
                    s.Delete(b);
                    d = ao;
                }
            } else if (s.Size == 3) {
                // then its the triangle case
                computeTriangleCase(ref s, out d);
            } else if (s.Size == 4) {
                //3 ifs to determine which face closest to
                //Then handle as if triangle case
                return computeTetrahedronCase(ref s, out d);
            }
            return false;
        }

        private static bool computeTetrahedronCase(ref Simplex s, out Vector3d dir) {
            Debug.Assert(s.Size == 4);
            Vector3d a = s.A;
            Vector3d b = s.B;
            Vector3d c = s.C;
            Vector3d d = s.D;
            Vector3d ao = -a;

            Vector3d ab = b - a;
            Vector3d ac = c - a;

            Vector3d abc = Vector3d.Cross(ab, ac);

            if (Vector3d.Dot(abc, ao) > 0) {
                s.Delete(d);
                computeTriangleCase(ref s, out dir);
                return false;
            }

            Vector3d ad = d - a;
            Vector3d acd = Vector3d.Cross(ac, ad);
            if (Vector3d.Dot(acd, ao) > 0) {
                //in front of triangle ACD
                s.Delete(b);
                computeTriangleCase(ref s, out dir);
                return false;
            }

            Vector3d adb = Vector3d.Cross(ad, ab);
            if (Vector3d.Dot(adb, ao) > 0) {
                //in front of triangle ADB
                s.Delete(a);
                s.Delete(c);
                s.Delete(d);
                s.Add(d);
                s.Add(a);

                computeTriangleCase(ref s, out dir);
            }

            //behind all three faces, the origin is in the tetrahedron, we're done
            dir = Vector3d.Zero;
            return true;
        }

        private static void computeTriangleCase(ref Simplex s, out Vector3d d) {
            Debug.Assert(s.Size == 3);
            Vector3d a = s.A;
            Vector3d ao = -a;
            Vector3d b = s.B;
            Vector3d c = s.C;

            // compute the edges
            Vector3d ab = b - a;
            Vector3d ac = c - a;
            Vector3d abc = Vector3d.Cross(ab, ac);

            //Check if origin is in region 1, 4, or 5(abc x ac)
            if (Vector3d.Dot(Vector3d.Cross(abc, ac), ao) > 0) {
                //Check if origin is in region 1
                if (Vector3d.Dot(ac, ao) > 0) {
                    s.Delete(b);
                    d = tripleProduct(ac, ao, ac);
                } else {//Then origin is in region 4 or 5
                    checkTriangleRegion45(ref s, out d);
                }
            } else {
                if (Vector3d.Dot(Vector3d.Cross(ab, abc), ao) > 0) {
                    checkTriangleRegion45(ref s, out d);
                } else {
                    if (Vector3d.Dot(abc, ao) > 0) {
                        d = abc;
                    } else {
                        //Change ABC to ACB
                        s.Delete(a);
                        s.Delete(c);
                        s.Add(c);
                        s.Add(a);
                        d = -abc;
                    }
                }
            }
        }

        private static void checkTriangleRegion45(ref Simplex s, out Vector3d d) {
            Vector3d ab = s.B - s.A;
            Vector3d ao = -s.A;
            Vector3d c = s.C;
            Vector3d b = s.B;
            //Check region 4
            if (Vector3d.Dot(ab, ao) > 0) {
                s.Delete(c);
                d = tripleProduct(ab, ao, ab);
            } else {//Check region 5
                s.Delete(b);
                s.Delete(c);
                d = ao;
            }
        }

        public static Vector3d tripleProduct(Vector3d A, Vector3d B, Vector3d C) {
            return Vector3d.Subtract((B * Vector3d.Dot(A, C)), (C * Vector3d.Dot(A, B)));
        }
    }

    class Plane {
        //Vector3 point1, point2, point3;
        internal Vector3d normal;
        internal double distance;
        internal int index;
    }
}
