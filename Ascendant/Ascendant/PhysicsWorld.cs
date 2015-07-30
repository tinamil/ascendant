using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BulletSharp;
using System.Diagnostics;
using OpenTK;
using Ascendant.Physics;

namespace Ascendant {
    class PhysicsWorld {

        MultiBodyDynamicsWorld physicsWorld;

        readonly protected List<IRigidBody> rigidBodies = new List<IRigidBody>();
        readonly protected List<MultiBodyObject> multiBodies = new List<MultiBodyObject>();


        readonly Stopwatch timer = Stopwatch.StartNew();
        long stoppedTime = 0L;

        public long ElapsedTime {
            get {
                if (timer.IsRunning) return timer.ElapsedMilliseconds;
                else return stoppedTime;
            }
        }

        public PhysicsWorld(Game game) {
            game.window.FocusedChanged += (o, e) => {
                if (game.window.Focused)
                    timer.Start();
                else
                    timer.Stop();
            };
        }

        public void Pause() {
            timer.Stop();
            stoppedTime = timer.ElapsedMilliseconds;
        }

        public void Resume() {
            timer.Start();
        }

        public void Update() {
            if (timer.IsRunning) {
                RunSim(timer.ElapsedMilliseconds);
            }
        }

        public void addObject(DisplayObject newObject) {
            var multiBodyObject = newObject as MultiBodyObject;
            if(multiBodyObject != null) {
                addObject(multiBodyObject);
            }
            var rigidObject = newObject as IRigidBody;
            if(rigidObject != null) {
                addObject(rigidObject);
            }
        }

        public void addObject(MultiBodyObject newObject) {
            multiBodies.Add(newObject);
            SetupSimulation();
        }

        public void addObject(IRigidBody newObject) {
            rigidBodies.Add(newObject);
            SetupSimulation();
        }

        private void SetupSimulation() {
            BroadphaseInterface broadphase = new DbvtBroadphase();

            // Set up the collision configuration and dispatcher
            CollisionConfiguration collisionConfiguration = new DefaultCollisionConfiguration();
            CollisionDispatcher dispatcher = new CollisionDispatcher(collisionConfiguration);

            // The actual physics solver
            MultiBodyConstraintSolver solver = new BulletSharp.MultiBodyConstraintSolver();

            // The world.
            physicsWorld = new BulletSharp.MultiBodyDynamicsWorld(dispatcher, broadphase, solver, collisionConfiguration);
            //dynamicsWorld.Gravity = new Vector3(0, 0f, 0);

            foreach (IRigidBody obj in rigidBodies) {
                var rObj = obj as IRigidBody;
                if (rObj != null) {
                    addObjToPhysics(rObj);
                }
            }

            physicsWorld.SolverInfo.RestingContactRestitutionThreshold = 5;
            physicsWorld.SolverInfo.SplitImpulse = 1;
            physicsWorld.SolverInfo.SplitImpulsePenetrationThreshold = -0.02f;
            physicsWorld.SolverInfo.NumIterations = 20;
        }

        private void addObjToPhysics(IRigidBody obj) {
            physicsWorld.AddRigidBody(obj.rigidBody);

            if (obj.constraint != null) physicsWorld.AddConstraint(obj.constraint);
            foreach (IRigidBody child in obj.RChildren) {
                addObjToPhysics(child);
            }
        }

        private void addMultiBodyObject(MultiBodyObject obj) {
            physicsWorld.AddMultiBody(obj.mBody);
            addColliders(obj);
        }



        private void addColliders(MultiBodyObject obj) {
            MultiBody mBody = obj.mBody;
            Joint[] links = obj.generateLinks();
            Quaternion[] world_to_local = new Quaternion[mBody.NumLinks + 1];

            var local_origin = new Vector3[mBody.NumLinks + 1];

            world_to_local[0] = mBody.WorldToBaseRot;
            local_origin[0] = mBody.BasePosition;
            {

                //	float pos[4]={local_origin[0].x(),local_origin[0].y(),local_origin[0].z(),1};
                var quat = new float[4] { -world_to_local[0].X, -world_to_local[0].Y, -world_to_local[0].Z, world_to_local[0].W };


                if (true) // Base
                {
                    var col = new MultiBodyLinkCollider(mBody, -1);
                    col.CollisionShape = obj.shape;

                    Matrix4 tr = Matrix4.CreateTranslation(local_origin[0]);
                    tr = tr * Matrix4.CreateFromQuaternion(new Quaternion(quat[0], quat[1], quat[2], quat[3]));
                    col.WorldTransform = tr;

                    physicsWorld.AddCollisionObject(col);

                    col.Friction = (1f);
                    mBody.BaseCollider = col;
                }
            }


            for (int i = 0; i < mBody.NumLinks; ++i) {
                int parent = mBody.GetParent(i);
                world_to_local[i + 1] = mBody.GetParentToLocalRot(i) * world_to_local[parent + 1];
                local_origin[i + 1] = local_origin[parent + 1] + (Vector3.Transform(mBody.GetRVector(i), world_to_local[i + 1].Inverted()));
            }

            for (int i = 0; i < mBody.NumLinks; ++i) {

                Vector3 posr = local_origin[i + 1];
                //	float pos[4]={posr.x(),posr.y(),posr.z(),1};

                var quat = new float[4] { -world_to_local[i + 1].X, -world_to_local[i + 1].Y, -world_to_local[i + 1].Z, world_to_local[i + 1].W };

                CollisionShape box = links[i].link.shape;
                MultiBodyLinkCollider col = new MultiBodyLinkCollider(mBody, i);

                col.CollisionShape = box;
                Matrix4 tr = Matrix4.CreateTranslation(posr);
                tr = tr * Matrix4.CreateFromQuaternion(new Quaternion(quat[0], quat[1], quat[2], quat[3]));
                col.WorldTransform = tr;
                col.Friction = (1f);
                physicsWorld.AddCollisionObject(col, 2, 1 + 2);

                mBody.GetLink(i).Collider = col;
            }
        }

        const float dt = 1.0f / 240f;
        float prevTime = 0;
        private void RunSim(long elapsedMillis) {

            float newTime = elapsedMillis / 1000.0f;
            float frameTime = newTime - prevTime;
            if (frameTime > .250) frameTime = .250f;
            if (frameTime < 0) return;
            prevTime = newTime;
            physicsWorld.StepSimulation(frameTime, 10, dt);

            //      int numManifolds = dynamicsWorld.Dispatcher.NumManifolds;
            //      for (int i=0;i<numManifolds;i++) {
            //  PersistentManifold contactManifold =  dynamicsWorld.Dispatcher.GetManifoldByIndexInternal(i);
            //CollisionObject obA = contactManifold.Body0;
            //  CollisionObject obB = contactManifold.Body1;
            //      }
        }
    }
}
