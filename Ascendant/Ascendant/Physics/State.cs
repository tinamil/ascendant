using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Ascendant.Graphics.objects;
using MIConvexHull;
using System.Diagnostics;

namespace Ascendant.Physics {
    /// Physics state.
    internal struct State {
        /// primary physics state
        internal Vector3 position;      // the position of the center of mass in world coordinates (meters).
        internal Vector3 momentum;                // the momentum of the cube in kilogram meters per second.
        internal Quaternion orientation;      // the orientation of the cube represented by a unit quaternion.
        internal Vector3 angularMomentum;         // angular momentum vector.
        internal Vector3 scale;             //Scale of the object
        public SimpleAABB boundingBox;            //Current axis aligned bounding box in world coordinates

        // secondary state
        internal Vector3 velocity;                // velocity in meters per second (calculated from momentum).
        internal Quaternion spin;                // quaternion rate of change in orientation.
        internal Vector3 angularVelocity;         // angular velocity (calculated from angularMomentum).

        /// constant state
        internal float mass;                     // mass of the cube in kilograms.
        internal float inverseMass;              // inverse of the mass used to convert momentum to velocity.
        internal float inertiaTensor;            // inertia tensor of the cube (i have simplified it to a single value due to the mass properties a cube).
        internal double inverseInertiaTensor;     // inverse inertia tensor used to convert angular momentum to angular velocity.
        internal SimpleAABB originalBoundingBox;  // Axis Aligned Bounding Box in model coordinates
        internal MovableObject parent;
        internal Mesh mesh;

        /// Default constructor.
        internal State(float size, float mass, Vector3 position, Vector3 momentum,
            Quaternion orientation, Vector3 scale, Vector3 angularMomentum, Mesh mesh, MovableObject parent)
            : this() {
            this.mass = mass;
            this.inverseMass = 1.0f / mass;
            this.position = position;
            this.momentum = momentum;
            this.orientation = orientation;
            this.angularMomentum = angularMomentum;
            this.inertiaTensor = mass * size * size * 1.0f / 6.0f;
            this.inverseInertiaTensor = 1.0f / inertiaTensor;
            this.scale = scale;
            this.originalBoundingBox = new SimpleAABB(mesh.vertices);
            this.parent = parent;
            this.mesh = mesh;
            recalculate(true);
        }

        public State(State current)
            : this() {
            this.mass = current.mass;
            this.inverseMass = current.inverseMass;
            this.position = current.position;
            this.momentum = current.momentum;
            this.orientation = current.orientation;
            this.angularMomentum = current.angularMomentum;
            this.inertiaTensor = current.inertiaTensor;
            this.inverseInertiaTensor = current.inverseInertiaTensor;
            this.scale = current.scale;
            this.originalBoundingBox = current.originalBoundingBox;
            this.parent = current.parent;
            this.mesh = current.mesh;
        }
        internal static State Lerp(State previous, State current, float blend) {
            State retVal = new State(current);
            retVal.position = Vector3.Lerp(previous.position, current.position, blend);
            retVal.orientation = Quaternion.Slerp(previous.orientation, current.orientation, blend);
            retVal.scale = current.scale;
            return retVal;
        }
        /// Recalculate secondary state values from primary values.

        internal void recalculate(bool updateBoundingBox) {
            velocity = momentum * inverseMass;
            angularVelocity = angularMomentum * (float)inverseInertiaTensor;
            orientation.Normalize();
            spin = Quaternion.Multiply(new Quaternion(angularVelocity.X, angularVelocity.Y, angularVelocity.Z, 0), 0.5f) * orientation;
            SimpleAABB.Update(originalBoundingBox, getModelToWorldMatrix(), out boundingBox);
        }

        public Matrix4 getModelToWorldMatrix(float scalingFactor = 1.0f) {
            Matrix4 Translate = Matrix4.CreateTranslation(position);
            //Rotate
            Matrix4 Rotate = Matrix4.CreateFromQuaternion(orientation);
            //Scale
            Matrix4 Scale = Matrix4.CreateScale(scale * scalingFactor);
            // return Translate * Rotate * Scale * parentMatrix;
            return getParentMatrix() * Scale * Rotate * Translate;
        }

