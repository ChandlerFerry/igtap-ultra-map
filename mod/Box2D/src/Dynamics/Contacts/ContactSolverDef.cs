namespace Box2D
{
    public struct ContactSolverDef
    {
        public b2TimeStep step;
        public b2Contact[] contacts;
        public int count;
        public b2Position[] positions;
        public b2Velocity[] velocities;
    }
}