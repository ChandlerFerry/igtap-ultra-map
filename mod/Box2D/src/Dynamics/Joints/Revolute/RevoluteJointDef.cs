using System.Numerics;

namespace Box2D
{
    public class b2RevoluteJointDef : b2JointDef
    {
        public bool enableLimit;

        public bool enableMotor;

        public b2Vec2 localAnchorA;

        public b2Vec2 localAnchorB;

        public float lowerAngle;

        public float maxMotorTorque;

        public float motorSpeed;

        public float referenceAngle;

        public float upperAngle;

        public void Initialize(b2Body body1, b2Body body2, b2Vec2 anchor)
        {
            bodyA = body1;
            bodyB = body2;
            localAnchorA = body1.GetLocalPoint(anchor);
            localAnchorB = body2.GetLocalPoint(anchor);
            referenceAngle = body2.GetAngle() - body1.GetAngle();
        }
    }
}