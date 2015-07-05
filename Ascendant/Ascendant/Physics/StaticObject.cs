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
    class StaticObject : GameObject {


        private Vector3 myScale;
        private BulletSharp.RigidBody rigidBody;

        override protected Vector3 scale { get { return myScale; } }
        public override BulletSharp.RigidBody body {
            get { return rigidBody; }
        }

        internal StaticObject(World world, int matNumber, Lighting.PointLight light, List<StaticObject> children,
            Vector3 position, Quaternion orientation, Vector3 scale, Mesh mesh)
            : base(world, matNumber, light, mesh, children) {
            this.myScale = scale;
            Matrix4 Translate = Matrix4.CreateTranslation(position);
            Matrix4 Rotate = Matrix4.CreateFromQuaternion(orientation);
            var modelToWorld = Rotate * Translate;

            var meshInterface = new BulletSharp.TriangleMesh();
            Vector3[] triangle = new Vector3[3];
            for (int i = 0; i < mesh.vertices.Length; ++i) {
                if (i % 3 == 0 && i > 0) {
                    meshInterface.AddTriangle(triangle[0], triangle[1], triangle[2]);
                }
                triangle[i % 3] = mesh.vertices[i];
            }

            var convexShape = new BulletSharp.BvhTriangleMeshShape(meshInterface, true);

            convexShape.LocalScaling = scale;

            var motionState = new BulletSharp.DefaultMotionState(modelToWorld);

            var constructionInfo = new BulletSharp.RigidBodyConstructionInfo(0, motionState, convexShape);
            rigidBody = new BulletSharp.RigidBody(constructionInfo);
        }
    }
}
