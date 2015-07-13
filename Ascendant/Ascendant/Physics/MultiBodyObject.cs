using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics;
using OpenTK;
using Ascendant.Graphics.objects;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Graphics.lighting;
using BulletSharp;

namespace Ascendant.Physics {
    class Joint {
        public MultiBodyPart link;

        enum type {
            Fixed, Planar, Prismatic, Revolute, Spherical
        }

        //All
        public int linkIndex;
        public float mass;
        public Vector3 inertia;
        public int parent;
        public Quaternion rotParentToThis;
        public Vector3 parentComToThisPivotOffset;
        public bool disableParentCollision;

        //Fixed/Prismatic/Revolute/Spherical
        public Vector3 thisPivotToThisComOffset;

        //Prismatic/Revolute
        public Vector3 movementAxis;
    }

    class MultiBodyPart {
        readonly public List<Joint> children = new List<Joint>();
        public GameObject part;
    }

    class MultiBodyObject : GameObject {

        override protected Vector3 scale { get { return myScale; } }

        private Vector3 myScale;

        public BulletSharp.MultiBody mBody { get; private set; }

        protected override Matrix4 WorldTransform {
            get { return mBody.BaseCollider.WorldTransform; }
        }

        internal MultiBodyObject(World world, bool isBase, int matNumber, List<Lighting.PointLight> lightList, List<Joint> children,
            float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale,
            Vector3 angularMomentum, Mesh mesh, Matrix4 parentTransform)
            : base(world, matNumber, lightList, mesh, new List<GameObject>()) {
            this.myScale = scale;
            Matrix4 Scale = Matrix4.CreateScale(scale);
            Matrix4 Translate = Matrix4.CreateTranslation(position);
            Matrix4 Rotate = Matrix4.CreateFromQuaternion(orientation);
            //var modelToWorld = parentTransform * Scale * Rotate * Translate;
            //var motionState = new BulletSharp.DefaultMotionState(modelToWorld);

            var meshInterface = new BulletSharp.TriangleMesh();
            Vector3[] triangle = new Vector3[3];
            for (int index = 0; index < mesh.vertices.Length; ++index) {
                if (index % 3 == 0 && index > 0) {
                    meshInterface.AddTriangle(triangle[0], triangle[1], triangle[2]);
                }
                triangle[index % 3] = mesh.vertices[index];
            }
            BulletSharp.ConvexShape tmpConvexShape = new BulletSharp.ConvexTriangleMeshShape(meshInterface);

            ////create a hull approximation
            //var hull = new BulletSharp.ShapeHull(tmpConvexShape);
            //float margin = tmpConvexShape.Margin;
            //hull.BuildHull(margin);
            //tmpConvexShape.UserObject = hull;

            //var convexShape = new BulletSharp.ConvexHullShape();
            //foreach (Vector3 v in hull.Vertices) {
            //    convexShape.AddPoint(v);
            //}

            tmpConvexShape.LocalScaling = scale;

            Vector3 inertia = Vector3.Zero;
            tmpConvexShape.CalculateLocalInertia(mass, out inertia);

            bool IsFixedBase = false;
            bool CanSleep = true;
            bool IsMultiDoF = true;
            mBody = new BulletSharp.MultiBody(children.Count, mass, inertia, IsFixedBase, CanSleep, IsMultiDoF);

            mBody.BasePosition = position;
            mBody.WorldToBaseRot = orientation;
            if (momentum != Vector3.Zero) mBody.BaseVelocity = momentum;

            int i = -1;
            foreach (var joint in children) {
                i += 1;
                joint.link.part.SetParent(this);

                float linkMass = joint.mass == 0 ? 0.0001f : joint.mass;

                Vector3 linkInertiaDiag;
                {
                    BulletSharp.CollisionShape shape = joint.link.part.shape;
                    if (i == 0) {
                        shape = new BulletSharp.BoxShape(linkHalfExtents);//
                    } else {
                        shape = new BulletSharp.SphereShape(radius);
                    }
                    shape.CalculateLocalInertia(linkMass, out linkInertiaDiag);
                }

                if (!spherical) {
                    //pMultiBody->setupRevolute(i, linkMass, linkInertiaDiag, i - 1, btQuaternion(0.f, 0.f, 0.f, 1.f), hingeJointAxis, parentComToCurrentPivot, currentPivotToCurrentCom, false);

                    if (i == 0) {
                        mBody.SetupRevolute(i, linkMass, linkInertiaDiag, i - 1,
                            new Quaternion(0f, 0f, 0f, 1f),
                            hingeJointAxis,
                            parentComToCurrentPivot,
                            currentPivotToCurrentCom, false);
                    } else {
                        parentComToCurrentCom = new Vector3(0, -linkHalfExtents[1], 0);						//par body's COM to cur body's COM offset
                        currentPivotToCurrentCom = new Vector3(0, 0, 0);							//cur body's COM to cur body's PIV offset
                        //btVector3 parentComToCurrentPivot = parentComToCurrentCom - currentPivotToCurrentCom;	//par body's COM to cur body's PIV offset


                        mBody.SetupFixed(i, linkMass, linkInertiaDiag, i - 1,
                        new Quaternion(0f, 0f, 0f, 1f),
                        parentComToCurrentPivot,
                        currentPivotToCurrentCom, false);
                    }

                    //pMultiBody->setupFixed(i,linkMass,linkInertiaDiag,i-1,btQuaternion(0,0,0,1),parentComToCurrentPivot,currentPivotToCurrentCom,false);

                } else {
                    //pMultiBody->setupPlanar(i, linkMass, linkInertiaDiag, i - 1, btQuaternion(0.f, 0.f, 0.f, 1.f)/*quat0*/, btVector3(1, 0, 0), parentComToCurrentPivot*2, false);
                    mBody.SetupSpherical(i, linkMass, linkInertiaDiag, i - 1, new Quaternion(0f, 0f, 0f, 1f), parentComToCurrentPivot, currentPivotToCurrentCom, false);
                }
            }
        }

