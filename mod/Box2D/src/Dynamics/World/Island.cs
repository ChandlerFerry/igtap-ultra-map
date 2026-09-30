using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Math = System.Math;

namespace Box2D
{
    public class b2Island
    {
        public readonly int m_bodyCapacity;
        public readonly int m_contactCapacity;
        private readonly b2Contact[] m_contacts;
        private readonly b2Joint[] m_joints;
        private readonly b2ContactListener m_listener;

        private readonly b2Position[] m_positions;
        private readonly b2Velocity[] m_velocities;

        public b2Body[] m_bodies;

        public int m_bodyCount;
        public int m_contactCount;
        private int m_jointCount;

        public b2Island(int bodyCapacity, int contactCapacity, int jointCapacity, b2ContactListener listener)
        {
            m_bodyCapacity = bodyCapacity;
            m_contactCapacity = contactCapacity;

            m_listener = listener;

            m_bodies = new b2Body[bodyCapacity];
            m_contacts = new b2Contact[contactCapacity];
            m_joints = new b2Joint[jointCapacity];

            m_velocities = new b2Velocity[m_bodyCapacity];
            m_positions = new b2Position[m_bodyCapacity];
        }

        public bool Fits(int bodyCapacity, int contactCapacity, int jointCapacity, b2ContactListener listener)
            => m_bodyCapacity >= bodyCapacity && m_contactCapacity >= contactCapacity && m_joints.Length >= jointCapacity
            && m_listener == listener;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            m_bodyCount = 0;
            m_contactCount = 0;
            m_jointCount = 0;
        }

        public void Solve(in b2TimeStep step, in b2Vec2 gravity, bool allowSleep)
        {
            float h = step.dt;
            for (var i = 0; i < m_bodyCount; ++i)
            {
                b2Body b = m_bodies[i];

                b2Vec2 c = b.m_sweep.c;
                float a = b.m_sweep.a;
                b2Vec2 v = b.m_linearVelocity;
                float w = b.m_angularVelocity;

                b.m_sweep.c0 = b.m_sweep.c;
                b.m_sweep.a0 = b.m_sweep.a;

                if (b.m_type == b2BodyType.Dynamic)
                {
                    v += b2Settings.useLegacyGravityIntegration
                        ? h * (b.m_gravityScale * gravity + b.m_invMass * b.m_force)
                        : h * b.m_invMass * (b.m_gravityScale * b.m_mass * gravity + b.m_force);
                    w += h * b.m_invI * b.m_torque;

                    v *= 1.0f / (1.0f + h * b.m_linearDamping);
                    w *= 1.0f / (1.0f + h * b.m_angularDamping);
                }

                m_positions[i].c = c;
                m_positions[i].a = a;
                m_velocities[i].v = v;
                m_velocities[i].w = w;
            }

            var solverData = new SolverData();
            solverData.step = step;
            solverData.positions = m_positions;
            solverData.velocities = m_velocities;

            ContactSolverDef contactSolverDef;
            contactSolverDef.step = step;
            contactSolverDef.contacts = m_contacts;
            contactSolverDef.count = m_contactCount;
            contactSolverDef.positions = m_positions;
            contactSolverDef.velocities = m_velocities;

            var contactSolver = new ContactSolver(contactSolverDef);
            contactSolver.InitializeVelocityConstraints();

            if (step.warmStarting)
            {
                contactSolver.WarmStart();
            }

            for (var i = 0; i < m_jointCount; ++i)
            {
                m_joints[i].InitVelocityConstraints(solverData);
            }

            for (var i = 0; i < step.velocityIterations; ++i)
            {
                for (var j = 0; j < m_jointCount; ++j)
                {
                    m_joints[j].SolveVelocityConstraints(solverData);
                }

                contactSolver.SolveVelocityConstraints();
            }

            contactSolver.StoreImpulses();

            for (var i = 0; i < m_bodyCount; ++i)
            {
                b2Vec2 c = m_positions[i].c;
                float a = m_positions[i].a;
                b2Vec2 v = m_velocities[i].v;
                float w = m_velocities[i].w;

                b2Vec2 translation = h * v;
                if (b2Vec2.Dot(translation, translation) > b2Settings.maxTranslationSquared)
                {
                    float ratio = b2Settings.maxTranslation / translation.Length();
                    v *= ratio;
                }

                float rotation = h * w;
                if (rotation * rotation > b2Settings.maxRotationSquared)
                {
                    float ratio = b2Settings.maxRotation / MathF.Abs(rotation);
                    w *= ratio;
                }

                c += h * v;
                a += h * w;

                m_positions[i].c = c;
                m_positions[i].a = a;
                m_velocities[i].v = v;
                m_velocities[i].w = w;
            }

            var positionSolved = false;
            for (var i = 0; i < step.positionIterations; ++i)
            {
                bool contactsOkay = contactSolver.SolvePositionConstraints();

                var jointsOkay = true;
                for (var j = 0; j < m_jointCount; ++j)
                {
                    bool jointOkay = m_joints[j].SolvePositionConstraints(solverData);
                    jointsOkay = jointsOkay && jointOkay;
                }

                if (contactsOkay && jointsOkay)
                {
                    positionSolved = true;
                    break;
                }
            }

            for (var i = 0; i < m_bodyCount; ++i)
            {
                b2Body body = m_bodies[i];
                body.m_sweep.c = m_positions[i].c;
                body.m_sweep.a = m_positions[i].a;
                body.m_linearVelocity = m_velocities[i].v;
                body.m_angularVelocity = m_velocities[i].w;
                body.SynchronizeTransform();
            }

            Report(contactSolver._velocityConstraints);

            if (allowSleep)
            {
                float minSleepTime = float.MaxValue;

                float linTolSqr = b2Settings.linearSleepTolerance * b2Settings.linearSleepTolerance;
                float angTolSqr = b2Settings.angularSleepTolerance * b2Settings.angularSleepTolerance;

                for (var i = 0; i < m_bodyCount; ++i)
                {
                    b2Body b = m_bodies[i];
                    if (b.Type() == b2BodyType.Static)
                    {
                        continue;
                    }

                    if (!b.HasFlag(b2BodyFlags.AutoSleep) ||
                        b.m_angularVelocity * b.m_angularVelocity > angTolSqr ||
                        b2Vec2.Dot(b.m_linearVelocity, b.m_linearVelocity) > linTolSqr)
                    {
                        b.m_sleepTime = 0.0f;
                        minSleepTime = 0.0f;
                    }
                    else
                    {
                        b.m_sleepTime += h;
                        minSleepTime = Math.Min(minSleepTime, b.m_sleepTime);
                    }
                }

                if (minSleepTime >= b2Settings.timeToSleep && positionSolved)
                {
                    for (var i = 0; i < m_bodyCount; ++i)
                    {
                        b2Body b = m_bodies[i];
                        b.SetAwake(false);
                    }
                }
            }
        }

