using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Physics;
using Ascendant.Graphics.lighting;
using OpenTK;
using System.Diagnostics;
using BulletSharp;

namespace Ascendant.Graphics.objects {
    class World {
        readonly protected List<GameObject> children = new List<GameObject>();

        protected MultiBodyDynamicsWorld physicsWorld;
        internal protected Lighting lights;

        readonly Stopwatch timer = Stopwatch.StartNew();
        private long stoppedTime = 0L;

        readonly protected internal Game parentGame;

        protected internal int modelToCameraMatrixUnif;
        protected internal int normalModelToCameraMatrixUnif;
        private bool g_useGammaDisplay = true;

        public World(Game game) {
            this.parentGame = game;
            this.parentGame.window.FocusedChanged += (o, e) => {
                if (this.parentGame.window.Focused)
                    timer.Start();
                else
                    timer.Stop();
            };
            game.window.KeyPress += (o, e) => {
                if (e.KeyChar == 'g') {
                    g_useGammaDisplay = !g_useGammaDisplay;
                }
            };
        }

        public void Render() {
            if (g_useGammaDisplay)
                GL.Enable(EnableCap.FramebufferSrgb);
            else
                GL.Disable(EnableCap.FramebufferSrgb);

            lights.Render(parentGame.Camera.GetWorldToCameraMatrix(), timer.ElapsedMilliseconds);
            foreach (GameObject obj in children) {
                obj.Render();
            }
        }


        internal void InitializeOpenGL(int program) {
            modelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "modelToCameraMatrix");
            normalModelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "normalModelToCameraMatrix");
            lights.InitializeOpenGL(program);
            foreach (GameObject obj in children) {
                obj.InitializeOpenGL(program);
            }
            MaterialLoader.LoadMaterialBufferBlock(program);
        }

        internal void Pause() {
            timer.Stop();
            stoppedTime = timer.ElapsedMilliseconds;
        }
        internal void Resume() {
            timer.Start();
        }
        internal void Update() {
            if (timer.IsRunning) {
                RunSim(timer.ElapsedMilliseconds);
            }
        }

        internal long currentTime() {
            if (timer.IsRunning) return timer.ElapsedMilliseconds;
            else return stoppedTime;
        }

        internal void addRootObjects(List<GameObject> newObjects) {
            this.children.AddRange(newObjects);
            lights = new Lighting(children);
        }

        internal void setSun(TimedLinearInterpolator<Sun> sunTimer) {
            lights.setSun(sunTimer);
        }

        internal Vector4 getBackgroundColor() {
            return lights.sunTimer.Interpolate(Sun.Alpha(timer.ElapsedMilliseconds)).background;
        }

        public void setupSimulation() {
            BroadphaseInterface broadphase = new DbvtBroadphase();

            // Set up the collision configuration and dispatcher
            CollisionConfiguration collisionConfiguration = new DefaultCollisionConfiguration();
            CollisionDispatcher dispatcher = new CollisionDispatcher(collisionConfiguration);

            // The actual physics solver
            MultiBodyConstraintSolver solver = new BulletSharp.MultiBodyConstraintSolver();

            // The world.
            physicsWorld = new BulletSharp.MultiBodyDynamicsWorld(dispatcher, broadphase, solver, collisionConfiguration);
            //dynamicsWorld.Gravity = new Vector3(0, 0f, 0);

            foreach (GameObject obj in children) {
                var mObj = obj as MultiBodyObject;
                if (mObj != null) {
                    addMultiBodyObject(mObj);
                }
                var rObj = obj as RigidBodyObject;
                if (rObj != null) {
                    addObjToPhysics(rObj);
                }
            }

            physicsWorld.SolverInfo.RestingContactRestitutionThreshold = 5;
            physicsWorld.SolverInfo.SplitImpulse = 1;
            physicsWorld.SolverInfo.SplitImpulsePenetrationThreshold = -0.02f;
            physicsWorld.SolverInfo.NumIterations = 20;
        }

        private void addObjToPhysics(RigidBodyObject obj) {
            physicsWorld.AddRigidBody(obj.rigidBody);

            if (obj.constraint != null) physicsWorld.AddConstraint(obj.constraint);
            foreach (RigidBodyObject child in obj.children) {
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

                    physicsWorld.AddCollisionObject(col, 2, 1 + 2);

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
        public void RunSim(long elapsedMillis) {

            float newTime = elapsedMillis / 1000.0f;
            float frameTime = newTime - prevTime;
            if (frameTime > .250) frameTime = .250f;
            if (frameTime < 0) return;
            prevTime = newTime;
            checkForce();
            physicsWorld.StepSimulation(frameTime, 10, dt);

            //      int numManifolds = dynamicsWorld.Dispatcher.NumManifolds;
            //      for (int i=0;i<numManifolds;i++) {
            //  PersistentManifold contactManifold =  dynamicsWorld.Dispatcher.GetManifoldByIndexInternal(i);
            //CollisionObject obA = contactManifold.Body0;
            //  CollisionObject obB = contactManifold.Body1;
            //      }
        }

        private void checkForce() {
            //if (parentGame.pressedKeys.Contains(OpenTK.Input.Key.J)) objects.Last().body.ApplyCentralForce(Vector3.UnitX);
            //if (parentGame.pressedKeys.Contains(OpenTK.Input.Key.L)) objects.Last().body.ApplyCentralForce(-Vector3.UnitX);
            //if (parentGame.pressedKeys.Contains(OpenTK.Input.Key.K)) objects.Last().body.ApplyCentralForce(-Vector3.UnitY);
            //if (parentGame.pressedKeys.Contains(OpenTK.Input.Key.I)) objects.Last().body.ApplyCentralForce(Vector3.UnitY);
            //if (parentGame.pressedKeys.Contains(OpenTK.Input.Key.U)) objects.Last().body.ApplyCentralForce(Vector3.UnitZ);
            //if (parentGame.pressedKeys.Contains(OpenTK.Input.Key.O)) objects.Last().body.ApplyCentralForce(-Vector3.UnitZ);
        }
    }
}
