using System.Numerics;

namespace Box2D
{
    public class b2MouseJoint : b2Joint
    {
        private readonly float m_dampingRatio;
        private readonly float m_frequencyHz;
        private readonly b2Vec2 m_localAnchor;
        private readonly float m_maxForce;
        private float m_beta;
        private b2Vec2 m_C;
        private float m_gamma;
        private b2Vec2 m_impulse;
        private int m_indexB;
        private float m_invIB;
        private float m_invMassB;
        private b2Vec2 m_localCenterB;
        private Matrix3x2 m_mass;
        private b2Vec2 m_rB;
        private b2Vec2 m_targetA;

        public b2MouseJoint(b2MouseJointDef def)
            : base(def)
        {
            m_targetA = def.Target;
            m_localAnchor = Math.MulT(m_bodyB.GetTransform(), m_targetA);

            m_maxForce = def.MaxForce;
            m_impulse = b2Vec2.Zero;

            m_frequencyHz = def.FrequencyHz;
            m_dampingRatio = def.DampingRatio;

            m_beta = 0.0f;
            m_gamma = 0.0f;
        }

        public override b2Vec2 GetAnchorA => m_targetA;

        public override b2Vec2 GetAnchorB => m_bodyB.GetWorldPoint(m_localAnchor);

        public override b2Vec2 GetReactionForce(float inv_dt) => inv_dt * m_impulse;

        public override float GetReactionTorque(float inv_dt) => inv_dt * 0.0f;

        public void SetTarget(b2Vec2 target)
        {
            if (!m_bodyB.IsAwake())
            {
                m_bodyB.SetAwake(true);
            }

            m_targetA = target;
        }

        public override void InitVelocityConstraints(in SolverData data)
        {
            m_indexB = m_bodyB.m_islandIndex;
            m_localCenterB = m_bodyB.m_sweep.localCenter;
            m_invMassB = m_bodyB.m_invMass;
            m_invIB = m_bodyB.m_invI;

            b2Vec2 cB = data.positions[m_indexB].c;
            float aB = data.positions[m_indexB].a;
            b2Vec2 vB = data.velocities[m_indexB].v;
            float wB = data.velocities[m_indexB].w;

            var qB = new b2Rot(aB);

            float mass = m_bodyB.GetMass();

            float omega = b2Settings.Tau * m_frequencyHz;

            float d = 2.0f * mass * m_dampingRatio * omega;

            float k = mass * (omega * omega);

            float h = data.step.dt;
            m_gamma = h * (d + h * k);
            if (m_gamma != 0.0f)
            {
                m_gamma = 1.0f / m_gamma;
            }

            m_beta = h * k * m_gamma;

            m_rB = Math.Mul(qB, -m_localCenterB);

            var K = new Matrix3x2();
            K.M11 = m_invMassB + m_invIB * m_rB.Y * m_rB.Y + m_gamma;
            K.M21 = -m_invIB * m_rB.X * m_rB.Y;
            K.M12 = K.M21;
            K.M22 = m_invMassB + m_invIB * m_rB.X * m_rB.Y + m_gamma;

            Matrex.Invert(K, out m_mass);

            m_C = cB + m_rB - m_targetA;
            m_C *= m_beta;

            wB *= 0.98f;

            if (data.step.warmStarting)
            {
                m_impulse *= data.step.dtRatio;
                vB += m_invMassB * m_impulse;
                wB += m_invIB * Vectex.Cross(m_rB, m_impulse);
            }
            else
            {
                m_impulse = b2Vec2.Zero;
            }

            data.velocities[m_indexB].v = vB;
            data.velocities[m_indexB].w = wB;
        }

        public override void SolveVelocityConstraints(in SolverData data)
        {
            b2Vec2 vB = data.velocities[m_indexB].v;
            float wB = data.velocities[m_indexB].w;

            b2Vec2 Cdot = vB + Vectex.Cross(wB, m_rB);
            var impulse =
                b2Vec2.Transform(-(Cdot + m_C + m_gamma * m_impulse),
                                  m_mass);

            b2Vec2 oldImpulse = m_impulse;
            m_impulse += impulse;
            float maxImpulse = data.step.dt * m_maxForce;
            if (m_impulse.LengthSquared() > maxImpulse * maxImpulse)
            {
                m_impulse *= maxImpulse / m_impulse.Length();
            }

            impulse = m_impulse - oldImpulse;

            vB += m_invMassB * impulse;
            wB += m_invIB * Vectex.Cross(m_rB, impulse);

            data.velocities[m_indexB].v = vB;
            data.velocities[m_indexB].w = wB;
        }

        public override bool SolvePositionConstraints(in SolverData data) => true;
    }
}