using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Collections;
using System.Diagnostics;

namespace Ascendant.Physics {
    class Collision {

    }

    class TreeNode<DATA> {

        public TreeNode<DATA> leftChild;
        public TreeNode<DATA> rightChild;

        public readonly DATA data;

        public TreeNode(DATA data) {
            this.data = data;
        }

        public bool isLeaf {
            get { return leftChild == null && rightChild == null; }
        }
    }

    class BoundingVolumeTree {
        const int MIN_OBJECTS_PER_LEAF = 1;
        TreeNode<AxisAlignedBoundingBox> root;
        readonly Dictionary<AxisAlignedBoundingBox, List<AxisAlignedBoundingBox>> collisions = new Dictionary<AxisAlignedBoundingBox, List<AxisAlignedBoundingBox>>();
        // Construct a top-down tree. Rearranges object[] array during construction
        public BoundingVolumeTree(AxisAlignedBoundingBox[] objects) {
            AxisAlignedBoundingBox boundingVolume = AxisAlignedBoundingBox.ComputeVolume(objects);
            root = new TreeNode<AxisAlignedBoundingBox>(boundingVolume);
            construction(ref root, objects);
            var collisionList = new Dictionary<AxisAlignedBoundingBox, AxisAlignedBoundingBox>();
            BVHCollision(ref collisions, root, root);
        }

        public AxisAlignedBoundingBox[] getCollisions(AxisAlignedBoundingBox box) {
            List<AxisAlignedBoundingBox> list;
            if (collisions.TryGetValue(box, out list)) {
                return list.ToArray();
            } else {
                return new AxisAlignedBoundingBox[0];
            }
        }

        private void construction(ref TreeNode<AxisAlignedBoundingBox> root, AxisAlignedBoundingBox[] objects) {
            if (objects.Length <= MIN_OBJECTS_PER_LEAF) {
                return;
            } else {
                // Based on some partitioning strategy, arrange objects into
                // two partitions: object[0..k-1], and object[k..numObjects-1]
                AxisAlignedBoundingBox[] partition1 = new AxisAlignedBoundingBox[0], partition2 = new AxisAlignedBoundingBox[0];
                while (partition1.Length == 0 || partition2.Length == 0)
                    PartitionObjects(root.data, objects, out partition1, out partition2);
                // Recursively construct left and right subtree from subarrays and
                // point the left and right fields of the current node at the subtrees
                TreeNode<AxisAlignedBoundingBox> leftChild = new TreeNode<AxisAlignedBoundingBox>(AxisAlignedBoundingBox.ComputeVolume(partition1));
                TreeNode<AxisAlignedBoundingBox> rightChild = new TreeNode<AxisAlignedBoundingBox>(AxisAlignedBoundingBox.ComputeVolume(partition2));
                root.leftChild = leftChild;
                root.rightChild = rightChild;
                construction(ref leftChild, partition1);
                construction(ref rightChild, partition2);
            }
        }

        Random r = new Random();
        private void PartitionObjects(AxisAlignedBoundingBox totalBox, AxisAlignedBoundingBox[] objects, out AxisAlignedBoundingBox[] partition1, out AxisAlignedBoundingBox[] partition2) {
            var more = new List<AxisAlignedBoundingBox>();
            var less = new List<AxisAlignedBoundingBox>();
            if (objects.Length == 0) {
                partition1 = new AxisAlignedBoundingBox[0];
                partition2 = new AxisAlignedBoundingBox[0];
                return;
            }
            //Find the axis with the largest variance
            Vector3 range = totalBox.max - totalBox.min;
            int axis;
            if (range.X >= range.Y && range.X >= range.Z) {
                axis = 0;
            } else if (range.Y >= range.Z) {
                axis = 1;
            } else {
                axis = 2;
            }
            //Find the mean of that axis
            float meanMaxAxis = 0;
            float meanMinAxis = 0;
            foreach (AxisAlignedBoundingBox box in objects) {
                meanMaxAxis += box.max[axis];
                meanMinAxis += box.min[axis];
            }
            meanMaxAxis /= objects.Length;
            meanMinAxis /= objects.Length;
            float meanAxis = (meanMaxAxis + meanMinAxis) / 2f;
            //Divide each object along the mean of that axis
            foreach (AxisAlignedBoundingBox box in objects) {
                float mean = (box.max[axis] + box.min[axis]) / 2f;
                if (mean < meanAxis)
                    less.Add(box);
                else if (mean > meanAxis)
                    more.Add(box);
                else {
                    if (r.NextDouble() < 0.5) {
                        less.Add(box);
                    } else {
                        more.Add(box);
                    }
                }
            }
            partition1 = less.ToArray();
            partition2 = more.ToArray();
        }

