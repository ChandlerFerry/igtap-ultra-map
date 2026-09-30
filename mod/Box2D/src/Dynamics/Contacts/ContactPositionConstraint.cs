using System.Numerics;

namespace Box2D
{
    public class ContactPositionConstraint
    {
        public int indexA;
        public int indexB;
        public float invIA;
        public float invIB;
        public float invMassA;
        public float invMassB;
        public b2Vec2 localCenterA;
        public b2Vec2 localCenterB;
        public b2Vec2 localNormal;
        public b2Vec2 localPoint;
        public b2Vec2[] localPoints = new b2Vec2[b2Settings.MaxManifoldPoints];
        public int pointCount;
        public float radiusA;
        public float radiusB;
        public b2ManifoldType type;
    }
}