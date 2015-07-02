using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using MIConvexHull;

namespace Ascendant.Graphics.objects {
    class Mesh {
        public PrimitiveType type { get; private set; }
        public Vector3[] vertices { get; private set; }
        public Vector2[] texCoords { get; private set; }
        public Vector3[] normals { get; private set; }
        public ConvexHull<DefaultVertex, DefaultConvexFace<DefaultVertex>> convexHull { get; private set; }

        public Mesh(Vector3[] _v, Vector2[] _t, Vector3[] _n, PrimitiveType _type) {
            vertices = _v;
            texCoords = _t;
            normals = _n;
            this.type = _type;
            var convexVertices = new List<double[]>();
            for (int i = 0; i < vertices.Length; ++i) {
                Double[] pos = new Double[3];
                convexVertices.Add(pos);
                pos[0] = vertices[i].X;
                pos[1] = vertices[i].Y;
                pos[2] = vertices[i].Z;
            }
            convexHull = MIConvexHull.ConvexHull.Create(convexVertices);
        }

        /**
         * Giftwrapping / Jarvis' march
         */
        static bool isLeft(Vector3 a, Vector3 b, Vector3 c) {
            float u1 = b.X - a.X;
            float v1 = b.Z - a.Z;
            float u2 = c.X - a.X;
            float v2 = c.Z - a.Z;
            return u1 * v2 - v1 * u2 < 0;
        }

        static bool comparePoints(Vector3 a, Vector3 b) {
            if (a.X < b.X) return true;
            if (a.X > b.X) return false;
            if (a.Z < b.Z) return true;
            if (a.Z > b.Z) return true;
            return false;
        }

        private static int[] CalculateConvexHull(Vector3[] vertices) {
            int hull = 0;
            for (int i = 1; i < vertices.Length; ++i) {
                if (comparePoints(vertices[i], vertices[hull]))
                    hull = i;
            }
            int endpt = 0;
            var convexHullIndices = new List<int>();
            do {
                convexHullIndices.Add(hull);
                endpt = 0;
                for (int j = 1; j < vertices.Length; ++j) {
                    if (hull == endpt || isLeft(vertices[hull], vertices[endpt], vertices[j])) {
                        endpt = j;
                    }
                }
                hull = endpt;
            } while (endpt != convexHullIndices[0]);
            return convexHullIndices.ToArray();
        }


    }
}
