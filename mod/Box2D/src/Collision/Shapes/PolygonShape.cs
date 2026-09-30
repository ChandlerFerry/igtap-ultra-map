using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public class b2PolygonShape : b2Shape
    {
        public const byte contactMatch = 2;
        public b2Vec2 m_centroid;
        public int m_count;
        public b2Vec2[] m_normals = new b2Vec2[b2Settings.MaxPolygonVertices];
        public b2Vec2[] m_vertices = new b2Vec2[b2Settings.MaxPolygonVertices];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2PolygonShape()
        {
            m_radius = b2Settings.polygonRadius;
            m_count = 0;
            m_centroid = b2Vec2.Zero;
        }

        public b2PolygonShape(params b2Vec2[] vectors) : this()
        {
            Set(vectors);
        }

        public b2PolygonShape(float hX, float hY) : this()
        {
            SetAsBox(hX, hY);
        }

        public override byte ContactMatch => contactMatch;

        public override b2Shape Clone() => (b2Shape)MemberwiseClone();

        public void SetAsBox(float hx, float hy)
        {
            m_count = 4;
            m_vertices[0] = new b2Vec2(-hx, -hy);
            m_vertices[1] = new b2Vec2(hx, -hy);
            m_vertices[2] = new b2Vec2(hx, hy);
            m_vertices[3] = new b2Vec2(-hx, hy);

            m_normals[0] = new b2Vec2(0, -1);
            m_normals[1] = new b2Vec2(1, 0);
            m_normals[2] = new b2Vec2(0, 1);
            m_normals[3] = new b2Vec2(-1, 0);

            m_centroid = b2Vec2.Zero;
        }

        public void SetAsBox(float hx, float hy, in b2Vec2 center, float angle)
        {
            SetAsBox(hx, hy);
            m_centroid = center;

            var xf = new b2Transform();
            xf.p = center;
            xf.q = Matrex.CreateRotation(angle);

            for (var i = 0; i < m_count; i++)
            {
                m_vertices[i] = Math.Mul(xf, m_vertices[i]);
                m_normals[i] = b2Vec2.Transform(m_normals[i], xf.q);
            }
        }

        public override int GetChildCount() => 1;

        private static b2Vec2 ComputeCentroid(in b2Vec2[] vs, int count)
        {
            b2Vec2 c = b2Vec2.Zero;
            var area = 0.0f;

            b2Vec2 s = vs[0];

            const float inv3 = 1.0f / 3.0f;

            for (var i = 0; i < count; ++i)
            {
                b2Vec2 p1 = vs[0] - s;
                b2Vec2 p2 = vs[i] - s;
                b2Vec2 p3 = i + 1 < count ? vs[i + 1] - s : vs[0] - s;

                b2Vec2 e1 = p2 - p1;
                b2Vec2 e2 = p3 - p1;

                float D = Vectex.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                c += triangleArea * inv3 * (p1 + p2 + p3);
            }

            c = 1.0f / area * c + s;
            return c;
        }

        public bool Set(b2Vec2[] points, int count)
        {
            if (points == null || count < 3)
            {
                return false;
            }

            b2Vec2[] copy = count == points.Length ? points : new b2Vec2[count];
            if (copy != points)
            {
                System.Array.Copy(points, copy, count);
            }

            Set(in copy);
            return m_count >= 3 && !float.IsNaN(m_centroid.x) && !float.IsNaN(m_centroid.y);
        }

        public void Set(in b2Vec2[] vertices)
        {
            int count = vertices.Length;
            if (count < 3)
            {
                SetAsBox(1f, 1f);
                return;
            }

            int n = Math.Min(count, b2Settings.MaxPolygonVertices);
            var ps = new b2Vec2[b2Settings.MaxPolygonVertices];
            var tempCount = 0;
            for (var i = 0; i < n; ++i)
            {
                b2Vec2 v = vertices[i];

                var unique = true;
                for (var j = 0; j < tempCount; ++j)
                {
                    if (b2Vec2.DistanceSquared(v, ps[j]) < 0.5f * b2Settings.linearSlop * (0.5f * b2Settings.linearSlop))
                    {
                        unique = false;
                        break;
                    }
                }

                if (unique)
                {
                    ps[tempCount++] = v;
                }
            }

            n = tempCount;
            if (n < 3)
            {
                SetAsBox(1.0f, 1.0f);
                return;
            }

            var i0 = 0;
            float x0 = ps[0].X;
            for (var i = 1; i < n; ++i)
            {
                float x = ps[i].X;
                if (x > x0 || x == x0 && ps[i].Y < ps[i0].Y)
                {
                    i0 = i;
                    x0 = x;
                }
            }

            var hull = new int[b2Settings.MaxPolygonVertices];
            var m = 0;
            int ih = i0;

            for (; ; )
            {
                hull[m] = ih;

                var ie = 0;
                for (var j = 1; j < n; ++j)
                {
                    if (ie == ih)
                    {
                        ie = j;
                        continue;
                    }

                    b2Vec2 r = ps[ie] - ps[hull[m]];
                    b2Vec2 v = ps[j] - ps[hull[m]];
                    float c = Vectex.Cross(r, v);
                    if (c < 0.0f)
                    {
                        ie = j;
                    }

                    if (c == 0.0f && v.LengthSquared() > r.LengthSquared())
                    {
                        ie = j;
                    }
                }

                ++m;
                ih = ie;

                if (ie == i0)
                {
                    break;
                }
            }

            if (m < 3)
            {
                SetAsBox(1.0f, 1.0f);
                return;
            }

            m_count = m;

            for (var i = 0; i < m; ++i)
            {
                m_vertices[i] = ps[hull[i]];
            }

            for (var i = 0; i < m; ++i)
            {
                int i1 = i;
                int i2 = i + 1 < m ? i + 1 : 0;
                b2Vec2 edge = m_vertices[i2] - m_vertices[i1];
                m_normals[i] = b2Vec2.Normalize(Vectex.Cross(edge, 1.0f));
            }

            m_centroid = ComputeCentroid(m_vertices, m);
        }

        public override bool TestPoint(in b2Transform xf, in b2Vec2 p)
        {
            b2Vec2 pLocal = Math.MulT(xf.q, p - xf.p);

            for (var i = 0; i < m_count; ++i)
            {
                float dot = b2Vec2.Dot(m_normals[i], pLocal - m_vertices[i]);
                if (dot > 0.0f)
                {
                    return false;
                }
            }

            return true;
        }

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

            float lower = 0.0f, upper = input.maxFraction;

            int index = -1;

            for (var i = 0; i < m_count; ++i)
            {
                float numerator = b2Vec2.Dot(m_normals[i], m_vertices[i] - p1);
                float denominator = b2Vec2.Dot(m_normals[i], d);

                if (denominator == 0.0f)
                {
                    if (numerator < 0.0f)
                    {
                        return false;
                    }
                }
                else
                {
                    if (denominator < 0.0f && numerator < lower * denominator)
                    {
                        lower = numerator / denominator;
                        index = i;
                    }
                    else if (denominator > 0.0f && numerator < upper * denominator)
                    {
                        upper = numerator / denominator;
                    }
                }

                if (upper < lower)
                {
                    return false;
                }
            }

            if (index >= 0)
            {
                output.fraction = lower;
                output.normal = b2Vec2.Transform(m_normals[index], xf.q);
                return true;
            }

            return false;
        }

        public override void ComputeAABB(out b2AABB aabb, in b2Transform xf, int childIndex)
        {
            b2Vec2 lower = Math.Mul(xf, m_vertices[0]);
            b2Vec2 upper = lower;

            for (var i = 1; i < m_count; ++i)
            {
                b2Vec2 v = Math.Mul(xf, m_vertices[i]);
                lower = b2Vec2.Min(lower, v);
                upper = b2Vec2.Max(upper, v);
            }

            var r = new b2Vec2(m_radius, m_radius);
            aabb.lowerBound = lower - r;
            aabb.upperBound = upper + r;
        }

        public override void ComputeMass(out b2MassData massData, float density)
        {
            b2Vec2 center = b2Vec2.Zero;
            var area = 0.0f;
            var I = 0.0f;

            b2Vec2 s = m_vertices[0];

            const float k_inv3 = 1.0f / 3.0f;

            for (var i = 0; i < m_count; ++i)
            {
                b2Vec2 e1 = m_vertices[i] - s;
                b2Vec2 e2 = i + 1 < m_count ? m_vertices[i + 1] - s : m_vertices[0] - s;

                float D = Vectex.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                center += triangleArea * k_inv3 * (e1 + e2);

                float ex1 = e1.X, ey1 = e1.Y;
                float ex2 = e2.X, ey2 = e2.Y;

                float intx2 = ex1 * ex1 + ex2 * ex1 + ex2 * ex2;
                float inty2 = ey1 * ey1 + ey2 * ey1 + ey2 * ey2;

                I += 0.25f * k_inv3 * D * (intx2 + inty2);
            }

            massData.mass = density * area;

            center *= 1.0f / area;
            massData.center = center + s;

            massData.I = density * I;

            massData.I += massData.mass * (b2Vec2.Dot(massData.center, massData.center) - b2Vec2.Dot(center, center));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Vec2[] GetVertices() => m_vertices[..m_count];
    }
}