        public void SolveTOI(in b2TimeStep subStep, int toiIndexA, int toiIndexB)
        {
            for (var i = 0; i < m_bodyCount; i++)
            {
                b2Body b = m_bodies[i];
                m_positions[i].c = b.m_sweep.c;
                m_positions[i].a = b.m_sweep.a;
                m_velocities[i].v = b.m_linearVelocity;
                m_velocities[i].w = b.m_angularVelocity;
            }

            var contactSolverDef = new ContactSolverDef();
            contactSolverDef.contacts = m_contacts;
            contactSolverDef.count = m_contactCount;
            contactSolverDef.step = subStep;
            contactSolverDef.positions = m_positions;
            contactSolverDef.velocities = m_velocities;

            var contactSolver = new ContactSolver(contactSolverDef);

            for (var i = 0; i < subStep.positionIterations; ++i)
            {
                if (contactSolver.SolveTOIPositionConstraints(toiIndexA, toiIndexB))
                {
                    break;
                }
            }

            m_bodies[toiIndexA].m_sweep.c0 = m_positions[toiIndexA].c;
            m_bodies[toiIndexA].m_sweep.a0 = m_positions[toiIndexA].a;
            m_bodies[toiIndexB].m_sweep.c0 = m_positions[toiIndexB].c;
            m_bodies[toiIndexB].m_sweep.a0 = m_positions[toiIndexB].a;

            contactSolver.InitializeVelocityConstraints();

            for (var i = 0; i < subStep.velocityIterations; ++i)
            {
                contactSolver.SolveVelocityConstraints();
            }

            float h = subStep.dt;

            for (var i = 0; i < m_bodyCount; ++i)
            {
                b2Vec2 c = m_positions[i].c;
                float a = m_positions[i].a;
                b2Vec2 v = m_velocities[i].v;
                float w = m_velocities[i].w;

                b2Vec2 translation = h * v;
                if (b2Vec2.Dot(translation, translation) > b2Settings.maxTranslationSquared)
                {
                    float ratio = b2Settings.maxTranslation / translation.Length();
                    v *= ratio;
                }

                float rotation = h * w;
                if (rotation * rotation > b2Settings.maxRotationSquared)
                {
                    float ratio = b2Settings.maxRotation / MathF.Abs(rotation);
                    w *= ratio;
                }

                c += h * v;
                a += h * w;

                m_positions[i].c = c;
                m_positions[i].a = a;
                m_velocities[i].v = v;
                m_velocities[i].w = w;

                b2Body body = m_bodies[i];
                body.m_sweep.c = c;
                body.m_sweep.a = a;
                body.m_linearVelocity = v;
                body.m_angularVelocity = w;
                body.SynchronizeTransform();
            }

            Report(contactSolver._velocityConstraints);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(b2Body body)
        {
            body.m_islandIndex = m_bodyCount;
            m_bodies[m_bodyCount++] = body;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(b2Contact contact)
        {
            m_contacts[m_contactCount++] = contact;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(b2Joint joint)
        {
            m_joints[m_jointCount++] = joint;
        }

        public void Report(ContactVelocityConstraint[] constraints)
        {
            if (m_listener == null)
            {
                return;
            }

            for (var i = 0; i < m_contactCount; ++i)
            {
                b2Contact c = m_contacts[i];
                ContactVelocityConstraint cc = constraints[i];
                var impulse = new b2ContactImpulse();
                for (var j = 0; j < cc.pointCount; ++j)
                {
                    impulse.normalImpulses[j] = cc.points[j].normalImpulse;
                    impulse.tangentImpulses[j] = cc.points[j].tangentImpulse;
                }

                m_listener.PostSolve(c, impulse);
            }
        }
    }
}