namespace Box2D
{
    public struct b2DistanceInput
    {
        public b2Transform transformA;
        public b2Transform transformB;
        public bool useRadii;
        public b2DistanceProxy proxyA;
        public b2DistanceProxy proxyB;
    }
}