using MathF = System.Math;
using Float = System.Double;

namespace Box2D
{
    public class b2GearJointDef : b2JointDef
    {
        public b2Joint Joint1;

        public b2Joint Joint2;

        public float Ratio;

        public b2GearJointDef() => Ratio = 1.0f;
    }
}