using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Math = Box2D.Math;

namespace Box2D
{
    public class b2EdgeShape : b2Shape
    {
        public const byte contactMatch = 1;
        public bool m_oneSided;
        public b2Vec2? m_vertex0;
        public b2Vec2 m_vertex1;
        public b2Vec2 m_vertex2;
        public b2Vec2? m_vertex3;

        public b2EdgeShape() => m_radius = b2Settings.polygonRadius;

        public b2EdgeShape(b2Vec2 v1, b2Vec2 v2) : this()
        {
            SetTwoSided(v1, v2);
        }

        public b2Vec2 Vertex1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_vertex1;
        }

        public b2Vec2 Vertex2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_vertex2;
        }

        public override byte ContactMatch => contactMatch;

        public void SetOneSided(in b2Vec2 v0, in b2Vec2 v1, in b2Vec2 v2, in b2Vec2 v3)
        {
            m_vertex0 = v0;
            m_vertex1 = v1;
            m_vertex2 = v2;
            m_vertex3 = v3;
            m_oneSided = true;
        }

        public void SetTwoSided(in b2Vec2 v1, in b2Vec2 v2)
        {
            m_vertex1 = v1;
            m_vertex2 = v2;
            m_oneSided = false;
        }

        [Obsolete("Use SetTwoSided instead", true)]
        public void Set(in b2Vec2 v1, in b2Vec2 v2)
        {
            m_vertex1 = v1;
            m_vertex2 = v2;
        }

        public override b2Shape Clone() => (b2EdgeShape)MemberwiseClone();

        public override int GetChildCount() => 1;

        public override bool TestPoint(in b2Transform xf, in b2Vec2 p) => false;

        public override bool RayCast(
            out b2RayCastOutput output,
            in b2RayCastInput input,
            in b2Transform xf,
            int childIndex)
        {
            output = default;
            b2Vec2 p1 = Math.MulT(xf.q, input.p1 - xf.p);
            b2Vec2 p2 = Math.MulT(xf.q, input.p2 - xf.p);
            b2Vec2 d = p2 - p1;

            b2Vec2 v1 = m_vertex1;
            b2Vec2 v2 = m_vertex2;
            b2Vec2 e = v2 - v1;

            var normal = b2Vec2.Normalize(new b2Vec2(e.Y, -e.X));

            float numerator = b2Vec2.Dot(normal, v1 - p1);
            if (m_oneSided && numerator > 0.0f)
            {
                return false;
            }

            float denominator = b2Vec2.Dot(normal, d);

            if (denominator == 0.0f)
            {
                return false;
            }

            float t = numerator / denominator;
            if (t < 0.0f || input.maxFraction < t)
            {
                return false;
            }

            b2Vec2 q = p1 + t * d;

            b2Vec2 r = v2 - v1;
            float rr = b2Vec2.Dot(r, r);
            if (rr == 0.0f)
            {
                return false;
            }

            float s = b2Vec2.Dot(q - v1, r) / rr;
            if (s < 0.0f || 1.0f < s)
            {
                return false;
            }

            output.fraction = t;
            if (numerator > 0.0f)
            {
                output.normal = -b2Vec2.Transform(normal, xf.q);
            }
            else
            {
                output.normal = b2Vec2.Transform(normal, xf.q);
            }

            return true;
        }

        public override void ComputeAABB(out b2AABB aabb, in b2Transform xf, int childIndex)
        {
            b2Vec2 v1 = Math.Mul(xf, m_vertex1);
            b2Vec2 v2 = Math.Mul(xf, m_vertex2);

            var lower = b2Vec2.Min(v1, v2);
            var upper = b2Vec2.Max(v1, v2);

            var r = new b2Vec2(m_radius, m_radius);
            aabb.lowerBound = lower - r;
            aabb.upperBound = upper + r;
        }

        public override void ComputeMass(out b2MassData massData, float density)
        {
            massData.mass = 0.0f;
            massData.center = 0.5f * (m_vertex1 + m_vertex2);
            massData.I = 0.0f;
        }

        public void Set(b2Vec2 v0, b2Vec2 v1, b2Vec2 v2, b2Vec2 v3)
        {
            m_vertex0 = v0;
            m_vertex1 = v1;
            m_vertex2 = v2;
            m_vertex3 = v3;
        }
    }
}