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
using Ascendant.Physics.Collision;
using Ascendant.Physics.Collision.React3D;
using BulletSharp;

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
        ExpandingPolytopeAlgorithm EPA = new ExpandingPolytopeAlgorithm();
        public void RunSim(long elapsedMillis) {
            DbvtBroadphase broadphase = new DbvtBroadphase();
             // Set up the collision configuration and dispatcher
            DefaultCollisionConfiguration collisionConfiguration = new DefaultCollisionConfiguration();
            CollisionDispatcher dispatcher = new CollisionDispatcher(collisionConfiguration);

            // The actual physics solver
            SequentialImpulseConstraintSolver solver = new SequentialImpulseConstraintSolver();

            // The world.
            DiscreteDynamicsWorld dynamicsWorld = new DiscreteDynamicsWorld(dispatcher, broadphase, solver, collisionConfiguration);
            dynamicsWorld.Gravity = new Vector3(0, -9.8f, 0);

            CollisionShape groundShape = new StaticPlaneShape(new Vector3(0, 1, 0), 1);
            CollisionShape fallShape = new SphereShape(1);

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
                            ContactInfo contact;
                            if (GilbertJohnsonKeerthi.CollisionTest(obj, box, out contact)) {
                                    CollisionResponse.collision(ref obj, ref box, contact);
                            }
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
