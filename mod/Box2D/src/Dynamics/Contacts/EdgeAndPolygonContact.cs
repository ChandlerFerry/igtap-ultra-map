namespace Box2D
{
    public class b2EdgeAndPolygonContact : b2Contact
    {
        private static readonly Collider<b2EdgeShape, b2PolygonShape> collider = new EdgeAndPolygonCollider();

        private readonly b2EdgeShape edgeA;
        private readonly b2PolygonShape polygonB;

        public b2EdgeAndPolygonContact(b2Fixture fA, int indexA, b2Fixture fB, int indexB) : base(fA, indexA, fB, indexB)
        {
            edgeA = (b2EdgeShape)m_fixtureA.b2Shape;
            polygonB = (b2PolygonShape)m_fixtureB.b2Shape;
        }

        public override void Evaluate(out b2Manifold manifold, in b2Transform xfA, in b2Transform xfB)
        {
            collider.Collide(out manifold, edgeA, in xfA, polygonB, in xfB);
        }
    }
}