using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public struct b2Sweep
    {
        public b2Vec2 localCenter;
        public b2Vec2 c0, c;
        public float a0, a;
        public float alpha0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetTransform(out b2Transform xf, in float beta)
        {
            xf.p = (1.0f - beta) * c0 + beta * c;
            float angle = (1.0f - beta) * a0 + beta * a;
            xf.q = Matrex.CreateRotation(angle);
            xf.p -= b2Vec2.Transform(localCenter, xf.q);
        }

        public void Advance(float alpha)
        {
            float beta = (alpha - alpha0) / (1.0f - alpha0);
            c0 += beta * (c - c0);
            a0 += beta * (a - a0);
            alpha0 = alpha;
        }

        public void Normalize()
        {
            float d = b2Settings.Tau * MathF.Floor(a0 / b2Settings.Tau);
            a0 -= d;
            a -= d;
        }
    }
}