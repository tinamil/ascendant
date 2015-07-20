using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics.lighting;
using Ascendant.Graphics.objects;

namespace Ascendant.Physics {
    public abstract class IRigidBody : DisplayObject {

        public abstract BulletSharp.RigidBody rigidBody { get; protected set; }

        public abstract BulletSharp.TypedConstraint constraint { get; protected set; }

        public abstract IEnumerable<IRigidBody> RChildren { get; protected set; }

        public IRigidBody(int matNumber, List<Lighting.PointLight> lightList, Mesh mesh)
            : base(matNumber, lightList, mesh) {
            //Pass through to the base constructor
        }
    }
}
