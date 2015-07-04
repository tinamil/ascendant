using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Collections;
using System.Diagnostics;
using Ascendant.Graphics;
using System.Threading;
using Ascendant.Graphics.objects;

namespace Ascendant.Physics {
   public class Primitive {
        internal readonly int size = 3;
        internal int start;
        internal Mesh mesh;

        internal float getSum(int axis) {
            float mean = 0;
            for (int i = start; i < size; ++i) {
                mean += mesh.vertices[i][axis];
            }
            return mean;
        }

        internal Primitive(Mesh _mesh, int _start) {
            this.start = _start;
            this.mesh = _mesh;
        }
    }

    struct ArrayIndex {
        public int start, size;

        public ArrayIndex(int start, int size) {
            this.start = start;
            this.size = size;
        }
    }

    public class BoundingVolumeTree {
        const int MIN_OBJECTS_PER_LEAF = 1;

        //internal TreeNode<SimpleAABB<Primitive>> root;

        // Construct a top-down tree
        public BoundingVolumeTree(Primitive[] primitives) {
            //ComplexAABB boundingVolume = new ComplexAABB(primitives, 0, primitives.Length);
            //root = new TreeNode<ComplexAABB>(boundingVolume);
            //construction(ref root, primitives, 0, primitives.Length);
        }

        //public BoundingVolumeTree(List<BoundingVolumeTree> children) {
        //    var aabbs = new List<ComplexAABB>();
        //    foreach(BoundingVolumeTree t in children){
        //        aabbs.Add(t.root.data);
        //    }
        //    ComplexAABB globalBox = new ComplexAABB(aabbs);
        //    this.root = new TreeNode<ComplexAABB>(globalBox);

        //    foreach (BoundingVolumeTree t in children) {
        //        root.children.Add(t.root);
        //    }
        //}

        //public ComplexAABB[] getCollisions(ComplexAABB box) {
        //    List<ComplexAABB> list;
        //    if (collisions.TryGetValue(box, out list)) {
        //        return list.ToArray();
        //    } else {
        //        return new ComplexAABB[0];
        //    }
        //}

        //private void construction(ref TreeNode<ComplexAABB> root, Primitive[] primitives, int start, int size) {
        //    if (size <= MIN_OBJECTS_PER_LEAF) {
        //        return;
        //    } else {
        //        // Based on some partitioning strategy, arrange objects into
        //        // two partitions: object[0..k-1], and object[k..numObjects-1]
        //        ArrayIndex[] limits;
        //        PartitionObjects(root.data, ref primitives, start, size, out limits);
        //        foreach (ArrayIndex limit in limits) {
        //            if (limit.size == 0) return;

        //            // Recursively construct left and right subtree from subarrays and
        //            // point the left and right fields of the current node at the subtrees

        //            var child = new TreeNode<ComplexAABB>(new ComplexAABB(primitives, limit.start, limit.size));

        //            root.children.Add(child);

        //            construction(ref child, primitives, limit.start, limit.size);
        //        }
        //    }
        //}

        //private void PartitionObjects(ComplexAABB totalBox, ref Primitive[] primitives, int start, int size, out ArrayIndex[] limits) {
        //    limits = new ArrayIndex[2];
        //    if (size == 0) {
        //        for (int i = 0; i < limits.Length; ++i) {
        //            limits[i] = new ArrayIndex(start, 0);
        //        }
        //        return;
        //    }
        //    //Find the axis with the largest variance
        //    Vector3 range = totalBox.max - totalBox.min;
        //    int axis;
        //    if (range.X >= range.Y && range.X >= range.Z) {
        //        axis = 0;
        //    } else if (range.Y >= range.Z) {
        //        axis = 1;
        //    } else {
        //        axis = 2;
        //    }
        //    //Find the mean of that axis
        //    float mean = 0f;
        //    for (int i = start; i < size; ++i) {
        //        for (int j = primitives[i].start; j < primitives[i].size; ++j) {
        //            mean += primitives[i].mesh.vertices[j][axis];
        //        }
        //    }
        //    mean /= size; //Only works if limits = [2] (binary tree)
        //    var keys = new List<int>();
        //    //Divide each object along the mean of that axis
        //    for (int i = start; i < size; ++i) {
        //        float vertexMean = 0f;
        //        for (int j = primitives[i].start; j < primitives[i].size; ++j) {
        //            vertexMean += primitives[i].mesh.vertices[j][axis];
        //        }
        //        vertexMean /= size;
        //        if (vertexMean < mean) {
        //            keys.Add(i);
        //        } else {
        //            keys.Add(i + size);
        //        }
        //    }
        //    Array.Sort(keys.ToArray(), primitives, start, size);
        //}

        //static internal void BVHCollision(ref Dictionary<ComplexAABB, List<ComplexAABB>> collisions, TreeNode<ComplexAABB> a, TreeNode<ComplexAABB> b) {
        //    if (!a.data.collisionTest(b.data)) return;
        //    if (a.isLeaf && b.isLeaf) {
        //        if (a != b) {
        //            // At leaf nodes. Perform collision tests on leaf node contents
        //            List<ComplexAABB> list;
        //            if (!collisions.TryGetValue(a.data, out list)) {
        //                list = new List<ComplexAABB>();
        //                collisions.Add(a.data, list);
        //            }
        //            list.Add(b.data);
        //        }
        //    } else {
        //        if (DescendA(a, b)) {
        //            foreach (TreeNode<ComplexAABB> child in a.children) {
        //                BVHCollision(ref collisions, child, b);
        //            }
        //        } else {
        //            foreach (TreeNode<ComplexAABB> child in b.children) {
        //                BVHCollision(ref collisions, a, child);
        //            }
        //        }
        //    }
        //}

        //// ‘Descend larger’ descent rule
        //static bool DescendA(TreeNode<ComplexAABB> a, TreeNode<ComplexAABB> b) {
        //    Vector3 aRange = a.data.max - a.data.min;
        //    Vector3 bRange = b.data.max - b.data.min;
        //    return b.isLeaf || (!a.isLeaf && (aRange.LengthSquared >= bRange.LengthSquared));
        //}
    }
}
