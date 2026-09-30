namespace Box2D
{
    public abstract class b2ContactListener
    {
        public abstract void BeginContact(in b2Contact contact);

        public abstract void EndContact(in b2Contact contact);

        public abstract void PreSolve(in b2Contact contact, in b2Manifold oldManifold);

        public abstract void PostSolve(in b2Contact contact, in b2ContactImpulse impulse);
    }
}