        private Matrix4 getParentMatrix() {
            return parent.getParentMatrix();
        }

        DefaultConvexFace<DefaultVertex> lastFace;
        private Vector3 searchAdjacentFaces(Vector3 direction) {
            Matrix4 transform = getModelToWorldMatrix();
            DefaultConvexFace<DefaultVertex> currentFace = lastFace ?? mesh.convexHull.Faces.ElementAt(0);
            DefaultVertex currentVertex = currentFace.Vertices[0];
            DefaultVertex bestVertex = currentVertex;
            Vector3 pos = new Vector3((float)bestVertex.Position[0], (float)bestVertex.Position[1], (float)bestVertex.Position[2]);
            float dot = Vector3.Dot(Vector3.Transform(pos, transform), direction);
            float bestMax = dot;
            float max = bestMax;
            do {
                if (max > bestMax) {
                    bestMax = max;
                    bestVertex = currentVertex;
                    lastFace = currentFace;
                }

                Dictionary<DefaultVertex, DefaultConvexFace<DefaultVertex>> dict = GetAdjacentVertices(ref currentFace, ref currentVertex);

                foreach (DefaultVertex vertex in dict.Keys) {
                    pos = new Vector3((float)vertex.Position[0], (float)vertex.Position[1], (float)vertex.Position[2]);
                    dot = Vector3.Dot(Vector3.Transform(pos, transform), direction);
                    if (dot > max) {
                        max = dot;
                        currentFace = dict[vertex];
                        currentVertex = vertex;
                    }
                }

            } while (bestMax != max);

            return new Vector3((float)bestVertex.Position[0], (float)bestVertex.Position[1], (float)bestVertex.Position[2]);
        }

        private Dictionary<DefaultVertex, DefaultConvexFace<DefaultVertex>> GetAdjacentVertices(ref DefaultConvexFace<DefaultVertex> currentFace, ref DefaultVertex currentVertex) {
            var adj = new Dictionary<DefaultVertex, DefaultConvexFace<DefaultVertex>>();
            int currentVertexIndex = 0;
            for (int i = 0; i < currentFace.Vertices.Length; ++i) {
                if (currentFace.Vertices[i] == currentVertex) {
                    currentVertexIndex = i;
                    break;
                }
            }
            for (int i = 0; i < currentFace.Adjacency.Length; ++i) {
                if (i != currentVertexIndex) {
                    foreach (DefaultVertex v in currentFace.Adjacency[i].Vertices) {
                        if (v != currentFace.Vertices[currentVertexIndex] && !adj.ContainsKey(v)) {
                            adj.Add(v, currentFace.Adjacency[i]);
                        }
                    }
                }
            }
            return adj;
        }

        public Vector3d MaxPointAlongDirectionOfConvexHull(Vector3d directionToMove, double scalingFactor = 1.0) {
            //Vector3 bestGreedy = searchAdjacentFaces(directionToMove);

            //Naive global search solution
            Matrix4 transform = getModelToWorldMatrix((float)scalingFactor);
            Matrix4d transformD = new Matrix4d(
                transform.M11, transform.M12, transform.M13, transform.M14, 
                transform.M21, transform.M22, transform.M23, transform.M24,
                transform.M31, transform.M32, transform.M33, transform.M34,
                transform.M41, transform.M42, transform.M43, transform.M44);

            double max = double.NegativeInfinity;
            Vector3d point = Vector3d.Zero;
            
            foreach (DefaultVertex vertex in mesh.convexHull.Points) {
                Vector3d pos = new Vector3d(vertex.Position[0], vertex.Position[1], vertex.Position[2]);
                Vector3d.Transform(ref pos, ref transformD, out pos);
                double dot = Vector3d.Dot(pos, directionToMove);
                if (dot > max) {
                    max = dot;
                    point = pos;
                }
            }
            //Debug.Assert(bestGreedy == point);
            return point;
        }
    }

