using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Ascendant.Graphics;
using Ascendant.Physics;

namespace Ascendant.Physics {
    class SimpleAABB {
        internal Vector3 min;
        internal Vector3 max;

        public override bool Equals(object otherObject) {
            var other = otherObject as SimpleAABB;
            if (other != null) {
                return min.Equals(other.min) && max.Equals(other.max);
            } else {
                return false;
            }
        }

        public override int GetHashCode() {
            return min.GetHashCode() + max.GetHashCode();
        }

        public SimpleAABB(Vector3[] vertices) {
            max = min = vertices[0];

            for (int i = 1; i < vertices.Length; ++i) {
                Vector3.ComponentMax(ref max, ref vertices[i], out max);
                Vector3.ComponentMin(ref min, ref vertices[i], out min);
            }
        }

        public SimpleAABB(SimpleAABB copy) {
            this.max = copy.max;
            this.min = copy.min;
        }

        // Transform AABB a by the orientation quaternion m and translation t, find maximum extents, and store result into AABB b.
        public static void Update(SimpleAABB a, Matrix4 transform, out SimpleAABB b) {
            Vector3[] corners = new Vector3[8];
            corners[0] = a.min;
            corners[1] = new Vector3(a.min.X, a.min.Y, a.max.Z);
            corners[2] = new Vector3(a.min.X, a.max.Y, a.min.Z);
            corners[3] = new Vector3(a.min.X, a.max.Y, a.max.Z);
            corners[4] = new Vector3(a.max.X, a.min.Y, a.min.Z);
            corners[5] = new Vector3(a.max.X, a.min.Y, a.max.Z);
            corners[6] = new Vector3(a.max.X, a.max.Y, a.min.Z);
            corners[7] = a.max;
            for (int i = 0; i < corners.Length; ++i) {
                corners[i] = Vector3.Transform(corners[i], transform);
            }
            b = new SimpleAABB(corners);
        }

        public bool collisionTest(SimpleAABB other) {
            // Exit with no intersection if separated along an axis
            if (this.max[0] < other.min[0] || this.min[0] > other.max[0]) return false;
            if (this.max[2] < other.min[2] || this.min[2] > other.max[2]) return false;
            if (this.max[1] < other.min[1] || this.min[1] > other.max[1]) return false;
            // Overlapping on all axes means AABBs are intersecting
            return true;
        }
    }
}
