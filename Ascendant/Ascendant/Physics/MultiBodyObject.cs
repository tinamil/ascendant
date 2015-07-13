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
        public Type type;

        public enum Type {
            Fixed, Planar, Prismatic, Revolute, Spherical
        }

        //All
        public int linkIndex;
        public float mass;
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
        public GameObject part { get; set; }
        public CollisionShape shape { get; set; }
    }

    class MultiBodyObject : GameObject {

        override protected Vector3 scale { get { return myScale; } }

        private Vector3 myScale;

        public MultiBody mBody { get; private set; }
        public ConvexHullShape shape { get; private set; }

        protected override Matrix4 WorldTransform {
            get { return mBody.BaseCollider.WorldTransform; }
        }

        List<Joint> children;

        internal MultiBodyObject(World world, bool isBase, int matNumber, List<Lighting.PointLight> lightList, List<Joint> children,
            float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale,
            Vector3 angularMomentum, Mesh mesh, Matrix4 parentTransform)
            : base(world, matNumber, lightList, mesh, new List<GameObject>()) {
            this.myScale = scale;
            this.children = children;
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
            var tmpConvexShape = new ConvexTriangleMeshShape(meshInterface);

            //create a hull approximation
            var hull = new BulletSharp.ShapeHull(tmpConvexShape);
            float margin = tmpConvexShape.Margin;
            hull.BuildHull(margin);
            tmpConvexShape.UserObject = hull;

            shape = new ConvexHullShape();
            foreach (Vector3 v in hull.Vertices) {
                shape.AddPoint(v);
            }

            shape.LocalScaling = scale;

            Vector3 inertia = Vector3.Zero;
            shape.CalculateLocalInertia(mass, out inertia);

            bool IsFixedBase = false;
            bool CanSleep = true;
            bool IsMultiDoF = true;
            mBody = new BulletSharp.MultiBody(children.Count, mass, inertia, IsFixedBase, CanSleep, IsMultiDoF);

            mBody.BasePosition = position;
            mBody.WorldToBaseRot = orientation;
            if (momentum != Vector3.Zero) mBody.BaseVelocity = momentum;

            int baseIndex = -1;
            int jointIndex = 0;
            foreach (var joint in children) {
                setupJoint(baseIndex, ref jointIndex, joint);
            }
        }

        private void setupJoint(int parentIndex, ref int jointIndex, Joint joint) {
            joint.link.part.SetParent(this);

            float linkMass = joint.mass == 0 ? 0.0001f : joint.mass;

            Vector3 linkInertiaDiag;
            joint.link.shape.CalculateLocalInertia(linkMass, out linkInertiaDiag);
            switch (joint.type) {
                case Joint.Type.Fixed:
                    mBody.SetupFixed(jointIndex, joint.mass, linkInertiaDiag, parentIndex, joint.rotParentToThis, joint.parentComToThisPivotOffset, joint.thisPivotToThisComOffset, joint.disableParentCollision);
                    break;
                case Joint.Type.Planar:
                    mBody.SetupPlanar(jointIndex, joint.mass, linkInertiaDiag, parentIndex, joint.rotParentToThis, joint.parentComToThisPivotOffset, joint.parentComToThisPivotOffset);
                    break;
                case Joint.Type.Prismatic:
                    mBody.SetupPrismatic(jointIndex, joint.mass, linkInertiaDiag, parentIndex, joint.rotParentToThis, joint.parentComToThisPivotOffset, joint.parentComToThisPivotOffset, joint.thisPivotToThisComOffset, joint.disableParentCollision);
                    break;
                case Joint.Type.Revolute:
                    mBody.SetupRevolute(jointIndex, joint.mass, linkInertiaDiag, parentIndex, joint.rotParentToThis, joint.movementAxis, joint.parentComToThisPivotOffset, joint.thisPivotToThisComOffset);
                    break;
                case Joint.Type.Spherical:
                    mBody.SetupSpherical(jointIndex, joint.mass, linkInertiaDiag, parentIndex, joint.rotParentToThis, joint.parentComToThisPivotOffset, joint.thisPivotToThisComOffset);
                    break;
            }
            int thisIndex = jointIndex;
            jointIndex += 1;
            foreach (Joint j in joint.link.children) {
                setupJoint(thisIndex, ref jointIndex, j);
            }
        }
        private List<Joint> getChildren(Joint joint) {
            List<Joint> children = joint.link.children;
            foreach (Joint j in children) {
                children.AddRange(getChildren(j));
            }
            return children;
        }
        internal Joint[] generateLinks() {
            List<Joint> allChildren = children;
            foreach (Joint j in allChildren) {
                allChildren.AddRange(getChildren(j));
            }
            return allChildren.ToArray();
        }
    }
}
