using System.Numerics;

namespace Box2D
{
    public struct b2RayCastInput
    {
        public b2Vec2 p1, p2;
        public float maxFraction;
    }
}