        static internal void BVHCollision(ref Dictionary<AxisAlignedBoundingBox, List<AxisAlignedBoundingBox>> collisions, TreeNode<AxisAlignedBoundingBox> a, TreeNode<AxisAlignedBoundingBox> b) {
            if (!a.data.collisionTest(b.data)) return;
            if (a.isLeaf && b.isLeaf) {
                if (a != b) {
                    // At leaf nodes. Perform collision tests on leaf node contents
                    List<AxisAlignedBoundingBox> list;
                    if (!collisions.TryGetValue(a.data, out list)) {
                        list = new List<AxisAlignedBoundingBox>();
                        collisions.Add(a.data, list);
                    }
                    list.Add(b.data);
                }
            } else {
                if (DescendA(a, b)) {
                    BVHCollision(ref collisions, a.leftChild, b);
                    BVHCollision(ref collisions, a.rightChild, b);
                } else {
                    BVHCollision(ref collisions, a, b.leftChild);
                    BVHCollision(ref collisions, a, b.rightChild);
                }
            }
        }

        // ‘Descend larger’ descent rule
        static bool DescendA(TreeNode<AxisAlignedBoundingBox> a, TreeNode<AxisAlignedBoundingBox> b) {
            Vector3 aRange = a.data.max - a.data.min;
            Vector3 bRange = b.data.max - b.data.min;
            return b.isLeaf || (!a.isLeaf && (aRange.LengthSquared >= bRange.LengthSquared));
        }
    }

    public class AxisAlignedBoundingBox {
        internal Vector3 min;
        internal Vector3 max;

        public override bool Equals(object otherObject) {
            var other = otherObject as AxisAlignedBoundingBox;
            if (other != null) {
                return min.Equals(other.min) && max.Equals(other.max);
            } else {
                return false;
            }
        }
        public override int GetHashCode() {
            return min.GetHashCode() + max.GetHashCode();
        }

        public AxisAlignedBoundingBox(Vector3[] vertices) {
            for (int i = 0; i < vertices.Length; ++i) {
                Vector3.ComponentMax(ref max, ref vertices[i], out max);
                Vector3.ComponentMin(ref min, ref vertices[i], out min);
            }
        }

        private AxisAlignedBoundingBox() { }

        // Transform AABB a by the orientation quaternion m and translation t, find maximum extents, and store result into AABB b.
        public static void Update(AxisAlignedBoundingBox a, Quaternion m, Vector3 t, Vector3 scale, out AxisAlignedBoundingBox b) {
            Update(a, Matrix3.CreateFromQuaternion(m), t, scale, out b);
        }

        static void Update(AxisAlignedBoundingBox a, Matrix3 rotation, Vector3 translation, Vector3 scale, out AxisAlignedBoundingBox b) {
            b = new AxisAlignedBoundingBox();
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
        public bool collisionTest(AxisAlignedBoundingBox other) {
            // Exit with no intersection if separated along an axis
            if (this.max[0] < other.min[0] || this.min[0] > other.max[0]) return false;
            if (this.max[2] < other.min[2] || this.min[2] > other.max[2]) return false;
            if (this.max[1] < other.min[1] || this.min[1] > other.max[1]) return false;
            // Overlapping on all axes means AABBs are intersecting
            return true;
        }

        public static AxisAlignedBoundingBox ComputeVolume(AxisAlignedBoundingBox[] objects) {
            AxisAlignedBoundingBox retVal = new AxisAlignedBoundingBox();
            if (objects.Length < 1) {
                retVal.max = Vector3.Zero;
                retVal.min = Vector3.Zero;
                return retVal;
            }
            retVal.max = objects[0].max;
            retVal.min = objects[0].min;
            for (int i = 1; i < objects.Length; ++i) {
                Vector3.ComponentMax(ref retVal.max, ref objects[i].max, out retVal.max);
                Vector3.ComponentMin(ref retVal.min, ref objects[i].min, out retVal.min);
            }
            return retVal;
        }
    }
}
