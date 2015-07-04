using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Ascendant.Physics {
    class SortSweep {
        readonly List<MovableObject> gAABBArray;
        int gSortAxis = 0;
        internal SortSweep(List<MovableObject> objects) {
            this.gAABBArray = objects;
        }

        internal Dictionary<MovableObject, List<MovableObject>> SortAndSweepAABBArray() {
            var collisions = new Dictionary<MovableObject, List<MovableObject>>();
            // Sort the array on currently selected sorting axis (gSortAxis)
            gAABBArray.Sort((box1, box2) => box1.current.boundingBox.min[gSortAxis].CompareTo(box2.current.boundingBox.min[gSortAxis]));
            // Sweep the array for collisions
            Vector3 s = Vector3.Zero, s2 = Vector3.Zero, v = new Vector3();

            for (int i = 0; i < gAABBArray.Count; i++) {
                // Determine AABB center point
                Vector3 p = 0.5f * (gAABBArray[i].current.boundingBox.min + gAABBArray[i].current.boundingBox.max);

                // Update sum and sum2 for computing variance of AABB centers
                for (int c = 0; c < 3; c++) {
                    s[c] += p[c];
                    s2[c] += p[c] * p[c];
                }

                // Test collisions against all possible overlapping AABBs following current one
                for (int j = i + 1; j < gAABBArray.Count; j++) {
                    // Stop when tested AABBs are beyond the end of current AABB
                    if (gAABBArray[j].current.boundingBox.min[gSortAxis] > gAABBArray[i].current.boundingBox.max[gSortAxis])
                        break;
                    if (gAABBArray[i].current.boundingBox.collisionTest(gAABBArray[j].current.boundingBox)) {
                        addCollision(ref collisions, gAABBArray[i], gAABBArray[j]);
                    }
                }
            }
            // Compute variance (less a, for comparison unnecessary, constant factor)
            for (int c = 0; c < 3; c++)
                v[c] = s2[c] - s[c] * s[c] / gAABBArray.Count;
            // Update axis sorted to be the one with greatest AABB variance
            gSortAxis = 0;
            if (v[1] > v[0]) gSortAxis = 1;
            if (v[2] > v[gSortAxis]) gSortAxis = 2;
            return collisions;
        }

        private void addCollision(ref Dictionary<MovableObject, List<MovableObject>> collisions, MovableObject box1, MovableObject box2) {
            List<MovableObject> box1Collisions;
            if (!collisions.TryGetValue(box1, out box1Collisions)) {
                box1Collisions = new List<MovableObject>();
                collisions.Add(box1, box1Collisions);
            }
            box1Collisions.Add(box2);
        }
    }
}
