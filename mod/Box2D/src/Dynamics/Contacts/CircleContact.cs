using System.Numerics;

namespace Box2D
{
    public class b2CircleContact : b2Contact
    {
        private readonly b2CircleShape circleA;

        private readonly b2CircleShape circleB;

        public b2CircleContact(b2Fixture fA, int indexA, b2Fixture fB, int indexB) : base(fA, indexA, fB, indexB)
        {
            circleB = (b2CircleShape)m_fixtureB.b2Shape;
            circleA = (b2CircleShape)m_fixtureA.b2Shape;
        }

        public override void Evaluate(out b2Manifold manifold, in b2Transform xfA, in b2Transform xfB)
        {
            manifold = new b2Manifold();

            b2Vec2 pA = Math.Mul(xfA, circleA.m_p);
            b2Vec2 pB = Math.Mul(xfB, circleB.m_p);

            b2Vec2 d = pB - pA;
            float distSqr = b2Vec2.Dot(d, d);
            float rA = circleA.m_radius, rB = circleB.m_radius;
            float radius = rA + rB;
            if (distSqr > radius * radius)
            {
                return;
            }

            manifold.type = b2ManifoldType.Circles;
            manifold.localPoint = circleA.m_p;
            manifold.localNormal = b2Vec2.Zero;
            manifold.pointCount = 1;

            manifold.points[0] = new b2ManifoldPoint();
            manifold.points[0].localPoint = circleB.m_p;
            manifold.points[0].id.key = 0;
        }
    }
}