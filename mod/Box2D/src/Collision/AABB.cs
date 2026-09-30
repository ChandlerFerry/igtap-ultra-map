using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public struct b2AABB
    {
        public b2Vec2 lowerBound;

        public b2Vec2 upperBound;

        public b2Vec2 LowerBound
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => lowerBound;
        }

        public b2Vec2 UpperBound
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => upperBound;
        }

        public b2Vec2 Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => upperBound - lowerBound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Vec2 GetCenter() => 0.5f * (lowerBound + upperBound);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Vec2 GetExtents() => 0.5f * (upperBound - lowerBound);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPerimeter()
        {
            float wx = upperBound.X - lowerBound.X;
            float wy = upperBound.Y - lowerBound.Y;
            return 2.0f * (wx + wy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Combine(in b2AABB aabb)
        {
            lowerBound = b2Vec2.Min(lowerBound, aabb.lowerBound);
            upperBound = b2Vec2.Max(upperBound, aabb.upperBound);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static b2AABB Combine(in b2AABB aabb1, in b2AABB aabb2)
        {
            b2AABB result = default;
            result.lowerBound = b2Vec2.Min(aabb1.lowerBound, aabb2.lowerBound);
            result.upperBound = b2Vec2.Max(aabb1.upperBound, aabb2.upperBound);
            return result;
        }

        public b2AABB Enlarged(float amount)
        {
            b2Vec2 vecAmt = new b2Vec2(amount, amount);
            return new b2AABB(lowerBound - vecAmt, upperBound + vecAmt);
        }

        public bool Intersects(in b2AABB other)
        {
            return other.lowerBound.Y <= this.upperBound.Y &&
                   other.upperBound.Y >= this.lowerBound.Y &&
                   other.upperBound.X >= this.lowerBound.X &&
                   other.lowerBound.X <= this.upperBound.X;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in b2AABB aabb)
        {
            var result = true;
            result = result && lowerBound.X <= aabb.lowerBound.X;
            result = result && lowerBound.Y <= aabb.lowerBound.Y;
            result = result && aabb.upperBound.X <= upperBound.X;
            result = result && aabb.upperBound.Y <= upperBound.Y;
            return result;
        }

        private bool RayCast(out b2RayCastOutput output, in b2RayCastInput input)
        {
            output = default;
            float tmin = float.MinValue;
            float tmax = float.MaxValue;

            b2Vec2 p = input.p1;
            b2Vec2 d = input.p2 - input.p1;
            var absD = b2Vec2.Abs(d);

            b2Vec2 normal = b2Vec2.Zero;

            for (var i = 0; i < 2; ++i)
            {
                if (absD.GetIdx(i) < b2Settings.FLT_EPSILON)
                {
                    if (p.GetIdx(i) < lowerBound.GetIdx(i) || upperBound.GetIdx(i) < p.GetIdx(i))
                    {
                        return false;
                    }
                }
                else
                {
                    float inv_d = 1.0f / d.GetIdx(i);
                    float t1 = (lowerBound.GetIdx(i) - p.GetIdx(i)) * inv_d;
                    float t2 = (upperBound.GetIdx(i) - p.GetIdx(i)) * inv_d;

                    float s = -1.0f;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                        s = 1.0f;
                    }

                    if (t1 > tmin)
                    {
                        normal = new b2Vec2(i == 0 ? s : 0, i == 1 ? s : 0);
                        tmin = t1;
                    }

                    tmax = MathF.Min(tmax, t2);

                    if (tmin > tmax)
                    {
                        return false;
                    }
                }
            }

            if (tmin < 0.0f || input.maxFraction < tmin)
            {
                return false;
            }

            output.fraction = tmin;
            output.normal = normal;
            return true;
        }

        private bool IsValid()
        {
            b2Vec2 d = upperBound - lowerBound;
            bool valid = d.X >= 0.0f && d.Y >= 0.0f;
            valid = valid && lowerBound.IsValid() && upperBound.IsValid();
            return valid;
        }

        public b2AABB(b2Vec2 lowerBound, b2Vec2 upperBound)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }
    }
}
