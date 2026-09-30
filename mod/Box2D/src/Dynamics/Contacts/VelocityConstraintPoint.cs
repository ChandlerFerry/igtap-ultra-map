using System.Numerics;

namespace Box2D
{
    public class VelocityConstraintPoint
    {
        public float normalImpulse;
        public float normalMass;
        public b2Vec2 rA;
        public b2Vec2 rB;
        public float tangentImpulse;
        public float tangentMass;
        public float velocityBias;
    }
}