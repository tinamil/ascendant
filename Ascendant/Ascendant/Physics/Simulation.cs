using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using OpenTK;
using Ascendant.Graphics;
using OpenTK.Graphics.OpenGL4;
using MIConvexHull;

namespace Ascendant.Physics {

    class Simulation {

        readonly protected List<MovableObject> objects;

        SortSweep broadCollisionTest;
        //BoundingVolumeTree bvt;

        public Simulation(List<MovableObject> worldObjects) {
            this.objects = worldObjects;
            broadCollisionTest = new SortSweep(objects);
        }


        // This method will be called when the thread is started.
        float t = 0.0f;
        const float dt = 0.01f;
        double accumulator = 0.0;
        double prevTime = 0;

        public void RunSim(long elapsedMillis) {
            double newTime = elapsedMillis / 1000.0;
            double frameTime = newTime - prevTime;
            if (frameTime > .250) frameTime = .250;
            if (frameTime < 0) return;
            prevTime = newTime;
            accumulator += frameTime;
            while (accumulator >= dt) {
                var collisions = broadCollisionTest.SortAndSweepAABBArray();
                for (int i = 0; i < objects.Count; ++i) {
                    var obj = objects[i];
                    List<MovableObject> collideList;
                    if (collisions.TryGetValue(objects[i], out collideList)) {
                        for (int j = 0; j < collideList.Count; ++j) {
                            var box = collideList[j];
                            CollisionResponse.collision(ref obj, ref box, Vector3.UnitY, Vector3.Zero);
                        }
                    }
                }
                for (int i = 0; i < objects.Count; ++i) {
                    objects[i].update(t, dt);
                }
                t += dt;
                accumulator -= dt;
            }
            float alpha = (float)(accumulator / dt);
            //Interpolate
            foreach (MovableObject obj in objects) {
                obj.lerp(alpha);
            }
        }

        internal void setupBVH() {
            //var list = new List<Primitive>();
            //foreach (PhysicsObject obj in objects) {
            //    Mesh mesh = obj.current.mesh;
            //    int primitiveSize = mesh.type == PrimitiveType.Triangles ? 3 : 4;
            //    Primitive[] primitives = new Primitive[mesh.vertices.Length / primitiveSize];
            //    for (int pIndex = 0; pIndex < primitives.Length; ++pIndex) {
            //        primitives[pIndex] = new Primitive(mesh, pIndex * primitiveSize);
            //    }
            //    list.AddRange(primitives);
            //}
            //bvt = new BoundingVolumeTree(list.ToArray());
        }

        internal void testCollisions() {
            //var collisions = new Dictionary<ComplexAABB, List<ComplexAABB>>();
            //BoundingVolumeTree.BVHCollision(ref collisions, bvt.root, bvt.root);
        }
    }
}
