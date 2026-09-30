using System.Numerics;

namespace Box2D
{
    public abstract class b2Shape
    {
        public float m_radius;
        public abstract byte ContactMatch { get; }

        public abstract b2Shape Clone();
        public abstract int GetChildCount();
        public abstract bool TestPoint(in b2Transform xf, in b2Vec2 p);

        public abstract bool RayCast(
            out b2RayCastOutput output,
            in b2RayCastInput input,
            in b2Transform transform,
            int childIndex);

        public abstract void ComputeAABB(out b2AABB aabb, in b2Transform xf, int childIndex);

        public abstract void ComputeMass(out b2MassData massData, float density);
    }
}