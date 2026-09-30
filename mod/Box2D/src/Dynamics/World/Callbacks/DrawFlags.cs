using System;

namespace Box2D
{
    [Flags]
    public enum b2DrawFlags
    {
        b2Shape = 0x0001,
        b2Joint = 0x0002,
        Aabb = 0x0008,
        Pair = 0x0020,
        CenterOfMass = 0x0040
    }
}