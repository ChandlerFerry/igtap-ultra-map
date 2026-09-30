using System;
using System.Numerics;

namespace Box2D
{
    public abstract class b2DebugDraw
    {
        protected b2DrawFlags _drawFlags;

        public b2DebugDraw() => _drawFlags = 0;

        public b2DrawFlags Flags
        {
            get => _drawFlags;
            set => _drawFlags = value;
        }

        public void AppendFlags(b2DrawFlags flags)
        {
            _drawFlags |= flags;
        }

        public void ClearFlags(b2DrawFlags flags)
        {
            _drawFlags &= ~flags;
        }

        public abstract void DrawTransform(in b2Transform xf);

        public abstract void DrawPoint(in b2Vec2 position, float size, in b2Color color);

#pragma warning disable 618
        [Obsolete("Look out for new calls using b2Vec2")]
        public abstract void DrawPolygon(in Vec2[] vertices, int vertexCount, in b2Color color);

        [Obsolete("Look out for new calls using b2Vec2")]
        public abstract void DrawSolidPolygon(in Vec2[] vertices, int vertexCount, in b2Color color);

        [Obsolete("Look out for new calls using b2Vec2")]
        public abstract void DrawCircle(in Vec2 center, float radius, in b2Color color);

        [Obsolete("Look out for new calls using b2Vec2")]
        public abstract void DrawSolidCircle(in Vec2 center, float radius, in Vec2 axis, in b2Color color);

        [Obsolete("Look out for new calls using b2Vec2")]
        public abstract void DrawSegment(in Vec2 p1, in Vec2 p2, in b2Color color);
#pragma warning restore 618
    }
}