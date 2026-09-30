namespace Box2D
{
    public struct b2TimeStep
    {
        public float dt;
        public float inv_dt;
        public float dtRatio;
        public int velocityIterations;
        public int positionIterations;
        public bool warmStarting;
    }
}