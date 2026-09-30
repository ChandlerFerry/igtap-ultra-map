using System;

namespace Box2D
{
    [Flags]
    public enum b2CollisionFlags
    {
        b2Island = 0x01,
        Touching = 0x02,
        Enabled = 0x04,
        b2Filter = 0x08,
        BulletHit = 0x10,
        Toi = 0x20
    }
}