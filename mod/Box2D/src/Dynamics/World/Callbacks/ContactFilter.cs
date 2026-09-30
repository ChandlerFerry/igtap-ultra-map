namespace Box2D
{
    public class b2ContactFilter
    {
        public virtual bool ShouldCollide(b2Fixture fixtureA, b2Fixture fixtureB)
        {
            b2Filter filterA = fixtureA.m_filter;
            b2Filter filterB = fixtureB.m_filter;

            if (filterA.groupIndex == filterB.groupIndex && filterA.groupIndex != 0)
            {
                return filterA.groupIndex > 0;
            }

            bool collide = (filterA.maskBits & filterB.categoryBits) != 0 && (filterA.categoryBits & filterB.maskBits) != 0;
            return collide;
        }

        public bool RayCollide(object userData, b2Fixture fixture)
        {
            if (userData == null)
            {
                return true;
            }

            return ShouldCollide((b2Fixture)userData, fixture);
        }
    }
}