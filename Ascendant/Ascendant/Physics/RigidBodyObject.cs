using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Ascendant.Graphics.objects;
using Ascendant.Graphics.lighting;

namespace Ascendant.Physics {
    public class RigidBodyObject : IRigidBody {
        private Vector3 myScale;

        public override BulletSharp.RigidBody rigidBody { get; protected set; }

        public override Matrix4 ModelToWorld {
            get { return rigidBody.MotionState.WorldTransform; }
        }

        public override IEnumerable<IRigidBody> RChildren {
            get {
                throw new NotImplementedException();
            }
            protected set {
                throw new NotImplementedException();
            }
        }

        public override BulletSharp.TypedConstraint constraint { get; protected set; }

        internal RigidBodyObject(World world, Dictionary<RigidBodyObject, ConeTwist> children,
            float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale,
            Vector3 angularMomentum, Mesh mesh, Matrix4 parentTransform, int matNumber, List<Lighting.PointLight> lightList)
            : base(matNumber, lightList, mesh) {
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
                var rChild = child as RigidBodyObject;
                if (rChild != null) {
                    BulletSharp.ConeTwistConstraint constraint = new BulletSharp.ConeTwistConstraint(rigidBody, rChild.rigidBody, constraintStruct.aFrame, constraintStruct.bFrame);
                    constraint.SetLimit(constraintStruct.swingSpan1, constraintStruct.swingSpan2, constraintStruct.twist, constraintStruct.softness, constraintStruct.bias, constraintStruct.relaxation);
                    rChild.constraint = constraint;
                }
            }
        }
    }
}
