using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using OpenTK;
using Ascendant.Graphics;

namespace Ascendant.Physics {

    class Simulation {
        List<PhysicsObject> objects = new List<PhysicsObject>();
        BoundingVolumeTree bvt;
        public void AddObject(PhysicsObject obj) {
            objects.Add(obj);
        }

        // This method will be called when the thread is started.
        float t = 0.0f;
        const float dt = 0.01f;
        double accumulator = 0.0;
        double prevTime = 0;

        public void RunSim(long elapsedMillis) {
            double newTime = elapsedMillis / 1000.0;
            double frameTime = newTime - prevTime;
            if (frameTime > .250) frameTime = .250;
            if (frameTime < 0) return;
            prevTime = newTime;
            accumulator += frameTime;
            while (accumulator >= dt) {
                AxisAlignedBoundingBox[] allObjects = new AxisAlignedBoundingBox[objects.Count];
                for (int i = 0; i < allObjects.Length; ++i) {
                    allObjects[i] = objects[i].getBounds();
                }
                bvt = new BoundingVolumeTree(allObjects);
                foreach (PhysicsObject obj in objects) {
                    obj.update(t, dt, bvt);
                }
                t += dt;
                accumulator -= dt;
            }
            float alpha = (float)(accumulator / dt);
            //Interpolate
            foreach (PhysicsObject obj in objects) {
                obj.lerp(alpha);
            }
        }

    }
    class PhysicsObject {
        internal State display;
        private State current;
        private State previous;
        internal AxisAlignedBoundingBox getBounds() {
            return current.orientedBound;
        }

        /// Update physics state.
        internal void update(float t, float dt, BoundingVolumeTree bvt) {
            previous = current;
            current.bvt = bvt;
            Derivative.integrate(ref current, t, dt);
        }

        internal void lerp(float blend) {
            display = State.Lerp(previous, current, blend);
        }

        internal PhysicsObject(float size, float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale, Vector3 angularMomentum, Mesh mesh) {
            previous = current = new State(size, mass, position, momentum, orientation, scale, angularMomentum, mesh);
        }

        /// Physics state.
        public struct State {
            /// primary physics state

            public Vector3 position { get; internal set; }                // the position of the center of mass in world coordinates (meters).
            internal Vector3 momentum;                // the momentum of the cube in kilogram meters per second.
            public Quaternion orientation { get; internal set; }        // the orientation of the cube represented by a unit quaternion.
            internal Vector3 angularMomentum;         // angular momentum vector.
            internal Vector3 scale;

            // secondary state
            internal Vector3 velocity;                // velocity in meters per second (calculated from momentum).
            internal Quaternion spin;                // quaternion rate of change in orientation.
            internal Vector3 angularVelocity;         // angular velocity (calculated from angularMomentum).

            /// constant state
            internal float mass;                     // mass of the cube in kilograms.
            internal float inverseMass;              // inverse of the mass used to convert momentum to velocity.
            internal float inertiaTensor;            // inertia tensor of the cube (i have simplified it to a single value due to the mass properties a cube).
            internal float inverseInertiaTensor;     // inverse inertia tensor used to convert angular momentum to angular velocity.
            internal AxisAlignedBoundingBox originalBound; // Original AABB created from mesh
            internal AxisAlignedBoundingBox orientedBound; //Cached rotated AABB
            internal BoundingVolumeTree bvt;           //Reference to the Bounding Volume Tree

            internal static State Lerp(State previous, State current, float blend) {
                State retVal = new State();
                retVal.position = Vector3.Lerp(previous.position, current.position, blend);
                retVal.orientation = Quaternion.Slerp(previous.orientation, current.orientation, blend);
                retVal.momentum = Vector3.Lerp(previous.momentum, current.momentum, blend);
                retVal.angularMomentum = Vector3.Lerp(previous.angularMomentum, current.angularMomentum, blend);
                return retVal;
            }
            /// Recalculate secondary state values from primary values.

            internal void recalculate() {
                velocity = momentum * inverseMass;
                angularVelocity = angularMomentum * inverseInertiaTensor;
                orientation.Normalize();
                spin = Quaternion.Multiply(new Quaternion(angularVelocity.X, angularVelocity.Y, angularVelocity.Z, 0), 0.5f) * orientation;
                AxisAlignedBoundingBox.Update(originalBound, orientation, position, scale, out orientedBound);
            }


            /// Default constructor.

            internal State(float size, float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale, Vector3 angularMomentum, Mesh mesh)
                : this() {
                this.mass = mass;
                this.inverseMass = 1.0f / mass;
                this.position = position;
                this.momentum = momentum;
                this.orientation = orientation;
                this.angularMomentum = angularMomentum;
                this.inertiaTensor = mass * size * size * 1.0f / 6.0f;
                this.inverseInertiaTensor = 1.0f / inertiaTensor;
                this.originalBound = new AxisAlignedBoundingBox(mesh.vertices);
                this.scale = scale;
                recalculate();
            }
        };

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
                state.recalculate();

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

                state.recalculate();
            }

            /// Calculate force and torque for physics state at time t.
            /// Due to the way that the RK4 integrator works we need to calculate
            /// force implicitly from state rather than explictly applying forces
            /// to the rigid body once per update. This is because the RK4 achieves
            /// its accuracy by detecting curvature in derivative values over the 
            /// timestep so we need our force values to supply the curvature.

            static void forces(State state, float t, out Vector3 force, out Vector3 torque) {

                collisionDetection(state, out force, out torque);
                //gravity(ref force);
                //damping(state, ref force, ref torque);
                //control(input, state, force, torque);
            }

            private static void collisionDetection(State state, out Vector3 force, out Vector3 torque) {
                force = Vector3.Zero;
                torque = Vector3.Zero;
                AxisAlignedBoundingBox[] collisionTargets = state.bvt.getCollisions(state.orientedBound);
                if (collisionTargets.Length > 0) {
                    Debug.WriteLine("Collisions: " + collisionTargets.Length);
                }
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
}
