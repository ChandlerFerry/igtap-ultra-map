using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    [DebuggerDisplay("b2Fixture of {m_body.UserData}")]
    public class b2Fixture
    {
        public b2Body m_body;

        public float m_density;
        public b2Filter m_filter;
        public float m_friction;

        public b2Fixture m_next;
        public FixtureProxy[] m_proxies;
        public int m_proxyCount;
        public float m_restitution;

        public b2Fixture()
        {
            UserData = null;
            m_proxyCount = 0;
            m_density = 0f;
        }

        public b2Shape b2Shape
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        internal bool Sensor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            set;
        }

        public b2Filter FilterData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_filter;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_filter = value;
                Refilter();
            }
        }

        public b2Body b2Body
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_body;
        }

        public b2Fixture Next
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_next;
        }

        public float Density
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_density;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_density = value;
        }

        public float Restitution
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_restitution;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_restitution = value;
        }

        public object UserData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            set;
        }

        public void Create(b2Body body, b2FixtureDef def)
        {
            if (def.shape == null)
            {
                throw new ArgumentNullException("def.shape");
            }

            UserData = def.userData;
            m_friction = def.friction;
            m_restitution = def.restitution;

            m_body = body;
            m_next = null;

            m_filter = def.filter;

            Sensor = def.isSensor;

            b2Shape = def.shape.Clone();

            int childCount = b2Shape.GetChildCount();
            m_proxies = new FixtureProxy[childCount];
            for (var i = 0; i < childCount; ++i)
            {
                m_proxies[i] = new FixtureProxy();
            }

            m_proxyCount = 0;

            m_density = def.density;
        }

        public void CreateProxies(b2BroadPhase broadPhase, in b2Transform xf)
        {
            m_proxyCount = b2Shape.GetChildCount();

            for (var i = 0; i < m_proxyCount; ++i)
            {
                FixtureProxy proxy = m_proxies[i];
                b2Shape.ComputeAABB(out proxy.aabb, in xf, i);
                proxy.proxyId = broadPhase.CreateProxy(proxy.aabb, proxy);
                proxy.fixture = this;
                proxy.childIndex = i;
            }
        }

        public void DestroyProxies(b2BroadPhase broadPhase)
        {
            for (var i = 0; i < m_proxyCount; ++i)
            {
                FixtureProxy proxy = m_proxies[i];
                broadPhase.DestroyProxy(proxy.proxyId);
                proxy.proxyId = -1;
            }

            m_proxyCount = 0;
        }

        public void Synchronize(b2BroadPhase broadPhase, in b2Transform transform1, in b2Transform transform2)
        {
            if (m_proxyCount == 0)
            {
                return;
            }

            for (var i = 0; i < m_proxyCount; ++i)
            {
                FixtureProxy proxy = m_proxies[i];

                b2Shape.ComputeAABB(out b2AABB aabb1, in transform1, proxy.childIndex);
                b2Shape.ComputeAABB(out b2AABB aabb2, in transform2, proxy.childIndex);

                proxy.aabb = b2AABB.Combine(aabb1, aabb2);

                b2Vec2 displacement = aabb2.GetCenter() - aabb1.GetCenter();

                broadPhase.MoveProxy(proxy.proxyId, proxy.aabb, displacement);
            }
        }

        private void SetFilterData(in b2Filter filter)
        {
            m_filter = filter;

            Refilter();
        }

        public void Refilter()
        {
            if (m_body == null)
            {
                return;
            }

            b2ContactEdge edge = m_body.GetContactList();
            while (edge != null)
            {
                b2Contact contact = edge.contact;
                b2Fixture fixtureA = contact.GetFixtureA();
                b2Fixture fixtureB = contact.GetFixtureB();
                if (fixtureA == this || fixtureB == this)
                {
                    contact.FlagForFiltering();
                }

                edge = edge.next;
            }

            b2World world = m_body.GetWorld();

            if (world == null)
            {
                return;
            }

            b2BroadPhase broadPhase = world.m_contactManager.m_broadPhase;
            for (var i = 0; i < m_proxyCount; ++i)
            {
                broadPhase.TouchProxy(m_proxies[i].proxyId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSensor() => Sensor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private b2Filter GetFilterData() => m_filter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Body GetBody() => b2Body;

        public b2Fixture GetNext() => m_next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Shape GetShape() => this.b2Shape;

        public bool TestPoint(in b2Vec2 p) => b2Shape.TestPoint(m_body.GetTransform(), p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RayCast(out b2RayCastOutput output, in b2RayCastInput input, int childIndex) =>
            b2Shape.RayCast(out output, in input, m_body.GetTransform(), childIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetMassData(out b2MassData massData)
        {
            b2Shape.ComputeMass(out massData, m_density);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2AABB GetAABB(int childIndex) => m_proxies[childIndex].aabb;
    }
}