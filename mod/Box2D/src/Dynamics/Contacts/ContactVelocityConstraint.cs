using System.Numerics;

namespace Box2D
{
    public class ContactVelocityConstraint
    {
        public int contactIndex;
        public float friction;
        public int indexA;
        public int indexB;
        public float invIA;
        public float invIB;
        public float invMassA;
        public float invMassB;
        public Matrix3x2 K;
        public b2Vec2 normal;
        public Matrix3x2 normalMass;
        public int pointCount;
        public VelocityConstraintPoint[] points = new VelocityConstraintPoint[b2Settings.MaxManifoldPoints];
        public float restitution;
        public float tangentSpeed;
    }
}