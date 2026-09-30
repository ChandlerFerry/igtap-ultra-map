using System;
using System.Numerics;

namespace Box2D
{
    public class b2WeldJointDef : b2JointDef
    {
        public float damping;

        [Obsolete("Use b2Joint.AngularStiffness to get stiffness & damping values", true)]
        public float dampingRatio;

        [Obsolete("Use b2Joint.AngularStiffness to get stiffness & damping values", true)]
        public float frequencyHz;

        public b2Vec2 localAnchorA;
        public b2Vec2 localAnchorB;
        public float referenceAngle;

        public float stiffness;
    }
}