using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Math = Box2D.Math;

namespace Box2D
{
    public partial class b2World
    {
        private bool m_clearForces;

        public readonly ContactManager m_contactManager;
        private bool m_continuousPhysics;
        private bool m_subStepping;

        private bool m_warmStarting;

        private Action DrawDebugDataStub = () => { };
        private bool m_allowSleep;

        private int m_bodyCount;

        private long m_touchCount;
        public long NextTouchOrder() => ++m_touchCount;

        private b2Body m_bodyList;
        private b2DebugDraw m_debugDraw;

        private b2DestructionListener m_destructionListener;

        private b2Vec2 m_gravity;

        private float m_inv_dt0;
        private int m_jointCount;
        private b2Joint m_jointList;
        private bool m_locked;

        public bool m_newContacts;

        private bool m_stepComplete;

        public b2World() : this(new b2Vec2(0, -10))
        { }

        public b2World(b2Vec2 gravity)
        {
            m_destructionListener = null;
            m_debugDraw = null;

            m_bodyList = null;
            m_jointList = null;

            m_bodyCount = 0;
            m_jointCount = 0;

            m_warmStarting = true;
            m_continuousPhysics = true;
            m_subStepping = false;

            m_stepComplete = true;
            m_allowSleep = true;
            m_gravity = gravity;

            m_newContacts = false;
            m_locked = false;
            m_clearForces = true;

            m_inv_dt0 = 0.0f;

            m_contactManager = new ContactManager();
        }

