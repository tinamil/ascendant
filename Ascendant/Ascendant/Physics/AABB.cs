using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Ascendant.Physics {
    class AABB {

        internal Vector3 min;
        internal Vector3 max;

        internal List<AABB> primitives;

        public override bool Equals(object otherObject) {
            var other = otherObject as AABB;
            if (other != null) {
                return min.Equals(other.min) && max.Equals(other.max);
            } else {
                return false;
            }
        }
        public override int GetHashCode() {
            return min.GetHashCode() + max.GetHashCode();
        }

        public AABB(List<AABB> primitives) {
            this.primitives = primitives;
            if (primitives.Count < 1) {
                max = Vector3.Zero;
                min = Vector3.Zero;
            } else {
                max = primitives[0].max;
                min = primitives[0].min;
                for (int i = 1; i < primitives.Count; ++i) {
                    Vector3.ComponentMax(ref max, ref primitives[i].max, out max);
                    Vector3.ComponentMin(ref min, ref primitives[i].min, out min);
                }
            }
        }

        private AABB(AABB copy) {
            this.max = copy.max;
            this.min = copy.min;
            this.primitives = copy.primitives;
        }

        // Transform AABB a by the orientation quaternion m and translation t, find maximum extents, and store result into AABB b.
        public static void Update(AABB a, Quaternion m, Vector3 t, Vector3 scale, out AABB b) {
            Update(a, Matrix3.CreateFromQuaternion(m), t, scale, out b);
        }

        static void Update(AABB a, Matrix3 rotation, Vector3 translation, Vector3 scale, out AABB b) {
            b = new AABB(a);
            // Start by adding in translation
            b.min = b.max = translation;
            b.min *= scale;
            b.max *= scale;
            // For all three axes
            for (int i = 0; i < 3; i++) {
                // Form extent by summing smaller and larger terms respectively
                for (int j = 0; j < 3; j++) {
                    float e = rotation[i, j] * a.min[j];
                    float f = rotation[i, j] * a.max[j];
                    if (e < f) {
                        b.min[i] += e;
                        b.max[i] += f;
                    } else {
                        b.min[i] += f;
                        b.max[i] += e;
                    }
                }
            }
        }

        public bool collisionTest(AABB other) {
            // Exit with no intersection if separated along an axis
            if (this.max[0] < other.min[0] || this.min[0] > other.max[0]) return false;
            if (this.max[2] < other.min[2] || this.min[2] > other.max[2]) return false;
            if (this.max[1] < other.min[1] || this.min[1] > other.max[1]) return false;
            // Overlapping on all axes means AABBs are intersecting
            return true;
        }

        internal static AABB FromVertices(Vector3[] vertices) {
            AABB retVal = new AABB(new List<AABB>());
            if (vertices.Length < 1) {
                retVal.max = Vector3.Zero;
                retVal.min = Vector3.Zero;
            } else {
                retVal.max = vertices[0];
                retVal.min = vertices[0];
                for (int i = 1; i < vertices.Length; ++i) {
                    Vector3.ComponentMax(ref retVal.max, ref vertices[i], out retVal.max);
                    Vector3.ComponentMin(ref retVal.min, ref vertices[i], out retVal.min);
                }
            }
            return retVal;
        }
    }
}