    struct Derivative {
        Vector3 velocity;                // velocity is the derivative of position.
        Vector3 force;                   // force in the derivative of momentum.
        Quaternion spin;                // spin is the derivative of the orientation quaternion.
        Vector3 torque;                  // torque is the derivative of angular momentum.

        static Derivative evaluate(State state, float t) {
            Derivative output = new Derivative();
            output.velocity = state.velocity;
            output.spin = state.spin;
            forces(state, t, out output.force, out output.torque);
            return output;
        }

        /// Evaluate derivative values for the physics state at future time t+dt 
        /// using the specified set of derivatives to advance dt seconds from the 
        /// specified physics state.

        static Derivative evaluate(State state, float t, float dt, ref Derivative derivative) {
            state.position += derivative.velocity * dt;
            state.momentum += derivative.force * dt;
            state.orientation += derivative.spin * dt;
            state.angularMomentum += derivative.torque * dt;
            state.recalculate(false);

            Derivative output;
            output.velocity = state.velocity;
            output.spin = state.spin;
            forces(state, t + dt, out output.force, out output.torque);
            return output;
        }
        /// Integrate physics state forward by dt seconds.
        /// Uses an RK4 integrator to numerically integrate with error O(5).

        internal static void integrate(ref State state, float t, float dt) {
            Derivative a = evaluate(state, t);
            Derivative b = evaluate(state, t, dt * 0.5f, ref a);
            Derivative c = evaluate(state, t, dt * 0.5f, ref b);
            Derivative d = evaluate(state, t, dt, ref c);

            state.position += 1.0f / 6.0f * dt * (a.velocity + 2.0f * (b.velocity + c.velocity) + d.velocity);
            state.momentum += 1.0f / 6.0f * dt * (a.force + 2.0f * (b.force + c.force) + d.force);
            state.orientation += 1.0f / 6.0f * dt * (a.spin + 2.0f * (b.spin + c.spin) + d.spin);
            state.angularMomentum += 1.0f / 6.0f * dt * (a.torque + 2.0f * (b.torque + c.torque) + d.torque);

            state.recalculate(true);
        }

        /// Calculate force and torque for physics state at time t.
        /// Due to the way that the RK4 integrator works we need to calculate
        /// force implicitly from state rather than explictly applying forces
        /// to the rigid body once per update. This is because the RK4 achieves
        /// its accuracy by detecting curvature in derivative values over the 
        /// timestep so we need our force values to supply the curvature.

        static void forces(State state, float t, out Vector3 force, out Vector3 torque) {
            torque = force = Vector3.Zero;
            //collisionDetection(state, ref force, ref torque);
            //gravity(ref force);
            //damping(state, ref force, ref torque);
            //control(input, state, force, torque);
        }

        private static void collisionDetection(State state, ref Vector3 force, ref Vector3 torque) {
            force = Vector3.Zero;
            torque = Vector3.Zero;
            //AxisAlignedBoundingBox[] collisionTargets = state.bvt.getCollisions(state.orientedBound);
            //if (collisionTargets.Length > 0) {
            // Debug.WriteLine("Collisions: " + collisionTargets.Length);
            // }
        }

        /// Calculate gravity force.
        /// @param force the force accumulator.
        static void gravity(ref Vector3 force) {
            force.Y -= 9.8f;
        }

        /// Calculate a simple linear and angular damping force.
        /// This roughly simulates energy loss due to heat dissipation
        /// or air resistance or whatever you like.
        /// @param state the current cube physics state.
        /// @param force the force accumulator.
        /// @param torque the torque accumulator.

        static void damping(State state, ref Vector3 force, ref Vector3 torque) {
            const float linear = 0.001f;
            const float angular = 0.001f;

            force -= linear * state.velocity;
            torque -= angular * state.angularVelocity;
        }
    }
}