        public b2Vec2 Gravity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_gravity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_gravity = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Body GetBodyList() => m_bodyList;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Joint GetJointList() => m_jointList;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Contact GetContactList() => m_contactManager.m_contactList;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBodyCount() => m_bodyCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetJointCount() => m_jointCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetContactCount() => m_contactManager.m_contactCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGravity(in b2Vec2 gravity)
        {
            m_gravity = gravity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public b2Vec2 GetGravity() => m_gravity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLocked() => m_locked;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetAutoClearForces() => m_clearForces;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ContactManager GetContactManager() => m_contactManager;

        public void SetDestructionListener(b2DestructionListener listener)
        {
            m_destructionListener = listener;
        }

        public void SetContactFilter(b2ContactFilter filter)
        {
            m_contactManager.m_contactFilter = filter;
        }

        public void SetContactListener(b2ContactListener listener)
        {
            m_contactManager.m_contactListener = listener;
        }

        public void SetDebugDraw(b2DebugDraw debugDraw)
        {
            m_debugDraw = debugDraw;
            DrawDebugDataStub = DrawDebugData;
        }

        public b2Body CreateBody(b2BodyDef def)
        {
            if (m_locked)
            {
                throw new
                    Box2DException("Cannot create bodies in the middle of Step. Has this been spawned from an event such as a b2ContactListener callback?");
            }

            var b = new b2Body(def, this);

            b.m_prev = null;
            b.m_next = m_bodyList;
            if (m_bodyList != null)
            {
                m_bodyList.m_prev = b;
            }

            m_bodyList = b;
            ++m_bodyCount;

            return b;
        }

        public void DestroyBody(b2Body b)
        {
            if (m_locked)
            {
                throw new
                    Box2DException("Cannot destroy bodies in the middle of Step. Has this been spawned from an event such as a b2ContactListener callback?");
            }

            b2JointEdge je = b.m_jointList;
            while (je != null)
            {
                b2JointEdge je0 = je;
                je = je.next;

                m_destructionListener?.SayGoodbye(je0.joint);

                DestroyJoint(je0.joint);

                b.m_jointList = je;
            }

            b.m_jointList = null;

            b2ContactEdge ce = b.m_contactList;
            while (ce != null)
            {
                b2ContactEdge ce0 = ce;
                ce = ce.next;
                m_contactManager.Destroy(ce0.contact);
            }

            b.m_contactList = null;

            b2Fixture f = b.m_fixtureList;
            while (f != null)
            {
                b2Fixture f0 = f;
                f = f.m_next;

                m_destructionListener?.SayGoodbye(f0);

                f0.DestroyProxies(m_contactManager.m_broadPhase);

                b.m_fixtureList = f;
                b.m_fixtureCount -= 1;
            }

            b.m_fixtureList = null;
            b.m_fixtureCount = 0;

            if (b.m_prev != null)
            {
                b.m_prev.m_next = b.m_next;
            }

            if (b.m_next != null)
            {
                b.m_next.m_prev = b.m_prev;
            }

            if (b == m_bodyList)
            {
                m_bodyList = b.m_next;
            }

            --m_bodyCount;
            b = null;
        }

        public b2Joint CreateJoint(b2JointDef def)
        {
            if (m_locked)
            {
                throw new
                    Box2DException("Cannot create joints in the middle of Step. Has this been spawned from an event such as a b2ContactListener callback?");
            }

            var j = b2Joint.Create(def);

            j.m_prev = null;
            j.m_next = m_jointList;
            if (m_jointList != null)
            {
                m_jointList.m_prev = j;
            }

            m_jointList = j;
            ++m_jointCount;

            j.m_edgeA.joint = j;
            j.m_edgeA.other = j.m_bodyB;
            j.m_edgeA.Prev = null;
            j.m_edgeA.next = j.m_bodyA.m_jointList;
            if (j.m_bodyA.m_jointList != null)
            {
                j.m_bodyA.m_jointList.Prev = j.m_edgeA;
            }

            j.m_bodyA.m_jointList = j.m_edgeA;

            j.m_edgeB.joint = j;
            j.m_edgeB.other = j.m_bodyA;
            j.m_edgeB.Prev = null;
            j.m_edgeB.next = j.m_bodyB.m_jointList;
            if (j.m_bodyB.m_jointList != null)
            {
                j.m_bodyB.m_jointList.Prev = j.m_edgeB;
            }

            j.m_bodyB.m_jointList = j.m_edgeB;

            b2Body bodyA = def.bodyA;
            b2Body bodyB = def.bodyB;

            if (def.collideConnected == false)
            {
                b2ContactEdge edge = bodyB.m_contactList;
                while (edge != null)
                {
                    if (edge.other == bodyA)
                    {
                        edge.contact.FlagForFiltering();
                    }

                    edge = edge.next;
                }
            }

            return j;
        }

        public void DestroyJoint(b2Joint j)
        {
            if (m_locked)
            {
                throw new
                    Box2DException("Cannot destroy joints in the middle of Step. Has this been spawned from an event such as a b2ContactListener callback?");
            }

            bool collideConnected = j.m_collideConnected;

            if (j.m_prev != null)
            {
                j.m_prev.m_next = j.m_next;
            }

            if (j.m_next != null)
            {
                j.m_next.m_prev = j.m_prev;
            }

            if (j == m_jointList)
            {
                m_jointList = j.m_next;
            }

            b2Body bodyA = j.m_bodyA;
            b2Body bodyB = j.m_bodyB;

            bodyA.SetAwake(true);
            bodyB.SetAwake(true);

            if (j.m_edgeA.Prev != null)
            {
                j.m_edgeA.Prev.next = j.m_edgeA.next;
            }

            if (j.m_edgeA.next != null)
            {
                j.m_edgeA.next.Prev = j.m_edgeA.Prev;
            }

            if (j.m_edgeA == bodyA.m_jointList)
            {
                bodyA.m_jointList = j.m_edgeA.next;
            }

            j.m_edgeA.Prev = null;
            j.m_edgeA.next = null;

            if (j.m_edgeB.Prev != null)
            {
                j.m_edgeB.Prev.next = j.m_edgeB.next;
            }

            if (j.m_edgeB.next != null)
            {
                j.m_edgeB.next.Prev = j.m_edgeB.Prev;
            }

            if (j.m_edgeB == bodyB.m_jointList)
            {
                bodyB.m_jointList = j.m_edgeB.next;
            }

            j.m_edgeB.Prev = null;
            j.m_edgeB.next = null;

            --m_jointCount;

            if (collideConnected == false)
            {
                b2ContactEdge edge = bodyB.m_contactList;
                while (edge != null)
                {
                    if (edge.other == bodyA)
                    {
                        edge.contact.FlagForFiltering();
                    }

                    edge = edge.next;
                }
            }
        }

        public void SetWarmStarting(bool flag) => m_warmStarting = flag;

        public void SetAutoClearForces(bool flag) => m_clearForces = flag;

        public void SetAllowSleeping(bool flag)
        {
            if (flag == m_allowSleep)
            {
                return;
            }

            m_allowSleep = flag;
            if (!m_allowSleep)
            {
                for (b2Body b = m_bodyList; b != null; b = b.m_next)
                {
                    b.SetAwake(true);
                }
            }
        }

        private void Solve(b2TimeStep step)
        {
            if (m_island == null || !m_island.Fits(m_bodyCount, m_contactManager.m_contactCount, m_jointCount, m_contactManager.m_contactListener))
                m_island = new b2Island(m_bodyCount,
                                    m_contactManager.m_contactCount,
                                    m_jointCount,
                                    m_contactManager.m_contactListener);
            b2Island island = m_island;

            for (b2Body b = m_bodyList; b != null; b = b.m_next)
            {
                b.UnsetFlag(b2BodyFlags.b2Island);
            }

            for (b2Contact c = m_contactManager.m_contactList; c != null; c = c.m_next)
            {
                c.m_flags &= ~b2CollisionFlags.b2Island;
            }

            for (b2Joint j = m_jointList; j != null; j = j.m_next)
            {
                j.m_islandFlag = false;
            }

            int stackSize = m_bodyCount;
            if (m_islandStack == null || m_islandStack.Length < m_bodyCount) m_islandStack = new b2Body[m_bodyCount];
            b2Body[] stack = m_islandStack;
            for (b2Body seed = m_bodyList; seed != null; seed = seed.m_next)
            {
                if (seed.HasFlag(b2BodyFlags.b2Island))
                {
                    continue;
                }

                if (seed.IsAwake() == false || seed.IsEnabled() == false)
                {
                    continue;
                }

                if (seed.m_type == b2BodyType.Static)
                {
                    continue;
                }

                island.Clear();
                var stackCount = 0;
                stack[stackCount++] = seed;
                seed.SetFlag(b2BodyFlags.b2Island);

                while (stackCount > 0)
                {
                    b2Body b = stack[--stackCount];
                    island.Add(b);

                    if (b.m_type == b2BodyType.Static)
                    {
                        continue;
                    }

                    b.SetFlag(b2BodyFlags.Awake);

                    for (b2ContactEdge ce = b.m_contactList; ce != null; ce = ce.next)
                    {
                        b2Contact contact = ce.contact;

                        if ((contact.m_flags & b2CollisionFlags.b2Island) == b2CollisionFlags.b2Island)
                        {
                            continue;
                        }

                        if (contact.IsEnabled() == false ||
                            contact.IsTouching() == false)
                        {
                            continue;
                        }

                        bool sensorA = contact.m_fixtureA.IsSensor();
                        bool sensorB = contact.m_fixtureB.IsSensor();
                        if (sensorA || sensorB)
                        {
                            continue;
                        }

                        island.Add(contact);
                        contact.m_flags |= b2CollisionFlags.b2Island;

                        b2Body other = ce.other;

                        if (other.HasFlag(b2BodyFlags.b2Island))
                        {
                            continue;
                        }

                        stack[stackCount++] = other;
                        other.SetFlag(b2BodyFlags.b2Island);
                    }

                    for (b2JointEdge je = b.m_jointList; je != null; je = je.next)
                    {
                        if (je.joint.m_islandFlag)
                        {
                            continue;
                        }

                        b2Body other = je.other;

                        if (other.IsEnabled() == false)
                        {
                            continue;
                        }

                        island.Add(je.joint);
                        je.joint.m_islandFlag = true;

                        if (other.HasFlag(b2BodyFlags.b2Island))
                        {
                            continue;
                        }

                        stack[stackCount++] = other;
                        other.SetFlag(b2BodyFlags.b2Island);
                    }
                }

                island.Solve(step, m_gravity, m_allowSleep);

                for (var i = 0; i < island.m_bodyCount; ++i)
                {
                    b2Body b = island.m_bodies[i];
                    if (b.m_type == b2BodyType.Static)
                    {
                        b.UnsetFlag(b2BodyFlags.b2Island);
                    }
                }
            }

            stack = null;

            {
                for (b2Body b = m_bodyList; b != null; b = b.GetNext())
                {
                    if (!b.HasFlag(b2BodyFlags.b2Island))
                    {
                        continue;
                    }

                    if (b.m_type == b2BodyType.Static)
                    {
                        continue;
                    }

                    b.SynchronizeFixtures();
                }

                m_contactManager.FindNewContacts();
            }
        }

        public void SetContinuousPhysics(bool flag) => m_continuousPhysics = flag;

        public void SetSubStepping(bool flag) => m_subStepping = flag;

        private void SolveTOI(in b2TimeStep step)
        {
            if (m_toiIsland == null || m_toiIsland.m_bodyCapacity != 2 * b2Settings.maxTOIContacts
                || m_toiIsland.m_contactCapacity != b2Settings.maxTOIContacts || !m_toiIsland.Fits(0, 0, 0, m_contactManager.m_contactListener))
                m_toiIsland = new b2Island(2 * b2Settings.maxTOIContacts, b2Settings.maxTOIContacts, 0,
                                    m_contactManager.m_contactListener);
            b2Island island = m_toiIsland;

            if (m_stepComplete)
            {
                for (b2Body b = m_bodyList; b != null; b = b.m_next)
                {
                    b.UnsetFlag(b2BodyFlags.b2Island);
                    b.m_sweep.alpha0 = 0.0f;
                }

                for (b2Contact c = m_contactManager.m_contactList; c != null; c = c.m_next)
                {
                    c.m_flags &= ~(b2CollisionFlags.Toi | b2CollisionFlags.b2Island);
                    c.m_toiCount = 0;
                    c.m_toi = 1.0f;
                }
            }

            for (; ; )
            {
                b2Contact minContact = null;
                var minAlpha = 1.0f;

                for (b2Contact c = m_contactManager.m_contactList; c != null; c = c.m_next)
                {
                    if (c.Enabled == false)
                    {
                        continue;
                    }

                    if (c.m_toiCount > b2Settings.maxSubSteps)
                    {
                        continue;
                    }

                    var alpha = 1.0f;
                    if ((c.m_flags & b2CollisionFlags.Toi) == b2CollisionFlags.Toi)
                    {
                        alpha = c.m_toi;
                    }
                    else
                    {
                        b2Fixture fA = c.FixtureA;
                        b2Fixture fB = c.FixtureB;

                        if (fA.IsSensor() || fB.IsSensor())
                        {
                            continue;
                        }

                        b2Body bA = fA.b2Body;
                        b2Body bB = fB.b2Body;

                        bool activeA = bA.IsAwake() && bA.m_type != b2BodyType.Static;
                        bool activeB = bB.IsAwake() && bB.m_type != b2BodyType.Static;

                        if (activeA == false && activeB == false)
                        {
                            continue;
                        }

                        bool collideA = bA.IsBullet() || bA.m_type != b2BodyType.Dynamic;
                        bool collideB = bB.IsBullet() || bB.m_type != b2BodyType.Dynamic;

                        if (collideA == false && collideB == false)
                        {
                            continue;
                        }

                        float alpha0 = bA.m_sweep.alpha0;

                        if (bA.m_sweep.alpha0 < bB.m_sweep.alpha0)
                        {
                            bA.m_sweep.Advance(alpha0 = bB.m_sweep.alpha0);
                        }
                        else if (bB.m_sweep.alpha0 < bA.m_sweep.alpha0)
                        {
                            bB.m_sweep.Advance(alpha0);
                        }

                        b2TOIInput input;
                        input.proxyA = new b2DistanceProxy();
                        input.proxyA.Set(fA.b2Shape, c.ChildIndexA);
                        input.proxyB = new b2DistanceProxy();
                        input.proxyB.Set(fB.b2Shape, c.ChildIndexB);
                        input.sweepA = bA.m_sweep;
                        input.sweepB = bB.m_sweep;
                        input.tMax = 1.0f;

                        TOI.TimeOfImpact(out b2TOIOutput output, in input);

                        if (output.state == TOIOutputState.Touching)
                        {
                            alpha = MathF.Min(alpha0 + (1.0f - alpha0) * output.t, 1.0f);
                        }
                        else
                        {
                            alpha = 1.0f;
                        }

                        c.m_toi = alpha;
                        c.m_flags |= b2CollisionFlags.Toi;
                    }

                    if (alpha < minAlpha)
                    {
                        minContact = c;
                        minAlpha = alpha;
                    }
                }

                if (minContact == null || 1.0f - 10.0f * b2Settings.FLT_EPSILON < minAlpha)
                {
                    m_stepComplete = true;
                    break;
                }

                {
                    b2Fixture fA = minContact.m_fixtureA;
                    b2Fixture fB = minContact.m_fixtureB;
                    b2Body bA = fA.b2Body;
                    b2Body bB = fB.b2Body;

                    b2Sweep backup1 = bA.m_sweep;
                    b2Sweep backup2 = bB.m_sweep;

                    bA.Advance(minAlpha);
                    bB.Advance(minAlpha);

                    minContact.Update(m_contactManager.m_contactListener);
                    minContact.m_flags &= ~b2CollisionFlags.Toi;
                    ++minContact.m_toiCount;

                    if (minContact.IsEnabled() == false || minContact.IsTouching() == false)
                    {
                        minContact.SetEnabled(false);
                        bA.m_sweep = backup1;
                        bB.m_sweep = backup2;
                        bA.SynchronizeTransform();
                        bB.SynchronizeTransform();
                        continue;
                    }

                    bA.SetAwake(true);
                    bB.SetAwake(true);

                    island.Clear();
                    island.Add(bA);
                    island.Add(bB);
                    island.Add(minContact);

                    bA.SetFlag(b2BodyFlags.b2Island);
                    bB.SetFlag(b2BodyFlags.b2Island);
                    minContact.m_flags |= b2CollisionFlags.b2Island;

                    b2Body[] bodies = { bA, bB };

                    for (var i = 0; i < 2; ++i)
                    {
                        b2Body body = bodies[i];
                        if (body.m_type == b2BodyType.Dynamic)
                        {
                            for (b2ContactEdge ce = body.m_contactList; ce != null; ce = ce.next)
                            {
                                if (island.m_bodyCount == island.m_bodyCapacity)
                                {
                                    break;
                                }

                                if (island.m_contactCount == island.m_contactCapacity)
                                {
                                    break;
                                }

                                b2Contact contact = ce.contact;

                                if ((contact.m_flags & b2CollisionFlags.b2Island) == b2CollisionFlags.b2Island)
                                {
                                    continue;
                                }

                                b2Body other = ce.other;
                                if (other.m_type == b2BodyType.Dynamic &&
                                    body.IsBullet() == false && other.IsBullet() == false)
                                {
                                    continue;
                                }

                                bool sensorA = contact.m_fixtureA.IsSensor();
                                bool sensorB = contact.m_fixtureB.IsSensor();
                                if (sensorA || sensorB)
                                {
                                    continue;
                                }

                                b2Sweep backup = other.m_sweep;
                                if (!other.HasFlag(b2BodyFlags.b2Island))
                                {
                                    other.Advance(minAlpha);
                                }

                                contact.Update(m_contactManager.m_contactListener);

                                if (contact.IsEnabled() == false)
                                {
                                    other.m_sweep = backup;
                                    other.SynchronizeTransform();
                                    continue;
                                }

                                if (contact.IsTouching() == false)
                                {
                                    other.m_sweep = backup;
                                    other.SynchronizeTransform();
                                    continue;
                                }

                                contact.m_flags |= b2CollisionFlags.b2Island;
                                island.Add(contact);

                                if (other.HasFlag(b2BodyFlags.b2Island))
                                {
                                    continue;
                                }

                                other.SetFlag(b2BodyFlags.b2Island);

                                if (other.m_type != b2BodyType.Static)
                                {
                                    other.SetAwake(true);
                                }

                                island.Add(other);
                            }
                        }
                    }

                    b2TimeStep subStep;
                    subStep.dt = (1.0f - minAlpha) * step.dt;
                    subStep.inv_dt = 1.0f / subStep.dt;
                    subStep.dtRatio = 1.0f;
                    subStep.positionIterations = 20;
                    subStep.velocityIterations = step.velocityIterations;
                    subStep.warmStarting = false;
                    island.SolveTOI(in subStep, bA.m_islandIndex, bB.m_islandIndex);

                    for (var i = 0; i < island.m_bodyCount; ++i)
                    {
                        b2Body body = island.m_bodies[i];
                        body.UnsetFlag(b2BodyFlags.b2Island);

                        if (body.m_type != b2BodyType.Dynamic)
                        {
                            continue;
                        }

                        body.SynchronizeFixtures();

                        for (b2ContactEdge ce = body.m_contactList; ce != null; ce = ce.next)
                        {
                            ce.contact.m_flags &= ~(b2CollisionFlags.Toi | b2CollisionFlags.b2Island);
                        }
                    }

                    m_contactManager.FindNewContacts();

                    if (m_subStepping)
                    {
                        m_stepComplete = false;
                        break;
                    }
                }
            }
        }

        public void Step(float dt, int velocityIterations, int positionIterations)
        {
            if (m_newContacts)
            {
                m_contactManager.FindNewContacts();
                m_newContacts = false;
            }

            m_locked = true;

            b2TimeStep step;
            step.dt = dt;
            step.velocityIterations = velocityIterations;
            step.positionIterations = positionIterations;
            if (dt > 0.0f)
            {
                step.inv_dt = 1.0f / dt;
            }
            else
            {
                step.inv_dt = 0.0f;
            }

            step.dtRatio = m_inv_dt0 * dt;

            step.warmStarting = m_warmStarting;

            {
                m_contactManager.Collide();
            }

            if (m_stepComplete && step.dt > 0.0f)
            {
                Solve(step);
            }

            if (m_continuousPhysics && step.dt > 0.0f)
            {
                SolveTOI(step);
            }

            if (step.dt > 0.0f)
            {
                m_inv_dt0 = step.inv_dt;
            }

            if (m_clearForces)
            {
                ClearForces();
            }

            m_locked = false;
        }

        public void ClearForces()
        {
            for (b2Body body = m_bodyList; body != null; body = body.GetNext())
            {
                body.m_force = b2Vec2.Zero;
                body.m_torque = 0.0f;
            }
        }

        private b2QueryCallback m_queryCallback;
        private b2Island m_island, m_toiIsland;
        private b2Body[] m_islandStack;
        private b2RayCastCallback m_rayCastCallback;
        private Func<int, bool> m_queryProxy;
        private Func<b2RayCastInput, int, float> m_rayCastProxy;

        public void QueryAABB(b2QueryCallback callback, in b2AABB aabb)
        {
            m_queryCallback = callback;
            m_contactManager.m_broadPhase.Query(m_queryProxy ??= QueryProxy, aabb);
        }

        private bool QueryProxy(int proxyId)
        {
            var proxy = (FixtureProxy)m_contactManager.m_broadPhase.GetUserData(proxyId);
            return m_queryCallback.ReportFixture(proxy.fixture);
        }

        public int QueryAABB(out b2Fixture[] fixtures, in b2AABB aabb, int maxFixtures = 256)
        {
            var result = new b2Fixture[maxFixtures];

            var i = 0;

            bool internalCallback(int proxyId)
            {
                var proxy = (FixtureProxy)m_contactManager.m_broadPhase.GetUserData(proxyId);
                result[i++] = proxy.fixture;
                return i != maxFixtures;
            }

            m_contactManager.m_broadPhase.Query(internalCallback, aabb);

            fixtures = result;
            return i;
        }

        public void RayCast(b2RayCastCallback callback, in b2Vec2 point1, in b2Vec2 point2)
        {
            m_rayCastCallback = callback;
            b2RayCastInput input;
            input.maxFraction = 1.0f;
            input.p1 = point1;
            input.p2 = point2;
            m_contactManager.m_broadPhase.RayCast(m_rayCastProxy ??= RayCastProxy, in input);
        }

        private float RayCastProxy(b2RayCastInput input, int proxyId)
        {
            object userData = m_contactManager.m_broadPhase.GetUserData(proxyId);
            var proxy = (FixtureProxy)userData;
            b2Fixture fixture = proxy.fixture;
            int index = proxy.childIndex;
            bool hit = fixture.RayCast(out b2RayCastOutput output, input, index);

            if (hit)
            {
                float fraction = output.fraction;
                b2Vec2 point = (1f - fraction) * input.p1 + fraction * input.p2;
                return m_rayCastCallback.ReportFixture(fixture, point, output.normal, fraction);
            }

            return input.maxFraction;
        }

        private void DrawJoint(b2Joint joint)
        {
            b2Body b1 = joint.GetBodyA();
            b2Body b2 = joint.GetBodyB();
            b2Transform xf1 = b1.GetTransform();
            b2Transform xf2 = b2.GetTransform();
            b2Vec2 x1 = xf1.p;
            b2Vec2 x2 = xf2.p;
            b2Vec2 p1 = joint.GetAnchorA;
            b2Vec2 p2 = joint.GetAnchorB;

            var color = new b2Color(0.5f, 0.8f, 0.8f);

            switch (joint)
            {
                case b2DistanceJoint j:
                    m_debugDraw.DrawSegment(p1, p2, color);
                    break;

                case b2PulleyJoint pulley:
                    {
                        b2Vec2 s1 = pulley.GroundAnchorA;
                        b2Vec2 s2 = pulley.GroundAnchorB;
                        m_debugDraw.DrawSegment(s1, p1, color);
                        m_debugDraw.DrawSegment(s2, p2, color);
                        m_debugDraw.DrawSegment(s1, s2, color);
                    }
                    break;

                case b2MouseJoint j:
                    break;

                default:
                    m_debugDraw.DrawSegment(x1, p1, color);
                    m_debugDraw.DrawSegment(p1, p2, color);
                    m_debugDraw.DrawSegment(x2, p2, color);
                    break;
            }
        }

        private void DrawFixture(b2Fixture fixture, b2Transform xf, b2Color color)
        {
            switch (fixture.b2Shape)
            {
                case b2CircleShape circle:
                    {
                        b2Vec2 center = Math.Mul(xf, circle.m_p);
                        float radius = circle.m_radius;
                        var axis = new b2Vec2(xf.q.M11, xf.q.M21);

                        m_debugDraw.DrawSolidCircle(center, radius, axis, color);
                    }
                    break;

                case b2PolygonShape poly:
                    {
                        int vertexCount = poly.m_count;
                        b2Vec2[] localVertices = poly.m_vertices;

                        var vertices = new b2Vec2[b2Settings.MaxPolygonVertices];

                        for (var i = 0; i < vertexCount; ++i)
                        {
                            vertices[i] = Math.Mul(xf, localVertices[i]);
                        }

                        m_debugDraw.DrawSolidPolygon(Vec2.ConvertArray(vertices), vertexCount, color);
                    }
                    break;

                case b2EdgeShape edge:
                    {
                        m_debugDraw.DrawSegment(Math.Mul(xf, edge.m_vertex1), Math.Mul(xf, edge.m_vertex2), color);
                    }
                    break;
            }
        }

        public void DrawDebugData()
        {
            b2DrawFlags flags = m_debugDraw.Flags;
            if ((flags & b2DrawFlags.b2Shape) == b2DrawFlags.b2Shape)
            {
                for (b2Body b = m_bodyList; b != null; b = b.GetNext())
                {
                    b2Transform xf = b.GetTransform();
                    for (b2Fixture f = b.GetFixtureList(); f != null; f = f.GetNext())
                    {
                        if (b.Type() == b2BodyType.Dynamic && b.m_mass == 0.0f)
                        {
                            DrawShape(f, xf, new b2Color(1.0f, 0.0f, 0.0f));
                        }
                        else if (b.IsEnabled() == false)
                        {
                            DrawShape(f, xf, new b2Color(0.5f, 0.5f, 0.3f));
                        }
                        else if (b.Type() == b2BodyType.Static)
                        {
                            DrawShape(f, xf, new b2Color(0.5f, 0.9f, 0.5f));
                        }
                        else if (b.Type() == b2BodyType.Kinematic)
                        {
                            DrawShape(f, xf, new b2Color(0.5f, 0.5f, 0.9f));
                        }
                        else if (b.IsAwake() == false)
                        {
                            DrawShape(f, xf, new b2Color(0.6f, 0.6f, 0.6f));
                        }
                        else
                        {
                            DrawShape(f, xf, new b2Color(0.9f, 0.7f, 0.7f));
                        }
                    }
                }
            }

            if ((flags & b2DrawFlags.b2Joint) == b2DrawFlags.b2Joint)
            {
                for (b2Joint j = m_jointList; j != null; j = j.GetNext())
                {
                    j.Draw(m_debugDraw);
                }
            }

            if ((flags & b2DrawFlags.Pair) == b2DrawFlags.Pair)
            {
                var color = new b2Color(0.3f, 0.9f, 0.9f);
                for (b2Contact c = m_contactManager.m_contactList; c != null; c = c.GetNext())
                {
                    b2Fixture fixtureA = c.GetFixtureA();
                    b2Fixture fixtureB = c.GetFixtureB();
                    int indexA = c.GetChildIndexA();
                    int indexB = c.GetChildIndexB();
                    b2Vec2 cA = fixtureA.GetAABB(indexA).GetCenter();
                    b2Vec2 cB = fixtureB.GetAABB(indexB).GetCenter();

                    m_debugDraw.DrawSegment(cA, cB, color);
                }
            }

            if ((flags & b2DrawFlags.Aabb) == b2DrawFlags.Aabb)
            {
                var color = new b2Color(0.9f, 0.3f, 0.9f);
                b2BroadPhase bp = m_contactManager.m_broadPhase;

                for (b2Body b = m_bodyList; b != null; b = b.GetNext())
                {
                    if (b.IsEnabled() == false)
                    {
                        continue;
                    }

                    for (b2Fixture f = b.GetFixtureList(); f != null; f = f.GetNext())
                        for (var i = 0; i < f.m_proxyCount; ++i)
                        {
                            FixtureProxy proxy = f.m_proxies[i];
                            b2AABB aabb = bp.GetFatAABB(proxy.proxyId);
                            var vs = new Vec2[4];
                            vs[0] = new Vec2(aabb.lowerBound.X, aabb.lowerBound.Y);
                            vs[1] = new Vec2(aabb.upperBound.X, aabb.lowerBound.Y);
                            vs[2] = new Vec2(aabb.upperBound.X, aabb.upperBound.Y);
                            vs[3] = new Vec2(aabb.lowerBound.X, aabb.upperBound.Y);

                            m_debugDraw.DrawPolygon(vs, 4, color);
                        }
                }
            }

            if ((flags & b2DrawFlags.CenterOfMass) == b2DrawFlags.CenterOfMass)
            {
                for (b2Body b = m_bodyList; b != null; b = b.GetNext())
                {
                    b2Transform xf = b.GetTransform();
                    xf.p = b.GetWorldCenter();
                    m_debugDraw.DrawTransform(xf);
                }
            }
        }

        private void DrawShape(b2Fixture fixture, in b2Transform xf, in b2Color color)
        {
            switch (fixture.b2Shape)
            {
                case b2CircleShape circle:
                    {
                        Vec2 center = Math.Mul(xf, circle.m_p);
                        float radius = circle.m_radius;
                        Vec2 axis = b2Vec2.Transform(new b2Vec2(1.0f, 0.0f), xf.q);

                        m_debugDraw.DrawSolidCircle(center, radius, axis, color);
                    }
                    break;

                case b2EdgeShape edge:
                    {
                        b2Vec2 v1 = Math.Mul(xf, edge.m_vertex1);
                        b2Vec2 v2 = Math.Mul(xf, edge.m_vertex2);
                        m_debugDraw.DrawSegment(v1, v2, color);

                        if (!edge.m_oneSided)
                        {
                            m_debugDraw.DrawPoint(v1, 4f, color);
                            m_debugDraw.DrawPoint(v2, 4f, color);
                        }
                    }
                    break;

                case b2ChainShape chain:
                    {
                        int count = chain.m_count;
                        b2Vec2[] vertices = chain.m_vertices;

                        b2Vec2 v1 = Math.Mul(xf, vertices[0]);
                        m_debugDraw.DrawPoint(v1, 4.0f, color);

                        for (var i = 1; i < count; ++i)
                        {
                            b2Vec2 v2 = Math.Mul(xf, vertices[i]);
                            m_debugDraw.DrawSegment(v1, v2, color);
                            v1 = v2;
                        }
                    }
                    break;

                case b2PolygonShape poly:
                    {
                        int vertexCount = poly.m_count;
                        var vertices = new Vec2[b2Settings.MaxPolygonVertices];

                        for (var i = 0; i < vertexCount; ++i)
                        {
                            vertices[i] = Math.Mul(xf, poly.m_vertices[i]);
                        }

                        m_debugDraw.DrawSolidPolygon(vertices, vertexCount, color);
                    }
                    break;
            }
        }
    }
}