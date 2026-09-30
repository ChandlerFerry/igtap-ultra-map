using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    [Obsolete("Since Vec2 has been replaced with System.Numerics.b2Vec2, this will be implictly cast to a b2Vec2. It is recommended to change your code to use System.Numerics.b2Vec2 instead.")]
    public struct Vec2
    {
        private bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        [Obsolete("Warning: Implicit cast from Vec2 to System.Numerics.b2Vec2. You are advised to change your code to expect b2Vec2.")]
        public static implicit operator b2Vec2(Vec2 src) => new b2Vec2(src.X, src.Y);

        [Obsolete("Warning: Implicit cast from System.Numerics.b2Vec2 to Vec2. You are advised to change your code to expect b2Vec2.")]
        public static implicit operator Vec2(b2Vec2 src) => new Vec2(src.X, src.Y);

        [Obsolete("Warning: Implicit cast from System.Numerics.b2Vec2 to Vec2. You are advised to change your code to expect b2Vec2.")]
        public static implicit operator Vec2((float, float) src) => new Vec2(src.Item1, src.Item2);

        public float X, Y;

        [Obsolete("Since Vec2 has been replaced with System.Numerics.b2Vec2, this will be implictly cast to a b2Vec2. It is recommended to change your code to use System.Numerics.b2Vec2 instead.")]
        public Vec2(float x)
        {
            X = x;
            Y = x;
        }

        [Obsolete("Since Vec2 has been replaced with System.Numerics.b2Vec2, this will be implictly cast to a b2Vec2. It is recommended to change your code to use System.Numerics.b2Vec2 instead.")]
        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        [Obsolete("Since Vec2 has been replaced with System.Numerics.b2Vec2, this means vectors are now considered immutable. Instead, please create a new b2Vec2 and assign it.",
                  true)]
        public void SetZero()
        {
            X = 0.0f;
            Y = 0.0f;
        }

        [Obsolete("Since Vec2 has been replaced with System.Numerics.b2Vec2, this means vectors are now considered immutable. Instead, please create a new b2Vec2 and assign it.",
                  true)]
        public void Set(float x, float y)
        {
            X = x;
            Y = y;
        }

        [Obsolete("Since Vec2 has been replaced with System.Numerics.b2Vec2, this means vectors are now considered immutable. Instead, please create a new b2Vec2 and assign it.",
                  true)]
        public void Set(float xy)
        {
            X = xy;
            Y = xy;
        }

        [Obsolete("This will still work, but may be removed in a future version. Check the field or property and see if a newer b2Vec2 is available.")]
        public float Length() => (float)System.Math.Sqrt(X * X + Y * Y);

        public float LengthSquared() => X * X + Y * Y;

        [Obsolete("Since Vec2 has been replaced with System.Numerics.b2Vec2, this won't work any more. If you need the Length, get .Length. If you need to normalize a vector, call b2Vec2.Normalize and re-assign the result.",
                  true)]
        public float Normalize()
        {
            float length = Length();
            if (length < b2Settings.FLT_EPSILON)
            {
                return 0.0f;
            }

            float invLength = 1.0f / length;
            X *= invLength;
            Y *= invLength;

            return length;
        }

        [Obsolete("Please switch to System.Numerics.b2Vec2 and use b2Vec2.IsValid() instead.")]
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.IsValid(X) && Math.IsValid(Y);
        }

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator -(Vec2 v1) => new Vec2(-v1.X, -v1.Y);

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator +(Vec2 v1, Vec2 v2) => new Vec2(v1.X + v2.X, v1.Y + v2.Y);

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator -(Vec2 v1, Vec2 v2) => new Vec2(v1.X - v2.X, v1.Y - v2.Y);

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(Vec2 v1, float a) => new Vec2(v1.X * a, v1.Y * a);

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(float a, Vec2 v1) => new Vec2(v1.X * a, v1.Y * a);

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vec2 a, Vec2 b) => a.X == b.X && a.Y == b.Y;

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vec2 a, Vec2 b) => a.X != b.X || a.Y != b.Y;

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        public static Vec2 Zero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Vec2(0, 0);
        }

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

        [Obsolete("Please switch to System.Numerics.b2Vec2.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

        [Obsolete("Please switch to System.Numerics.b2Vec2 and use Vectex.Cross.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Cross(Vec2 a, float s) => new Vec2(s * a.Y, -s * a.X);

        [Obsolete("Please switch to System.Numerics.b2Vec2 and use Vectex.Cross.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Cross(float s, Vec2 a) => new Vec2(-s * a.Y, s * a.X);

        [Obsolete("Use b2Vec2.Distance instead")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length();

        [Obsolete("Use b2Vec2.DistanceSquared instead")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vec2 a, Vec2 b)
        {
            Vec2 c = a - b;
            return Dot(c, c);
        }

        public static Vec2[] ConvertArray(b2Vec2[] vertices)
        {
            var result = new Vec2[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                result[i] = vertices[i];
            }

            return result;
        }
    }
}