using System.Numerics;

namespace Box2D
{
    public class b2PrismaticJointDef : b2JointDef
    {
        public bool enableLimit;

        public bool enableMotor;

        public b2Vec2 localAnchorA;

        public b2Vec2 localAnchorB;

        public b2Vec2 localAxisA;

        public float lowerTranslation;

        public float maxMotorForce;

        public float motorSpeed;

        public float referenceAngle;

        public float upperTranslation;

        public b2PrismaticJointDef() => localAxisA = new b2Vec2(1.0f, 0.0f);

        public void Initialize(b2Body body1, b2Body body2, b2Vec2 anchor, b2Vec2 axis)
        {
            bodyA = body1;
            bodyB = body2;
            localAnchorA = body1.GetLocalPoint(anchor);
            localAnchorB = body2.GetLocalPoint(anchor);
            localAxisA = body1.GetLocalVector(axis);
            referenceAngle = body2.GetAngle() - body1.GetAngle();
        }
    }
}