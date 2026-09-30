#pragma warning disable 618

namespace Box2D
{
    public abstract class b2DestructionListener
    {
        public abstract void SayGoodbye(b2Joint joint);

        public abstract void SayGoodbye(b2Fixture fixture);
    }
}