namespace Box2D
{
    public class b2PolygonContact : b2Contact
    {
        private static readonly Collider<b2PolygonShape, b2PolygonShape> collider = new PolygonAndPolygonCollider();

        public b2PolygonContact(b2Fixture fA, int indexA, b2Fixture fB, int indexB) : base(fA, indexA, fB, indexB)
        { }

        public override void Evaluate(out b2Manifold manifold, in b2Transform xfA, in b2Transform xfB)
        {
            collider.Collide(out manifold, (b2PolygonShape)m_fixtureA.b2Shape, in xfA, (b2PolygonShape)m_fixtureB.b2Shape, in xfB);
        }
    }
}