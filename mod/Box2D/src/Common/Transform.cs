using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public struct b2Transform
    {
        public b2Vec2 p;

        public Matrix3x2 q;

        public b2Transform(b2Vec2 position, Matrix3x2 rotation)
        {
            p = position;
            q = rotation;
        }

        public void SetIdentity()
        {
            p = b2Vec2.Zero;
            q = Matrix3x2.Identity;
        }

        public void Set(b2Vec2 p, float angle)
        {
            this.p = p;
            q = Matrex.CreateRotation(angle);
        }

        public float GetAngle() =>
            MathF.Atan2(q.M21, q.M11);

        public static b2Transform Identity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new b2Transform(b2Vec2.Zero, Matrix3x2.Identity);
        }
    }
}