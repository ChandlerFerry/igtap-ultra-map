using System.Diagnostics;
using System.Numerics;

namespace Box2D
{
    [DebuggerDisplay("localNormal = {" + nameof(localNormal) + "}")]
    public class b2Manifold
    {
        public b2Vec2 localNormal;

        public b2Vec2 localPoint;

        public int pointCount;

        public b2ManifoldPoint[] points = new b2ManifoldPoint[b2Settings.MaxManifoldPoints];

        public b2ManifoldType type;

        public b2Manifold()
        {
        }
    }
}