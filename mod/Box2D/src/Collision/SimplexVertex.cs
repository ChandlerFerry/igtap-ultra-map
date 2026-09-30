using System.Numerics;

namespace Box2D
{
    public struct b2SimplexVertex
    {
        public b2Vec2 wA;
        public b2Vec2 wB;
        public b2Vec2 w;
        public float a;
        public int indexA;
        public int indexB;
    }
}