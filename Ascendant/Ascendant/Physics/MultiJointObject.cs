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
    class MultiJointObject : MovableObject {

        internal MultiJointObject(World world, int matNumber, Lighting.PointLight light, List<MovableObject> children,
            float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale,
            Vector3 angularMomentum, Mesh mesh)
            : base(world, matNumber, light,children, mass, position, momentum, orientation, scale, angularMomentum, mesh) {
            
           
        }
    }
}
