namespace Box2D
{
    public class ContactManager
    {
        public b2BroadPhase m_broadPhase;
        public int m_contactCount;
        public b2ContactFilter m_contactFilter;
        public b2Contact m_contactList;
        public b2Contact m_contactTail;
        public b2ContactListener m_contactListener;

        public ContactManager()
        {
            m_contactList = null;
            m_contactCount = 0;
            m_contactFilter = new b2ContactFilter();
            m_contactListener = null;
            m_broadPhase = new b2BroadPhase();
        }

        public void Destroy(b2Contact c)
        {
            b2Fixture fixtureA = c.FixtureA;
            b2Fixture fixtureB = c.FixtureB;
            if (fixtureA == null || fixtureB == null)
            {
                return;
            }

            b2Body bodyA = fixtureA.b2Body;
            b2Body bodyB = fixtureB.b2Body;

            if (bodyA == null || bodyB == null)
            {
                return;
            }

            if (m_contactListener != null && c.Touching)
            {
                m_contactListener.EndContact(c);
            }

            if (c.Touching)
            {
                c.LeaveTouchOrder();
            }

            if (c.m_prev != null)
            {
                c.m_prev.m_next = c.m_next;
            }

            if (c.m_next != null)
            {
                c.m_next.m_prev = c.m_prev;
            }

            if (c == m_contactList)
            {
                m_contactList = c.m_next;
            }

            if (c == m_contactTail)
            {
                m_contactTail = c.m_prev;
            }

            if (c.m_nodeA.prev != null)
            {
                c.m_nodeA.prev.next = c.m_nodeA.next;
            }

            if (c.m_nodeA.next != null)
            {
                c.m_nodeA.next.prev = c.m_nodeA.prev;
            }

            if (c.m_nodeA == bodyA.m_contactList)
            {
                bodyA.m_contactList = c.m_nodeA.next;
            }

            if (c.m_nodeB.prev != null)
            {
                c.m_nodeB.prev.next = c.m_nodeB.next;
            }

            if (c.m_nodeB.next != null)
            {
                c.m_nodeB.next.prev = c.m_nodeB.prev;
            }

            if (c.m_nodeB == bodyB.m_contactList)
            {
                bodyB.m_contactList = c.m_nodeB.next;
            }

            --m_contactCount;
        }

        public void Collide()
        {
            b2Contact c = m_contactList;
            while (c != null)
            {
                b2Fixture fixtureA = c.FixtureA;
                b2Fixture fixtureB = c.FixtureB;
                int indexA = c.ChildIndexA;
                int indexB = c.ChildIndexB;
            if (fixtureA == null || fixtureB == null)
            {
                return;
            }

                b2Body bodyA = fixtureA.b2Body;
                b2Body bodyB = fixtureB.b2Body;

            if (bodyA == null || bodyB == null)
            {
                return;
            }

                if ((c.m_flags & b2CollisionFlags.b2Filter) == b2CollisionFlags.b2Filter)
                {
                    if (bodyB.ShouldCollide(bodyA) == false)
                    {
                        b2Contact cNuke = c;
                        c = cNuke.GetNext();
                        Destroy(cNuke);
                        continue;
                    }

                    if (m_contactFilter != null && m_contactFilter.ShouldCollide(fixtureA, fixtureB) == false)
                    {
                        b2Contact cNuke = c;
                        c = cNuke.GetNext();
                        Destroy(cNuke);
                        continue;
                    }

                    c.m_flags &= ~b2CollisionFlags.b2Filter;
                }

                bool activeA = bodyA.IsAwake() && bodyA.m_type != b2BodyType.Static;
                bool activeB = bodyB.IsAwake() && bodyB.m_type != b2BodyType.Static;

                if (activeA == false && activeB == false)
                {
                    c = c.GetNext();
                    continue;
                }

                int proxyIdA = fixtureA.m_proxies[indexA].proxyId;
                int proxyIdB = fixtureB.m_proxies[indexB].proxyId;
                bool overlap = m_broadPhase.TestOverlap(proxyIdA, proxyIdB);

                if (overlap == false)
                {
                    b2Contact cNuke = c;
                    c = cNuke.GetNext();
                    Destroy(cNuke);
                    continue;
                }

                c.Update(m_contactListener);
                c = c.GetNext();
            }
        }

        public void FindNewContacts()
        {
            m_broadPhase.UpdatePairs(AddPair);
        }

        private void AddPair(object proxyUserDataA, object proxyUserDataB)
        {
            var proxyA = (FixtureProxy)proxyUserDataA;
            var proxyB = (FixtureProxy)proxyUserDataB;

            b2Fixture fixtureA = proxyA.fixture;
            b2Fixture fixtureB = proxyB.fixture;

            int indexA = proxyA.childIndex;
            int indexB = proxyB.childIndex;

            if (fixtureA == null || fixtureB == null)
            {
                return;
            }

            b2Body bodyA = fixtureA.b2Body;
            b2Body bodyB = fixtureB.b2Body;

            if (bodyA == null || bodyB == null)
            {
                return;
            }

            if (bodyA == bodyB)
            {
                return;
            }

            b2ContactEdge edge = bodyB.GetContactList();
            while (edge != null)
            {
                if (edge.other == bodyA)
                {
                    b2Fixture fA = edge.contact.GetFixtureA();
                    b2Fixture fB = edge.contact.GetFixtureB();
                    int iA = edge.contact.GetChildIndexA();
                    int iB = edge.contact.GetChildIndexB();

                    if (fA == fixtureA && fB == fixtureB && iA == indexA && iB == indexB)
                    {
                        return;
                    }

                    if (fA == fixtureB && fB == fixtureA && iA == indexB && iB == indexA)
                    {
                        return;
                    }
                }

                edge = edge.next;
            }

            if (bodyB.ShouldCollide(bodyA) == false)
            {
                return;
            }

            if (m_contactFilter != null && m_contactFilter.ShouldCollide(fixtureA, fixtureB) == false)
            {
                return;
            }

            var c = b2Contact.Create(fixtureA, indexA, fixtureB, indexB);
            if (c == null)
            {
                return;
            }

            fixtureA = c.GetFixtureA();
            fixtureB = c.GetFixtureB();
            indexA = c.GetChildIndexA();
            indexB = c.GetChildIndexB();
            bodyA = fixtureA.GetBody();
            bodyB = fixtureB.GetBody();

            c.m_prev = m_contactTail;
            c.m_next = null;
            if (m_contactTail != null)
            {
                m_contactTail.m_next = c;
            }
            else
            {
                m_contactList = c;
            }

            m_contactTail = c;

            c.m_nodeA.contact = c;
            c.m_nodeA.other = bodyB;

            c.m_nodeA.prev = null;
            c.m_nodeA.next = bodyA.m_contactList;
            if (bodyA.m_contactList != null)
            {
                bodyA.m_contactList.prev = c.m_nodeA;
            }

            bodyA.m_contactList = c.m_nodeA;

            c.m_nodeB.contact = c;
            c.m_nodeB.other = bodyA;

            c.m_nodeB.prev = null;
            c.m_nodeB.next = bodyB.m_contactList;
            if (bodyB.m_contactList != null)
            {
                bodyB.m_contactList.prev = c.m_nodeB;
            }

            bodyB.m_contactList = c.m_nodeB;

            ++m_contactCount;
        }
    }
}