using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Math = Box2D.Math;

namespace Box2D
{
    public class b2PulleyJoint : b2Joint
    {
        public static readonly float MinPulleyLength = 2.0f;
        private readonly float m_constant;

        private readonly b2Vec2 m_localAnchorA;
        private readonly b2Vec2 m_localAnchorB;
        private float m_impulse;

        private int m_indexA;
        private int m_indexB;
        private float m_invIA;
        private float m_invIB;
        private float m_invMassA;
        private float m_invMassB;
        private b2Vec2 m_localCenterA;
        private b2Vec2 m_localCenterB;
        private float m_mass;
        private b2Vec2 m_rA;
        private b2Vec2 m_rB;
        private b2Vec2 m_uA;
        private b2Vec2 m_uB;

        public b2PulleyJoint(b2PulleyJointDef def)
            : base(def)
        {
            GroundAnchorA = def.GroundAnchorA;
            GroundAnchorB = def.GroundAnchorB;
            m_localAnchorA = def.LocalAnchorA;
            m_localAnchorB = def.LocalAnchorB;

            LengthA = def.LengthA;
            LengthB = def.LengthB;

            Ratio = def.Ratio;

            m_constant = def.LengthA + Ratio * def.LengthB;

            m_impulse = 0.0f;
        }

        public override b2Vec2 GetAnchorA => m_bodyA.GetWorldPoint(m_localAnchorA);

        public override b2Vec2 GetAnchorB => m_bodyB.GetWorldPoint(m_localAnchorB);

        public b2Vec2 GroundAnchorA
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public b2Vec2 GroundAnchorB
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public float LengthA
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public float LengthB
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public float Ratio
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public override b2Vec2 GetReactionForce(float invDt) => invDt * m_impulse * m_uB;

        public override float GetReactionTorque(float inv_dt) => 0.0f;

        public override void InitVelocityConstraints(in SolverData data)
        {
            m_indexA = m_bodyA.m_islandIndex;
            m_indexB = m_bodyB.m_islandIndex;
            m_localCenterA = m_bodyA.m_sweep.localCenter;
            m_localCenterB = m_bodyB.m_sweep.localCenter;
            m_invMassA = m_bodyA.m_invMass;
            m_invMassB = m_bodyB.m_invMass;
            m_invIA = m_bodyA.m_invI;
            m_invIB = m_bodyB.m_invI;

            b2Vec2 cA = data.positions[m_indexA].c;
            float aA = data.positions[m_indexA].a;
            b2Vec2 vA = data.velocities[m_indexA].v;
            float wA = data.velocities[m_indexA].w;

            b2Vec2 cB = data.positions[m_indexB].c;
            float aB = data.positions[m_indexB].a;
            b2Vec2 vB = data.velocities[m_indexB].v;
            float wB = data.velocities[m_indexB].w;

            b2Rot qA = new b2Rot(aA), qB = new b2Rot(aB);

            m_rA = Math.Mul(qA, m_localAnchorA - m_localCenterA);
            m_rB = Math.Mul(qB, m_localAnchorB - m_localCenterB);

            m_uA = cA + m_rA - GroundAnchorA;
            m_uB = cB + m_rB - GroundAnchorB;

            float lengthA = m_uA.Length();
            float lengthB = m_uB.Length();

            if (lengthA > 10.0f * b2Settings.linearSlop)
            {
                m_uA *= 1.0f / lengthA;
            }
            else
            {
                m_uA = b2Vec2.Zero;
            }

            if (lengthB > 10.0f * b2Settings.linearSlop)
            {
                m_uB *= 1.0f / lengthB;
            }
            else
            {
                m_uB = b2Vec2.Zero;
            }

            float ruA = Vectex.Cross(m_rA, m_uA);
            float ruB = Vectex.Cross(m_rB, m_uB);

            float mA = m_invMassA + m_invIA * ruA * ruA;
            float mB = m_invMassB + m_invIB * ruB * ruB;

            m_mass = mA + Ratio * Ratio * mB;

            if (m_mass > 0.0f)
            {
                m_mass = 1.0f / m_mass;
            }

            if (data.step.warmStarting)
            {
                m_impulse *= data.step.dtRatio;

                b2Vec2 PA = -m_impulse * m_uA;
                b2Vec2 PB = -Ratio * m_impulse * m_uB;

                vA += m_invMassA * PA;
                wA += m_invIA * Vectex.Cross(m_rA, PA);
                vB += m_invMassB * PB;
                wB += m_invIB * Vectex.Cross(m_rB, PB);
            }
            else
            {
                m_impulse = 0.0f;
            }

            data.velocities[m_indexA].v = vA;
            data.velocities[m_indexA].w = wA;
            data.velocities[m_indexB].v = vB;
            data.velocities[m_indexB].w = wB;
        }

