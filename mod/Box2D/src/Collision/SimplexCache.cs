namespace Box2D
{
    public struct b2SimplexCache
    {
        public ushort count;

        public b2Array3<byte> indexA;

        public b2Array3<byte> indexB;

        public float metric;
    }

    [System.Runtime.CompilerServices.InlineArray(3)]
    public struct b2Array3<T>
    {
        private T _element0;
    }
}