namespace Box2D
{
    public class b2ContactImpulse
    {
        public float[] normalImpulses = new float[b2Settings.MaxManifoldPoints];
        public float[] tangentImpulses = new float[b2Settings.MaxManifoldPoints];
    }
}