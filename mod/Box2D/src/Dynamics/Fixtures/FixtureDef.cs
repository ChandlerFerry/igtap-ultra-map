namespace Box2D
{
    public class b2FixtureDef
    {
        public float density;

        public b2Filter filter = new b2Filter();

        public float friction = 0.2f;

        public bool isSensor;

        public float restitution;

        public b2Shape shape;

        public object userData;
    }
}