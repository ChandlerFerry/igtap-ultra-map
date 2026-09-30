using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D
{
    public static partial class Math
    {
        public const ushort USHRT_MAX = ushort.MaxValue;
        public const byte UCHAR_MAX = byte.MaxValue;
        public const int RAND_LIMIT = 32767;

        private static readonly Random s_rnd = new Random();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(float x) =>
            !(float.IsNaN(x) || float.IsNegativeInfinity(x) || float.IsPositiveInfinity(x));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InvSqrt(float x)
        {
            Convert convert = default;
            convert.x = x;
            float xhalf = 0.5f * x;
            convert.i = 0x5f3759df - (convert.i >> 1);
            x = convert.x;
            x = x * (1.5f - xhalf * x * x);
            return x;
        }

        [Obsolete("Use MathF.Sqrt", true)]
        public static float Sqrt(float x) => MathF.Sqrt(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Random()
        {
            float r = s_rnd.Next() & RAND_LIMIT;
            r /= RAND_LIMIT;
            r = 2.0f * r - 1.0f;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Random(float lo, float hi)
        {
            float r = s_rnd.Next() & RAND_LIMIT;
            r /= RAND_LIMIT;
            r = (hi - lo) * r + lo;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint NextPowerOfTwo(uint x)
        {
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(uint x)
        {
            bool result = x > 0 && (x & (x - 1)) == 0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Vec2 MulT(Matrix3x2 A, b2Vec2 v)
        {
            b2Vec2 ex = new b2Vec2(A.M11, A.M12);
            b2Vec2 ey = new b2Vec2(A.M21, A.M22);
            return new b2Vec2(b2Vec2.Dot(v, ex), b2Vec2.Dot(v, ey));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Mul(Matrix3x2 A, Matrix3x2 B)
        {
            Matrix3x2 C;
            C.M11 = A.M11 * B.M11 + A.M21 * B.M12;
            C.M12 = A.M12 * B.M11 + A.M22 * B.M12;
            C.M21 = A.M11 * B.M21 + A.M21 * B.M22;
            C.M22 = A.M12 * B.M21 + A.M22 * B.M22;
            C.M31 = 0.0f;
            C.M32 = 0.0f;
            return C;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 MulT(Matrix3x2 A, Matrix3x2 B)
        {
            Matrix3x2 C;
            C.M11 = A.M11 * B.M11 + A.M12 * B.M12;
            C.M12 = A.M21 * B.M11 + A.M22 * B.M12;
            C.M21 = A.M11 * B.M21 + A.M12 * B.M22;
            C.M22 = A.M21 * B.M21 + A.M22 * B.M22;
            C.M31 = 0.0f;
            C.M32 = 0.0f;
            return C;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Vec2 Mul(b2Transform T, b2Vec2 v) =>
            new b2Vec2((T.q.M11 * v.x - T.q.M12 * v.y) + T.p.x, (T.q.M12 * v.x + T.q.M11 * v.y) + T.p.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Vec2 MulT(b2Transform T, b2Vec2 v) => MulT(T.q, v - T.p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Mul(Mat33 A, Vector3 v) => v.X * A.ex + v.Y * A.ey + v.Z * A.ez;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Vec2 Mul(b2Rot q, b2Vec2 v) => new b2Vec2(q.c * v.X - q.s * v.Y, q.s * v.X + q.c * v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Vec2 MulT(b2Rot q, b2Vec2 v) => new b2Vec2(q.c * v.X + q.s * v.Y, -q.s * v.X + q.c * v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Transform Mul(in b2Transform A, in b2Transform B)
        {
            b2Transform C;
            C.q = Mul(A.q, B.q);
            C.p = A.p + b2Vec2.Transform(B.p, A.q);
            return C;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Transform MulT(in b2Transform A, in b2Transform B)
        {
            b2Transform C;
            C.q = MulT(A.q, B.q);
            C.p = MulT(A.q, B.p - A.p);
            return C;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2Vec2 Mul22(in Mat33 A, in b2Vec2 v) =>
            new b2Vec2(A.ex.X * v.X + A.ey.X * v.Y, A.ex.Y * v.X + A.ey.Y * v.Y);

        [StructLayout(LayoutKind.Explicit)]
        public struct Convert
        {
            [FieldOffset(0)]
            public float x;

            [FieldOffset(0)]
            public int i;
        }
    }
}