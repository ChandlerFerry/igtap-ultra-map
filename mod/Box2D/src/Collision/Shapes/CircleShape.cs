using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public class b2CircleShape : b2Shape
    {
        public const byte contactMatch = 0;
        public b2Vec2 m_p;

        public b2CircleShape()
        {
            m_radius = 0;
            m_p = b2Vec2.Zero;
        }

        public b2Vec2 Center
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_p;
            set => m_p = value;
        }

        public float Radius
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_radius;
            set => m_radius = value;
        }

        public override byte ContactMatch => contactMatch;

        public override b2Shape Clone() => (b2CircleShape)MemberwiseClone();

        public override int GetChildCount() => 1;

        public override bool TestPoint(in b2Transform transform, in b2Vec2 p)
        {
            b2Vec2 center = transform.p + b2Vec2.Transform(m_p, transform.q);
            b2Vec2 d = p - center;
            return b2Vec2.Dot(d, d) <= m_radius * m_radius;
        }

        public override bool RayCast(
            out b2RayCastOutput output,
            in b2RayCastInput input,
            in b2Transform transform,
            int childIndex)
        {
            output = default;

            b2Vec2 position = transform.p + b2Vec2.Transform(m_p, transform.q);
            b2Vec2 s = input.p1 - position;
            float b = b2Vec2.Dot(s, s) - m_radius * m_radius;

            b2Vec2 r = input.p2 - input.p1;
            float c = b2Vec2.Dot(s, r);
            float rr = b2Vec2.Dot(r, r);
            float sigma = c * c - rr * b;

            if (sigma < 0.0f || rr < b2Settings.FLT_EPSILON)
            {
                return false;
            }

            float a = -(c + MathF.Sqrt(sigma));

            if (0.0f <= a && a <= input.maxFraction * rr)
            {
                a /= rr;
                output.fraction = a;
                output.normal = b2Vec2.Normalize(s + a * r);
                return true;
            }

            return false;
        }

        public override void ComputeAABB(out b2AABB aabb, in b2Transform transform, int childIndex)
        {
            b2Vec2 p = transform.p + b2Vec2.Transform(m_p, transform.q);
            aabb.lowerBound = new b2Vec2(p.X - m_radius, p.Y - m_radius);
            aabb.upperBound = new b2Vec2(p.X + m_radius, p.Y + m_radius);
        }

        public override void ComputeMass(out b2MassData massData, float density)
        {
            massData.mass = density * b2Settings.Pi * m_radius * m_radius;
            massData.center = m_p;

            massData.I = massData.mass * (0.5f * m_radius * m_radius + b2Vec2.Dot(m_p, m_p));
        }

        public void Set(in b2Vec2 center, in float radius)
        {
            m_p = center;
            m_radius = radius;
        }
    }
}