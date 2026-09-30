using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public struct b2Simplex
    {
        public b2Array3<b2SimplexVertex> m_v;

        public int m_count;

        private b2SimplexVertex m_v1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_v[0];
        }

        private b2SimplexVertex m_v2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_v[1];
        }

        private b2SimplexVertex m_v3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_v[2];
        }

        public void ReadCache(
            in b2SimplexCache cache,
            in b2DistanceProxy proxyA,
            in b2Transform transformA,
            in b2DistanceProxy proxyB,
            in b2Transform transformB)
        {
            m_count = cache.count;
            ref b2Array3<b2SimplexVertex> vertices = ref m_v;
            for (var i = 0; i < m_count; ++i)
            {
                ref b2SimplexVertex v = ref vertices[i];
                v.indexA = cache.indexA[i];
                v.indexB = cache.indexB[i];
                b2Vec2 wALocal = proxyA.GetVertex(v.indexA);
                b2Vec2 wBLocal = proxyB.GetVertex(v.indexB);
                v.wA = Math.Mul(transformA, wALocal);
                v.wB = Math.Mul(transformB, wBLocal);
                v.w = v.wB - v.wA;
                v.a = 0.0f;
            }

            if (m_count > 1)
            {
                float metric1 = cache.metric;
                float metric2 = GetMetric();
                if (metric2 < 0.5f * metric1 || 2.0f * metric1 < metric2 || metric2 < b2Settings.FLT_EPSILON)
                {
                    m_count = 0;
                }
            }

            if (m_count == 0)
            {
                ref b2SimplexVertex v = ref vertices[0];
                v.indexA = 0;
                v.indexB = 0;
                b2Vec2 wALocal = proxyA.GetVertex(0);
                b2Vec2 wBLocal = proxyB.GetVertex(0);
                v.wA = Math.Mul(transformA, wALocal);
                v.wB = Math.Mul(transformB, wBLocal);
                v.w = v.wB - v.wA;
                v.a = 1.0f;
                m_count = 1;
            }
        }

        public void WriteCache(ref b2SimplexCache cache)
        {
            cache.metric = GetMetric();
            cache.count = (ushort)m_count;
            ref b2Array3<b2SimplexVertex> vertices = ref m_v;
            for (var i = 0; i < m_count; ++i)
            {
                cache.indexA[i] = (byte)vertices[i].indexA;
                cache.indexB[i] = (byte)vertices[i].indexB;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Vec2 GetSearchDirection()
        {
            switch (m_count)
            {
                case 1:
                    return -m_v1.w;

                case 2:
                    {
                        b2Vec2 e12 = m_v2.w - m_v1.w;
                        float sgn = Vectex.Cross(e12, -m_v1.w);

                        return sgn > 0.0f ? Vectex.Cross(1.0f, e12) : Vectex.Cross(e12, 1.0f);
                    }

                default:
                    return b2Vec2.Zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Vec2 GetClosestPoint()
        {
            switch (m_count)
            {
                case 0:
                    return b2Vec2.Zero;
                case 1:
                    return m_v1.w;
                case 2:
                    return m_v1.a * m_v1.w + m_v2.a * m_v2.w;
                case 3:
                    return b2Vec2.Zero;
                default:
                    return b2Vec2.Zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetWitnessPoints(out b2Vec2 pA, out b2Vec2 pB)
        {
            switch (m_count)
            {
                case 1:
                    pA = m_v1.wA;
                    pB = m_v1.wB;
                    break;

                case 2:
                    pA = m_v1.a * m_v1.wA + m_v2.a * m_v2.wA;
                    pB = m_v1.a * m_v1.wB + m_v2.a * m_v2.wB;
                    break;

                case 3:
                    pA = m_v1.a * m_v1.wA + m_v2.a * m_v2.wA + m_v3.a * m_v3.wA;
                    pB = pA;
                    break;
                case 0:
                default:
                    pA = default;
                    pB = default;
                    break;
            }
        }

        private float GetMetric()
        {
            switch (m_count)
            {
                case 1:
                    return 0.0f;

                case 2:
                    return b2Vec2.Distance(m_v1.w, m_v2.w);

                case 3:
                    return Vectex.Cross(m_v2.w - m_v1.w, m_v3.w - m_v1.w);
                case 0:
                default:
                    return 0.0f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Solve2()
        {
            b2Vec2 w1 = m_v1.w;
            b2Vec2 w2 = m_v2.w;
            b2Vec2 e12 = w2 - w1;

            float d12_2 = -b2Vec2.Dot(w1, e12);
            if (d12_2 <= 0.0f)
            {
                m_v[0].a = 1.0f;
                m_count = 1;
                return;
            }

            float d12_1 = b2Vec2.Dot(w2, e12);
            if (d12_1 <= 0.0f)
            {
                m_v[1].a = 1.0f;
                m_count = 1;
                m_v[0] = m_v[1];
                return;
            }

            float inv_d12 = 1.0f / (d12_1 + d12_2);
            m_v[0].a = d12_1 * inv_d12;
            m_v[1].a = d12_2 * inv_d12;
            m_count = 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Solve3()
        {
            b2Vec2 w1 = m_v1.w;
            b2Vec2 w2 = m_v2.w;
            b2Vec2 w3 = m_v3.w;

            b2Vec2 e12 = w2 - w1;
            float w1e12 = b2Vec2.Dot(w1, e12);
            float w2e12 = b2Vec2.Dot(w2, e12);
            float d12_1 = w2e12;
            float d12_2 = -w1e12;

            b2Vec2 e13 = w3 - w1;
            float w1e13 = b2Vec2.Dot(w1, e13);
            float w3e13 = b2Vec2.Dot(w3, e13);
            float d13_1 = w3e13;
            float d13_2 = -w1e13;

            b2Vec2 e23 = w3 - w2;
            float w2e23 = b2Vec2.Dot(w2, e23);
            float w3e23 = b2Vec2.Dot(w3, e23);
            float d23_1 = w3e23;
            float d23_2 = -w2e23;

            float n123 = Vectex.Cross(e12, e13);

            float d123_1 = n123 * Vectex.Cross(w2, w3);
            float d123_2 = n123 * Vectex.Cross(w3, w1);
            float d123_3 = n123 * Vectex.Cross(w1, w2);

            if (d12_2 <= 0.0f && d13_2 <= 0.0f)
            {
                m_v[0].a = 1.0f;
                m_count = 1;
                return;
            }

            if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
            {
                float inv_d12 = 1.0f / (d12_1 + d12_2);
                m_v[0].a = d12_1 * inv_d12;
                m_v[1].a = d12_1 * inv_d12;
                m_count = 2;
                return;
            }

            if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
            {
                float inv_d13 = 1.0f / (d13_1 + d13_2);
                m_v[0].a = d13_1 * inv_d13;
                m_v[2].a = d13_2 * inv_d13;
                m_count = 2;
                m_v[1] = m_v[2];
                return;
            }

            if (d12_1 <= 0.0f && d23_2 <= 0.0f)
            {
                m_v[1].a = 1.0f;
                m_count = 1;
                m_v[0] = m_v[1];
                return;
            }

            if (d13_1 <= 0.0f && d23_1 <= 0.0f)
            {
                m_v[2].a = 1.0f;
                m_count = 1;
                m_v[0] = m_v[2];
                return;
            }

            if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
            {
                float inv_d23 = 1.0f / (d23_1 + d23_2);
                m_v[1].a = d23_1 * inv_d23;
                m_v[2].a = d23_2 * inv_d23;
                m_count = 2;
                m_v[0] = m_v[2];
                return;
            }

            float inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
            m_v[0].a = d123_1 * inv_d123;
            m_v[1].a = d123_2 * inv_d123;
            m_v[2].a = d123_3 * inv_d123;
            m_count = 3;
        }
    }
}