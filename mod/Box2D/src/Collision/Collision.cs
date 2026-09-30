using System.Numerics;

namespace Box2D
{
    public class Collision
    {
        public static bool TestOverlap(in b2AABB a, in b2AABB b)
        {
            b2Vec2 d1, d2;
            d1 = b.lowerBound - a.upperBound;
            d2 = a.lowerBound - b.upperBound;

            if (d1.X > 0.0f || d1.Y > 0.0f)
            {
                return false;
            }

            if (d2.X > 0.0f || d2.Y > 0.0f)
            {
                return false;
            }

            return true;
        }

        public static int ClipSegmentToLine(
            out b2ClipVertex[] vOut,
            in b2ClipVertex[] vIn,
            in b2Vec2 normal,
            float offset,
            int vertexIndexA)
        {
            vOut = new b2ClipVertex[2];

            var numOut = 0;

            float distance0 = b2Vec2.Dot(normal, vIn[0].v) - offset;
            float distance1 = b2Vec2.Dot(normal, vIn[1].v) - offset;

            if (distance0 <= 0.0f)
            {
                vOut[numOut++] = vIn[0];
            }

            if (distance1 <= 0.0f)
            {
                vOut[numOut++] = vIn[1];
            }

            if (distance0 * distance1 < 0.0f)
            {
                float interp = distance0 / (distance0 - distance1);
                vOut[numOut].v = vIn[0].v + interp * (vIn[1].v - vIn[0].v);

                vOut[numOut].id.cf.indexA = (byte)vertexIndexA;
                vOut[numOut].id.cf.indexB = vIn[0].id.cf.indexB;
                vOut[numOut].id.cf.typeA = (byte)b2ContactFeatureType.Vertex;
                vOut[numOut].id.cf.typeB = (byte)b2ContactFeatureType.Face;
                ++numOut;
            }

            return numOut;
        }
    }
}