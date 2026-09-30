using System.Numerics;

namespace Box2D
{
    public class b2ManifoldPoint
    {
        public b2ContactID id;

        public b2Vec2 localPoint;

        public float normalImpulse;

        public float tangentImpulse;
    }
}