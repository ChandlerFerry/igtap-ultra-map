using System.Runtime.InteropServices;

namespace Box2D
{
    [StructLayout(LayoutKind.Explicit)]
    public struct b2ContactID
    {
        [FieldOffset(0)]
        public b2ContactFeature cf;

        [FieldOffset(0)]
        public uint key;
    }
}
