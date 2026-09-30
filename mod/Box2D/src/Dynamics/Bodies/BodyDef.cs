using System.Numerics;

namespace Box2D
{
    public class b2BodyDef
    {
        public bool allowSleep;

        public float angle;

        public float angularDamping;

        public float angularVelocity;

        public bool awake;

        public bool bullet;

        public bool enabled;

        public bool fixedRotation;

        public float gravityScale;

        public float linearDamping;

        public b2Vec2 linearVelocity;

        public b2Vec2 position;

        public b2BodyType type;

        public object userData;

        public b2BodyDef()
        {
            userData = null;
            position = b2Vec2.Zero;
            angle = 0.0f;
            linearVelocity = b2Vec2.Zero;
            angularVelocity = 0.0f;
            linearDamping = 0.0f;
            angularDamping = 0.0f;
            allowSleep = true;
            awake = true;
            fixedRotation = false;
            bullet = false;
            type = b2BodyType.Static;
            enabled = true;
            gravityScale = 1.0f;
        }
    }
}