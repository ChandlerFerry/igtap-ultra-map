using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
    public class b2DistanceJoint : b2Joint
    {
        private readonly float m_length;
        private readonly b2Vec2 m_localAnchorA;
        private readonly b2Vec2 m_localAnchorB;
        private float m_bias;
        private float m_gamma;
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
        private b2Vec2 m_u;

        public b2DistanceJoint(b2DistanceJointDef def)
            : base(def)
        {
            m_localAnchorA = def.localAnchorA;
            m_localAnchorB = def.localAnchorB;
            m_length = def.length;

            if (def.frequencyHz.HasValue && def.dampingRatio.HasValue)
            {
                LinearStiffness(out def.stiffness, out def.damping, def.frequencyHz.Value, def.dampingRatio.Value, def.bodyA,
                                def.bodyB);
            }

            Stiffness = def.stiffness;
            Damping = def.damping;
            m_impulse = 0.0f;
            m_gamma = 0.0f;
            m_bias = 0.0f;
        }

        public override b2Vec2 GetAnchorA => m_bodyA.GetWorldPoint(m_localAnchorA);

        public override b2Vec2 GetAnchorB => m_bodyB.GetWorldPoint(m_localAnchorB);

        public float Stiffness
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }

        public float Damping
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override b2Vec2 GetReactionForce(float inv_dt) => inv_dt * m_impulse * m_u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            m_u = cB + m_rB - cA - m_rA;

            float length = m_u.Length();
            if (length > b2Settings.linearSlop)
            {
                m_u *= 1.0f / length;
            }
            else
            {
                m_u = b2Vec2.Zero;
            }

            float crAu = Vectex.Cross(m_rA, m_u);
            float crBu = Vectex.Cross(m_rB, m_u);
            float invMass = m_invMassA + m_invIA * crAu * crAu + m_invMassB + m_invIB * crBu * crBu;

            if (Stiffness > 0.0f)
            {
                float C = length - m_length;

                float d = Damping;

                float k = Stiffness;

                float h = data.step.dt;

                m_gamma = h * (d + h * k);
                m_gamma = m_gamma != 0.0f ? 1.0f / m_gamma : 0.0f;
                m_bias = C * h * k * m_gamma;

                invMass += m_gamma;
                m_mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;
            }
            else
            {
                m_gamma = 0.0f;
                m_bias = 0.0f;
                m_mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;
            }

            if (data.step.warmStarting)
            {
                m_impulse *= data.step.dtRatio;

                b2Vec2 P = m_impulse * m_u;
                vA -= m_invMassA * P;
                wA -= m_invIA * Vectex.Cross(m_rA, P);
                vB += m_invMassB * P;
                wB += m_invIB * Vectex.Cross(m_rB, P);
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
            float Cdot = b2Vec2.Dot(m_u, vpB - vpA);

            float impulse = -m_mass * (Cdot + m_bias + m_gamma * m_impulse);
            m_impulse += impulse;

            b2Vec2 P = impulse * m_u;
            vA -= m_invMassA * P;
            wA -= m_invIA * Vectex.Cross(m_rA, P);
            vB += m_invMassB * P;
            wB += m_invIB * Vectex.Cross(m_rB, P);

            data.velocities[m_indexA].v = vA;
            data.velocities[m_indexA].w = wA;
            data.velocities[m_indexB].v = vB;
            data.velocities[m_indexB].w = wB;
        }

        public override bool SolvePositionConstraints(in SolverData data)
        {
            if (Stiffness > 0.0f)
            {
                return true;
            }

            b2Vec2 cA = data.positions[m_indexA].c;
            float aA = data.positions[m_indexA].a;
            b2Vec2 cB = data.positions[m_indexB].c;
            float aB = data.positions[m_indexB].a;

            b2Rot qA = new b2Rot(aA), qB = new b2Rot(aB);

            b2Vec2 rA = Math.Mul(qA, m_localAnchorA - m_localCenterA);
            b2Vec2 rB = Math.Mul(qB, m_localAnchorB - m_localCenterB);
            b2Vec2 u = cB + rB - cA - rA;

            float length = u.Length();
            u = b2Vec2.Normalize(u);
            float C = length - m_length;
            C = Math.Clamp(C, -b2Settings.maxLinearCorrection, b2Settings.maxLinearCorrection);

            float impulse = -m_mass * C;
            b2Vec2 P = impulse * u;

            cA -= m_invMassA * P;
            aA -= m_invIA * Vectex.Cross(rA, P);
            cB += m_invMassB * P;
            aB += m_invIB * Vectex.Cross(rB, P);

            data.positions[m_indexA].c = cA;
            data.positions[m_indexA].a = aA;
            data.positions[m_indexB].c = cB;
            data.positions[m_indexB].a = aB;

            return Math.Abs(C) < b2Settings.linearSlop;
        }
    }
}