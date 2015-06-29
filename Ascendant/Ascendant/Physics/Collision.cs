using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Collections;
using System.Diagnostics;
using Ascendant.Graphics;
using System.Threading;

namespace Ascendant.Physics {

    class BoundingVolumeTree {
        const int MIN_OBJECTS_PER_LEAF = 1;

        TreeNode<AABB> root;

        readonly Dictionary<AABB, List<AABB>> collisions = new Dictionary<AABB, List<AABB>>();

        // Construct a top-down tree
        public BoundingVolumeTree(List<AABB> boxes) {
            AABB boundingVolume = new AABB(boxes);
            root = new TreeNode<AABB>(boundingVolume);
            construction(ref root, boxes);
            if (root.leftChild != null && root.rightChild != null)
                BVHCollision(ref collisions, root.leftChild, root.rightChild);
        }

        public AABB[] getCollisions(AABB box) {
            List<AABB> list;
            if (collisions.TryGetValue(box, out list)) {
                return list.ToArray();
            } else {
                return new AABB[0];
            }
        }

        private void construction(ref TreeNode<AABB> root, List<AABB> objects) {
            if (objects.Count <= MIN_OBJECTS_PER_LEAF) {
                return;
            } else {
                // Based on some partitioning strategy, arrange objects into
                // two partitions: object[0..k-1], and object[k..numObjects-1]
                List<AABB> partition1, partition2;
                PartitionObjects(root.data, objects, out partition1, out partition2);
                if (partition1.Count == 0 || partition2.Count == 0) return;
                // Recursively construct left and right subtree from subarrays and
                // point the left and right fields of the current node at the subtrees
                TreeNode<AABB> leftChild = new TreeNode<AABB>(new AABB(partition1));
                TreeNode<AABB> rightChild = new TreeNode<AABB>(new AABB(partition2));
                root.leftChild = leftChild;
                root.rightChild = rightChild;

                construction(ref leftChild, partition1);
                construction(ref rightChild, partition2);
            }
        }

        private void PartitionObjects(AABB totalBox, List<AABB> objects, out List<AABB> partition1, out List<AABB> partition2) {
            partition1 = new List<AABB>();
            partition2 = new List<AABB>();
            if (objects.Count == 0) {
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
            float meanMax = 0, meanMin = 0;
            foreach (AABB vertex in objects) {
                meanMax += vertex.max[axis];
                meanMin += vertex.min[axis];
            }
            float mean = (meanMax + meanMin) / 2f;
            mean /= objects.Count;

            //Divide each object along the mean of that axis
            foreach (AABB vertex in objects) {
                float vertexMean = (vertex.max[axis] + vertex.min[axis]) / 2f;
                if (vertexMean < mean) {
                    partition1.Add(vertex);
                } else {
                    partition2.Add(vertex);
                }
            }
        }

        static internal void SAT() {
            double overlap = double.MaxValue; // really large value;
            Axis smallest = null;
            Axis[] axes1 = shape1.getAxes();
            Axis[] axes2 = shape2.getAxes();
            // loop over the axes1
            for (int i = 0; i < axes1.length; i++) {
                Axis axis = axes1[i];
                // project both shapes onto the axis
                Projection p1 = shape1.project(axis);
                Projection p2 = shape2.project(axis);
                // do the projections overlap?
                if (!p1.overlap(p2)) {
                    // then we can guarantee that the shapes do not overlap
                    return false;
                } else {
                    // get the overlap
                    double o = p1.getOverlap(p2);
                    // check for minimum
                    if (o < overlap) {
                        // then set this one as the smallest
                        overlap = o;
                        smallest = axis;
                    }
                }
            }
            // loop over the axes2
            for (int i = 0; i < axes2.length; i++) {
                Axis axis = axes2[i];
                // project both shapes onto the axis
                Projection p1 = shape1.project(axis);
                Projection p2 = shape2.project(axis);
                // do the projections overlap?
                if (!p1.overlap(p2)) {
                    // then we can guarantee that the shapes do not overlap
                    return false;
                } else {
                    // get the overlap
                    double o = p1.getOverlap(p2);
                    // check for minimum
                    if (o < overlap) {
                        // then set this one as the smallest
                        overlap = o;
                        smallest = axis;
                    }
                }
            }
            MTV mtv = new MTV(smallest, overlap);
            // if we get here then we know that every axis had overlap on it
            // so we can guarantee an intersection
            return mtv;
        }

        static internal void BVHCollision(ref Dictionary<AABB, List<AABB>> collisions, TreeNode<AABB> a, TreeNode<AABB> b) {
            if (!a.data.collisionTest(b.data)) return;
            if (a.isLeaf && b.isLeaf) {
                if (a != b) {
                    // At leaf nodes. Perform collision tests on leaf node contents
                    List<AABB> list;
                    if (!collisions.TryGetValue(a.data, out list)) {
                        list = new List<AABB>();
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
        static bool DescendA(TreeNode<AABB> a, TreeNode<AABB> b) {
            Vector3 aRange = a.data.max - a.data.min;
            Vector3 bRange = b.data.max - b.data.min;
            return b.isLeaf || (!a.isLeaf && (aRange.LengthSquared >= bRange.LengthSquared));
        }
    }
}
