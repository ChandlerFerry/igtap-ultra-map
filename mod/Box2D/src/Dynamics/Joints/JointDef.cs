namespace Box2D
{
    public class b2JointDef
    {
        public b2Body bodyA;

        public b2Body bodyB;

        public bool collideConnected;

        public object UserData;

        public b2JointDef()
        {
            UserData = null;
            bodyA = null;
            bodyB = null;
            collideConnected = false;
        }
    }
}