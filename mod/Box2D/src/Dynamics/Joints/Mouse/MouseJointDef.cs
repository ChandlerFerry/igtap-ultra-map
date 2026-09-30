using System.Numerics;

namespace Box2D
{
    public class b2MouseJointDef : b2JointDef
    {
        public float DampingRatio;

        public float FrequencyHz;

        public float MaxForce;

        public b2Vec2 Target;

        public b2MouseJointDef()
        {
            FrequencyHz = 5.0f;
            DampingRatio = 0.7f;
        }
    }
}