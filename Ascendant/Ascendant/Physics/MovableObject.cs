using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics;
using OpenTK;
using Ascendant.Graphics.objects;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Graphics.lighting;

namespace Ascendant.Physics {
    class MovableObject : GameObject {

        override protected Vector3 scale { get { return myScale; } }

        private Vector3 myScale;

        protected BulletSharp.RigidBody rigidBody;

        public override BulletSharp.RigidBody body {
            get { return rigidBody; }
        }

        internal MovableObject(World world, int matNumber, List<Lighting.PointLight> lightList, Dictionary<GameObject, ConeTwist> children,
            float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale,
            Vector3 angularMomentum, Mesh mesh, Matrix4 parentTransform)
            : base(world, matNumber, lightList, mesh, children.Keys) {
            this.myScale = scale;
            Matrix4 Scale = Matrix4.CreateScale(scale);
            Matrix4 Translate = Matrix4.CreateTranslation(position);
            Matrix4 Rotate = Matrix4.CreateFromQuaternion(orientation);
            var modelToWorld = parentTransform * Scale * Rotate * Translate;
            var motionState = new BulletSharp.DefaultMotionState(modelToWorld);

            var meshInterface = new BulletSharp.TriangleMesh();
            Vector3[] triangle = new Vector3[3];
            for (int i = 0; i < mesh.vertices.Length; ++i) {
                if (i % 3 == 0 && i > 0) {
                    meshInterface.AddTriangle(triangle[0], triangle[1], triangle[2]);
                }
                triangle[i % 3] = mesh.vertices[i];
            }
            BulletSharp.ConvexShape tmpConvexShape = new BulletSharp.ConvexTriangleMeshShape(meshInterface);

            //create a hull approximation
            var hull = new BulletSharp.ShapeHull(tmpConvexShape);
            float margin = tmpConvexShape.Margin;
            hull.BuildHull(margin);
            tmpConvexShape.UserObject = hull;

            var convexShape = new BulletSharp.ConvexHullShape();
            foreach (Vector3 v in hull.Vertices) {
                convexShape.AddPoint(v);
            }

            convexShape.LocalScaling = scale;

            Vector3 inertia = Vector3.Zero;
            convexShape.CalculateLocalInertia(mass, out inertia);
            var constructionInfo = new BulletSharp.RigidBodyConstructionInfo(mass, motionState, convexShape, inertia);
            rigidBody = new BulletSharp.RigidBody(constructionInfo);

            rigidBody.AngularVelocity = angularMomentum;
            rigidBody.LinearVelocity = momentum;

            foreach (var childEntry in children) {
                var child = childEntry.Key;
                var constraintStruct = childEntry.Value;
                BulletSharp.ConeTwistConstraint constraint = new BulletSharp.ConeTwistConstraint(rigidBody, child.body, constraintStruct.aFrame, constraintStruct.bFrame);
                constraint.SetLimit(constraintStruct.swingSpan1, constraintStruct.swingSpan2, constraintStruct.twist, constraintStruct.softness, constraintStruct.bias, constraintStruct.relaxation);

                child.setParent(this, constraint);
            }
        }
    }
}
