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
        static public void collision(ref MovableObject a, ref MovableObject b, ContactInfo info) {
            Vector3d contactNormal = info.normal;
            Vector3d contactPoint1 = info.point1;
            Vector3d contactPoint2 = info.point2;
            Vector3d aContactOffset = contactPoint1 - new Vector3d(a.current.position.X, a.current.position.Y, a.current.position.Z); //Offset from center of mass to contact point
            Vector3d bContactOffset = contactPoint2 - new Vector3d(b.current.position.X, b.current.position.Y, b.current.position.Z);
            Vector3 v_r_f = a.current.velocity - b.current.velocity; //Relative velocity
            Vector3d v_r = new Vector3d(v_r_f.X, v_r_f.Y, v_r_f.Z);
            float relativeVelocityMagnitude = (float)Math.Sqrt(v_r.X * v_r.X + v_r.Y * v_r.Y + v_r.Z * v_r.Z);
            
            if (relativeVelocityMagnitude == 0f) relativeVelocityMagnitude = 0.00001f; //Prevent divide by 0 error

            Vector3d contactTangent = Vector3d.Cross(Vector3d.Cross(contactNormal, v_r), contactNormal) / relativeVelocityMagnitude;

            double normalImpulseMagnitude = calculateImpulseMagnitude(a, b, contactNormal, v_r, aContactOffset, bContactOffset);
            double frictionImpulseMagnitude = calculateFrictionImpulseMagnitude(a, b, contactTangent, v_r, aContactOffset, bContactOffset);
            Vector3d impulseVector = normalImpulseMagnitude * contactNormal + frictionImpulseMagnitude * contactTangent;
            Vector3 impulseFloatVector = new Vector3((float)impulseVector.X, (float)impulseVector.Y, (float)impulseVector.Z);
            //Vector3d offset = info.normal * info.penetrationDepth * .5;
            //Vector3 offsetFloat = new Vector3((float)offset.X, (float)offset.Y, (float)offset.Z);
            //a.current.position = a.current.position - offsetFloat;
            //b.current.position = b.current.position + offsetFloat;
            a.current.momentum = a.current.momentum - impulseFloatVector;
            b.current.momentum = b.current.momentum + impulseFloatVector;
            a.current.angularMomentum = a.current.angularMomentum - Vector3.Cross(new Vector3((float)aContactOffset.X, (float)aContactOffset.Y, (float)aContactOffset.Z), impulseFloatVector);
            b.current.angularMomentum = b.current.angularMomentum + Vector3.Cross(new Vector3((float)bContactOffset.X, (float)bContactOffset.Y, (float)bContactOffset.Z), impulseFloatVector);
        }

        static double calculateImpulseMagnitude(MovableObject a, MovableObject b, Vector3d contactNormal, Vector3d relativeVelocity, Vector3d aContactOffset, Vector3d bContactOffset) {
            double e = .5f;//coefficient of restitution
            double kn = calculateK(a, b, contactNormal, aContactOffset, bContactOffset);
            return Vector3d.Dot(-(1 + e) * relativeVelocity, contactNormal) / kn;
        }

        static double calculateFrictionImpulseMagnitude(MovableObject a, MovableObject b, Vector3d contactTangent, Vector3d relativeVelocity, Vector3d aContactOffset, Vector3d bContactOffset) {
            double frictionCoefficient = 0.3; //Coefficient of friction of normal to frictional force
            double kt = calculateK(a, b, contactTangent, aContactOffset, bContactOffset);
            return -(Vector3d.Dot(relativeVelocity, contactTangent) * (frictionCoefficient + 1) / kt);
        }

        static double calculateK(MovableObject a, MovableObject b, Vector3d contact, Vector3d aContactOffset, Vector3d bContactOffset) {
            Vector3d a_1 = Vector3d.Cross(a.current.inverseInertiaTensor * Vector3d.Cross(aContactOffset, contact), aContactOffset); //Angular velocity
            Vector3d a_2 = Vector3d.Cross(b.current.inverseInertiaTensor * Vector3d.Cross(bContactOffset, contact), bContactOffset);
            return a.current.inverseMass + b.current.inverseMass + Vector3d.Dot(a_1 + a_2, contact);
        }
    }
}
