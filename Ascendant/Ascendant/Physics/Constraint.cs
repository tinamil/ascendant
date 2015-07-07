using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Ascendant.Physics {
   
        //btScalar _swingSpan1,btScalar _swingSpan2,btScalar _twistSpan,btScalar _softness = 1.f, btScalar _biasFactor = 0.3f, btScalar _relaxationFactor = 1.0f
        public struct ConeTwist {
            //Radians, 0 to 2pi
            public float swingSpan1, swingSpan2, twist, softness, bias, relaxation;
            public Matrix4 aFrame, bFrame;
        }
}