        public override void SolveVelocityConstraints(in SolverData data)
        {
            b2Vec2 vA = data.velocities[m_indexA].v;
            float wA = data.velocities[m_indexA].w;
            b2Vec2 vB = data.velocities[m_indexB].v;
            float wB = data.velocities[m_indexB].w;

            b2Vec2 vpA = vA + Vectex.Cross(wA, m_rA);
            b2Vec2 vpB = vB + Vectex.Cross(wB, m_rB);

            float Cdot = -b2Vec2.Dot(m_uA, vpA) - Ratio * b2Vec2.Dot(m_uB, vpB);
            float impulse = -m_mass * Cdot;
            m_impulse += impulse;

            b2Vec2 PA = -impulse * m_uA;
            b2Vec2 PB = -Ratio * impulse * m_uB;
            vA += m_invMassA * PA;
            wA += m_invIA * Vectex.Cross(m_rA, PA);
            vB += m_invMassB * PB;
            wB += m_invIB * Vectex.Cross(m_rB, PB);

            data.velocities[m_indexA].v = vA;
            data.velocities[m_indexA].w = wA;
            data.velocities[m_indexB].v = vB;
            data.velocities[m_indexB].w = wB;
        }

        public override bool SolvePositionConstraints(in SolverData data)
        {
            b2Vec2 cA = data.positions[m_indexA].c;
            float aA = data.positions[m_indexA].a;
            b2Vec2 cB = data.positions[m_indexB].c;
            float aB = data.positions[m_indexB].a;

            b2Rot qA = new b2Rot(aA), qB = new b2Rot(aB);

            b2Vec2 rA = Math.Mul(qA, m_localAnchorA - m_localCenterA);
            b2Vec2 rB = Math.Mul(qB, m_localAnchorB - m_localCenterB);

            b2Vec2 uA = cA + rA - GroundAnchorA;
            b2Vec2 uB = cB + rB - GroundAnchorB;

            float lengthA = uA.Length();
            float lengthB = uB.Length();

            if (lengthA > 10.0f * b2Settings.linearSlop)
            {
                uA *= 1.0f / lengthA;
            }
            else
            {
                uA = b2Vec2.Zero;
            }

            if (lengthB > 10.0f * b2Settings.linearSlop)
            {
                uB *= 1.0f / lengthB;
            }
            else
            {
                uB = b2Vec2.Zero;
            }

            float ruA = Vectex.Cross(rA, uA);
            float ruB = Vectex.Cross(rB, uB);

            float mA = m_invMassA + m_invIA * ruA * ruA;
            float mB = m_invMassB + m_invIB * ruB * ruB;

            float mass = mA + Ratio * Ratio * mB;

            if (mass > 0.0f)
            {
                mass = 1.0f / mass;
            }

            float C = m_constant - lengthA - Ratio * lengthB;
            float linearError = MathF.Abs(C);

            float impulse = -mass * C;

            b2Vec2 PA = -impulse * uA;
            b2Vec2 PB = -Ratio * impulse * uB;

            cA += m_invMassA * PA;
            aA += m_invIA * Vectex.Cross(rA, PA);
            cB += m_invMassB * PB;
            aB += m_invIB * Vectex.Cross(rB, PB);

            data.positions[m_indexA].c = cA;
            data.positions[m_indexA].a = aA;
            data.positions[m_indexB].c = cB;
            data.positions[m_indexB].a = aB;

            return linearError < b2Settings.linearSlop;
        }
    }
}