using System;
using System.Numerics;

namespace Box2D
{
    public class b2DistanceJointDef : b2JointDef
    {
        public float damping;

        [Obsolete("Use stiffness and damping instead of frequencyHz and dampingRatio")]
        public float? dampingRatio;

        [Obsolete("Use stiffness and damping instead of frequencyHz and dampingRatio")]
        public float? frequencyHz;

        public float length;

        public b2Vec2 localAnchorA;

        public b2Vec2 localAnchorB;

        public float stiffness;

        public b2DistanceJointDef() => length = 1.0f;

        public void Initialize(b2Body bodyA, b2Body bodyB, b2Vec2 anchor1, b2Vec2 anchor2, float frequencyHz = 0f,
            float dampingRatio = 0f)
        {
            this.bodyA = bodyA;
            this.bodyB = bodyB;

            localAnchorA = bodyA.GetLocalPoint(anchor1);
            localAnchorB = bodyB.GetLocalPoint(anchor2);

            b2Vec2 d = anchor2 - anchor1;
            length = d.Length();

            b2Joint.LinearStiffness(out stiffness, out damping, frequencyHz, dampingRatio, bodyA, bodyB);
        }
    }
}