        private void addColliders(BulletSharp.MultiBody pMultiBody, MultiBodyDynamicsWorld pWorld, Mesh parentMesh, Joint[] links) {
            Quaternion[] world_to_local = new Quaternion[pMultiBody.NumLinks + 1];

            var local_origin = new Vector3[pMultiBody.NumLinks + 1];

            world_to_local[0] = pMultiBody.WorldToBaseRot;
            local_origin[0] = pMultiBody.BasePosition;
            {

                //	float pos[4]={local_origin[0].x(),local_origin[0].y(),local_origin[0].z(),1};
                var quat = new float[4] { -world_to_local[0].X, -world_to_local[0].Y, -world_to_local[0].Z, world_to_local[0].W };


                if (true) // Base
                {
                    CollisionShape box = new BoxShape(baseHalfExtents);
                    var col = new MultiBodyLinkCollider(pMultiBody, -1);
                    col.CollisionShape = box;

                    Matrix4 tr = Matrix4.CreateTranslation(local_origin[0]);
                    tr = tr * Matrix4.CreateFromQuaternion(new Quaternion(quat[0], quat[1], quat[2], quat[3]));
                    col.WorldTransform = tr;

                    pWorld.AddCollisionObject(col, 2, 1 + 2);

                    col.Friction = (1f);
                    pMultiBody.BaseCollider = col;
                }
            }


            for (int i = 0; i < pMultiBody.NumLinks; ++i) {
                int parent = pMultiBody.GetParent(i);
                world_to_local[i + 1] = pMultiBody.GetParentToLocalRot(i) * world_to_local[parent + 1];
                local_origin[i + 1] = local_origin[parent + 1] + (Vector3.Transform(pMultiBody.GetRVector(i), world_to_local[i + 1].Inverted()));
            }

            for (int i = 0; i < pMultiBody.NumLinks; ++i) {

                Vector3 posr = local_origin[i + 1];
                //	float pos[4]={posr.x(),posr.y(),posr.z(),1};

                var quat = new float[4] { -world_to_local[i + 1].X, -world_to_local[i + 1].Y, -world_to_local[i + 1].Z, world_to_local[i + 1].W };

                CollisionShape box = new BoxShape(linkHalfExtents);
                MultiBodyLinkCollider col = new MultiBodyLinkCollider(pMultiBody, i);

                col.CollisionShape = box;
                Matrix4 tr = Matrix4.CreateTranslation(posr);
                tr = tr * Matrix4.CreateFromQuaternion(new Quaternion(quat[0], quat[1], quat[2], quat[3]));
                col.WorldTransform = tr;
                col.Friction = (1f);
                pWorld.AddCollisionObject(col, 2, 1 + 2);


                pMultiBody.GetLink(i).Collider = col;
            }
        }
    }
}
