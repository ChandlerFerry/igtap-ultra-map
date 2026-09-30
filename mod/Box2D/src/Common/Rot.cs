using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public struct b2Rot
    {
        public float s;

        public float c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Rot(float angle)
        {
            s = MathF.Sin(angle);
            c = MathF.Cos(angle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(float angle)
        {
            s = MathF.Sin(angle);
            c = MathF.Cos(angle);
        }

        private void SetIdentity()
        {
            s = 0.0f;
            c = 1.0f;
        }

        private float GetAngle() => MathF.Atan2(s, c);

        private b2Vec2 GetXAxis() => new b2Vec2(c, s);

        private b2Vec2 GetYAxis() => new b2Vec2(-s, c);
    }
}