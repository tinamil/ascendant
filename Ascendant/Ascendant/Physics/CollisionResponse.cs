using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;

namespace Ascendant.Physics {
    static class CollisionResponse {
        /**
         * contactNormal is the vector of separation
         * contactPoint is the coordinate at which contact occurred
         * 
         */
        static public void collision(ref MovableObject a, ref MovableObject b, Vector3 contactNormal, Vector3 contactPoint) {
            Vector3 aContactOffset = contactPoint - a.current.position; //Offset from center of mass to contact point
            Vector3 bContactOffset = contactPoint - b.current.position;
            Vector3 v_r = a.current.velocity - b.current.velocity; //Relative velocity
            float relativeVelocityMagnitude = (float)Math.Sqrt(v_r.X * v_r.X + v_r.Y * v_r.Y + v_r.Z * v_r.Z);
            
            if (relativeVelocityMagnitude == 0f) relativeVelocityMagnitude = 0.00001f; //Prevent divide by 0 error

            Vector3 contactTangent = Vector3.Cross(Vector3.Cross(contactNormal, v_r), contactNormal) / relativeVelocityMagnitude;

            float normalImpulseMagnitude = calculateImpulseMagnitude(a, b, contactNormal, contactPoint, v_r, aContactOffset, bContactOffset);
            float frictionImpulseMagnitude = calculateFrictionImpulseMagnitude(a, b, contactTangent, v_r, aContactOffset, bContactOffset);
            Vector3 impulseVector = normalImpulseMagnitude * contactNormal + frictionImpulseMagnitude * contactTangent;

            a.current.momentum = a.current.momentum - impulseVector;
            b.current.momentum = b.current.momentum + impulseVector;
            a.current.angularMomentum = a.current.angularMomentum - Vector3.Cross(aContactOffset, impulseVector);
            b.current.angularMomentum = b.current.angularMomentum + Vector3.Cross(bContactOffset, impulseVector);
        }

        static float calculateImpulseMagnitude(MovableObject a, MovableObject b, Vector3 contactNormal, Vector3 contactPoint,
            Vector3 relativeVelocity, Vector3 aContactOffset, Vector3 bContactOffset) {
            float e = .5f;//coefficient of restitution
            float kn = calculateK(a, b, contactNormal, aContactOffset, bContactOffset);
            return Vector3.Dot(-(1 + e) * relativeVelocity, contactNormal) / kn;
        }

        static float calculateFrictionImpulseMagnitude(MovableObject a, MovableObject b, Vector3 contactTangent, Vector3 relativeVelocity, Vector3 aContactOffset, Vector3 bContactOffset) {
            float frictionCoefficient = 0.3f; //Coefficient of friction of normal to frictional force
            float kt = calculateK(a, b, contactTangent, aContactOffset, bContactOffset);
            return -(Vector3.Dot(relativeVelocity, contactTangent) * (frictionCoefficient + 1) / kt);
        }

        static float calculateK(MovableObject a, MovableObject b, Vector3 contact, Vector3 aContactOffset, Vector3 bContactOffset) {
            Vector3 a_1 = Vector3.Cross(a.current.inverseInertiaTensor * Vector3.Cross(aContactOffset, contact), aContactOffset); //Angular velocity
            Vector3 a_2 = Vector3.Cross(b.current.inverseInertiaTensor * Vector3.Cross(bContactOffset, contact), bContactOffset);
            return a.current.inverseMass + b.current.inverseMass + Vector3.Dot(a_1 + a_2, contact);
        }
    }
}
