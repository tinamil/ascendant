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

        BulletSharp.RigidBody rigidBody;

        public override BulletSharp.RigidBody body {
            get { return rigidBody; }
        }

        internal MovableObject(World world, int matNumber, Lighting.PointLight light, List<MovableObject> children,
            float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale,
            Vector3 angularMomentum, Mesh mesh)
            : base(world, matNumber, light, mesh, children) {
            this.myScale = scale;
            Matrix4 Translate = Matrix4.CreateTranslation(position);
            Matrix4 Rotate = Matrix4.CreateFromQuaternion(orientation);
            var modelToWorld = Rotate * Translate;
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

            var constructionInfo = new BulletSharp.RigidBodyConstructionInfo(mass, motionState, convexShape);
            rigidBody = new BulletSharp.RigidBody(constructionInfo);

            rigidBody.AngularVelocity = angularMomentum;
            rigidBody.LinearVelocity = momentum;
        }
    }
}
