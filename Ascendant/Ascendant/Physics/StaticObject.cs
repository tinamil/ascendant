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
    class StaticObject : IRigidBody {


        private Vector3 myScale;
        public override BulletSharp.RigidBody rigidBody { get; protected set; }
        public override BulletSharp.TypedConstraint constraint { get { return null; } protected set {} }
        public override IEnumerable<IRigidBody> RChildren { get; protected set; }

        public override Matrix4 ModelToWorld {
            get { return rigidBody.MotionState.WorldTransform; }
        }

        internal StaticObject(World world, int matNumber, List<Lighting.PointLight> light, List<StaticObject> children,
            Vector3 position, Quaternion orientation, Vector3 scale, Mesh mesh)
            : base(matNumber, light, mesh) {
            this.myScale = scale;
            this.RChildren = children;
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
