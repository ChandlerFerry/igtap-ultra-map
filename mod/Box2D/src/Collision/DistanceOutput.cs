using System.Numerics;

namespace Box2D
{
    public struct b2DistanceOutput
    {
        public b2Vec2 pointA;

        public b2Vec2 pointB;

        public float distance;

        public int iterations;
    }
}