using System.Numerics;

namespace Box2D
{
    public class b2PulleyJointDef : b2JointDef
    {
        public b2Vec2 GroundAnchorA;

        public b2Vec2 GroundAnchorB;

        public float LengthA;

        public float LengthB;

        public b2Vec2 LocalAnchorA;

        public b2Vec2 LocalAnchorB;

        public float MaxLength1;

        public float MaxLength2;

        public float Ratio;

        public b2PulleyJointDef()
        {
            GroundAnchorA = new b2Vec2(-1.0f, 1.0f);
            GroundAnchorB = new b2Vec2(1.0f, 1.0f);
            LocalAnchorA = new b2Vec2(-1.0f, 0.0f);
            LocalAnchorB = new b2Vec2(1.0f, 0.0f);
            Ratio = 1.0f;
            collideConnected = true;
        }

        public void Initialize(
            b2Body body1,
            b2Body body2,
            b2Vec2 groundAnchor1,
            b2Vec2 groundAnchor2,
            b2Vec2 anchor1,
            b2Vec2 anchor2,
            float ratio)
        {
            bodyA = body1;
            bodyB = body2;
            GroundAnchorA = groundAnchor1;
            GroundAnchorB = groundAnchor2;
            LocalAnchorA = body1.GetLocalPoint(anchor1);
            LocalAnchorB = body2.GetLocalPoint(anchor2);
            b2Vec2 dA = anchor1 - groundAnchor1;
            LengthA = dA.Length();
            b2Vec2 dB = anchor2 - groundAnchor2;
            LengthB = dB.Length();
            Ratio = ratio;
        }
    }
}