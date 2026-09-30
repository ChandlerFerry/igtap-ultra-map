using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Math = Box2D.Math;

namespace Box2D
{
    public class b2RevoluteJoint : b2Joint, IMotorisedJoint
    {
        public readonly float m_referenceAngle;
        private float m_angle;
        private float m_axialMass;

        private b2Vec2 m_impulse;

        private int m_indexA;
        private int m_indexB;
        private float m_invIA;
        private float m_invIB;
        private float m_invMassA;
        private float m_invMassB;
        private Matrix3x2 m_K;
        public b2Vec2 m_localAnchorA;
        public b2Vec2 m_localAnchorB;
        private b2Vec2 m_localCenterA;
        private b2Vec2 m_localCenterB;
        private float m_lowerImpulse;
        private float m_maxMotorTorque;
        private float m_motorSpeed;
        private b2Vec2 m_rA;
        private b2Vec2 m_rB;
        private float m_upperImpulse;

        public b2RevoluteJoint(b2RevoluteJointDef def)
            : base(def)
        {
            m_localAnchorA = def.localAnchorA;
            m_localAnchorB = def.localAnchorB;
            m_referenceAngle = def.referenceAngle;

            m_impulse = b2Vec2.Zero;
            m_axialMass = 0.0f;
            MotorTorque = 0.0f;
            m_lowerImpulse = 0.0f;
            m_upperImpulse = 0.0f;

            LowerLimit = def.lowerAngle;
            UpperLimit = def.upperAngle;
            m_maxMotorTorque = def.maxMotorTorque;
            m_motorSpeed = def.motorSpeed;
            IsLimitEnabled = def.enableLimit;
            IsMotorEnabled = def.enableMotor;

            m_angle = 0.0f;
        }

        public override b2Vec2 GetAnchorA => m_bodyA.GetWorldPoint(m_localAnchorA);

        public override b2Vec2 GetAnchorB => m_bodyB.GetWorldPoint(m_localAnchorB);

        public float JointAngle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                b2Body b1 = m_bodyA;
                b2Body b2 = m_bodyB;
                return b2.m_sweep.a - b1.m_sweep.a - m_referenceAngle;
            }
        }

        public float JointSpeed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                b2Body b1 = m_bodyA;
                b2Body b2 = m_bodyB;
                return b2.m_angularVelocity - b1.m_angularVelocity;
            }
        }

        public bool IsLimitEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        public float LowerLimit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        public float UpperLimit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        public bool IsMotorEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        public float MotorTorque
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMotorSpeed() => m_motorSpeed;

        public float MotorSpeed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_motorSpeed;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetMotorSpeed(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMotorSpeed(float speed)
        {
            m_motorSpeed = speed;
            m_bodyA.SetAwake(true);
            m_bodyB.SetAwake(true);
            m_motorSpeed = speed;
        }

        public override b2Vec2 GetReactionForce(float invDt) => invDt * new b2Vec2(m_impulse.X, m_impulse.Y);

        public override float GetReactionTorque(float inv_dt) => inv_dt * (m_lowerImpulse + m_upperImpulse);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnableLimit(bool flag)
        {
            m_bodyA.SetAwake(true);
            m_bodyB.SetAwake(true);
            IsLimitEnabled = flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLimits(float lower, float upper)
        {
            m_bodyA.SetAwake(true);
            m_bodyB.SetAwake(true);
            LowerLimit = lower;
            UpperLimit = upper;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnableMotor(bool flag)
        {
            m_bodyA.SetAwake(true);
            m_bodyB.SetAwake(true);
            IsMotorEnabled = flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMaxMotorTorque(float torque)
        {
            m_bodyA.SetAwake(true);
            m_bodyB.SetAwake(true);
            m_maxMotorTorque = torque;
        }

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

            float aA = data.positions[m_indexA].a;
            b2Vec2 vA = data.velocities[m_indexA].v;
            float wA = data.velocities[m_indexA].w;

            float aB = data.positions[m_indexB].a;
            b2Vec2 vB = data.velocities[m_indexB].v;
            float wB = data.velocities[m_indexB].w;

            b2Rot qA = new b2Rot(aA), qB = new b2Rot(aB);

            m_rA = Math.Mul(qA, m_localAnchorA - m_localCenterA);
            m_rB = Math.Mul(qB, m_localAnchorB - m_localCenterB);

            float mA = m_invMassA, mB = m_invMassB;
            float iA = m_invIA, iB = m_invIB;

            m_K.M11 = mA + mB + m_rA.Y * m_rA.Y * iA + m_rB.Y * m_rB.Y * iB;
            m_K.M12 = -m_rA.Y * m_rA.X * iA - m_rB.Y * m_rB.X * iB;
            m_K.M21 = m_K.M12;
            m_K.M22 = mA + mB + m_rA.X * m_rA.X * iA + m_rB.X * m_rB.X * iB;

            m_axialMass = iA + iB;
            var fixedRotation = false;
            if (m_axialMass > 0f)
            {
                m_axialMass = 1f / m_axialMass;
                fixedRotation = true;
            }

            if (IsMotorEnabled == false || fixedRotation)
            {
                MotorTorque = 0.0f;
            }
            else
            {
                fixedRotation = true;
            }

            m_angle = aB - aA - m_referenceAngle;
            if (IsLimitEnabled == false || fixedRotation)
            {
                m_lowerImpulse = 0.0f;
                m_upperImpulse = 0.0f;
            }

            if (IsMotorEnabled == false || fixedRotation)
            {
                MotorTorque = 0.0f;
            }

            if (data.step.warmStarting)
            {
                m_impulse *= data.step.dtRatio;
                MotorTorque *= data.step.dtRatio;
                m_lowerImpulse *= data.step.dtRatio;
                m_upperImpulse *= data.step.dtRatio;

                float axialImpulse = MotorTorque + m_lowerImpulse - m_upperImpulse;

                var P = new b2Vec2(m_impulse.X, m_impulse.Y);

                vA -= mA * P;
                wA -= iA * (Vectex.Cross(m_rA, P) + axialImpulse);

                vB += mB * P;
                wB += iB * (Vectex.Cross(m_rB, P) + axialImpulse);
            }
            else
            {
                m_impulse = b2Vec2.Zero;
                MotorTorque = 0.0f;
                m_lowerImpulse = 0.0f;
                m_upperImpulse = 0.0f;
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

            float mA = m_invMassA, mB = m_invMassB;
            float iA = m_invIA, iB = m_invIB;

            bool fixedRotation = iA + iB == 0.0f;

            if (IsMotorEnabled && fixedRotation == false)
            {
                float Cdot = wB - wA - m_motorSpeed;
                float impulse = -m_axialMass * Cdot;
                float oldImpulse = MotorTorque;
                float maxImpulse = data.step.dt * m_maxMotorTorque;
                MotorTorque = Math.Clamp(MotorTorque + impulse, -maxImpulse, maxImpulse);
                impulse = MotorTorque - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            if (IsLimitEnabled && fixedRotation == false)
            {
                {
                    float C = m_angle - LowerLimit;
                    float Cdot = wB - wA;
                    float impulse = -m_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.step.inv_dt);
                    float oldImpulse = m_lowerImpulse;
                    m_lowerImpulse = MathF.Max(m_lowerImpulse + impulse, 0.0f);
                    impulse = m_lowerImpulse - oldImpulse;

                    wA -= iA * impulse;
                    wB += iB * impulse;
                }

                {
                    float C = UpperLimit - m_angle;
                    float Cdot = wA - wB;
                    float impulse = -m_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.step.inv_dt);
                    float oldImpulse = m_upperImpulse;
                    m_upperImpulse = MathF.Max(m_upperImpulse + impulse, 0.0f);
                    impulse = m_upperImpulse - oldImpulse;

                    wA += iA * impulse;
                    wB -= iB * impulse;
                }
            }

            {
                b2Vec2 Cdot = vB + Vectex.Cross(wB, m_rB) - vA - Vectex.Cross(wA, m_rA);
                b2Vec2 impulse = m_K.Solve(-Cdot);

                m_impulse.X += impulse.X;
                m_impulse.Y += impulse.Y;
                vA -= mA * impulse;
                wA -= iA * Vectex.Cross(m_rA, impulse);
                vB += mB * impulse;
                wB += iB * Vectex.Cross(m_rB, impulse);
            }

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

            var angularError = 0.0f;
            var positionError = 0.0f;

            bool fixedRotation = m_invIA + m_invIB == 0.0f;

            if (IsLimitEnabled && fixedRotation == false)
            {
                float angle = aB - aA - m_referenceAngle;
                var C = 0.0f;

                if (MathF.Abs(UpperLimit - LowerLimit) < 2.0f * b2Settings.angularSlop)
                {
                    C = Math.Clamp(angle - LowerLimit, -b2Settings.maxAngularCorrection, b2Settings.maxAngularCorrection);
                }
                else if (angle < LowerLimit)
                {
                    C = Math.Clamp(angle - LowerLimit + b2Settings.angularSlop, -b2Settings.maxAngularCorrection, 0.0f);
                }
                else if (angle >= UpperLimit)
                {
                    C = Math.Clamp(angle - UpperLimit - b2Settings.angularSlop, 0.0f, b2Settings.maxAngularCorrection);
                }

                float limitImpulse = -m_axialMass * C;
                aA -= m_invIA * limitImpulse;
                aB += m_invIB * limitImpulse;
                angularError = MathF.Abs(C);
            }

            {
                qA.Set(aA);
                qB.Set(aB);
                b2Vec2 rA = Math.Mul(qA, m_localAnchorA - m_localCenterA);
                b2Vec2 rB = Math.Mul(qB, m_localAnchorB - m_localCenterB);

                b2Vec2 C = cB + rB - cA - rA;
                positionError = C.Length();

                float mA = m_invMassA, mB = m_invMassB;
                float iA = m_invIA, iB = m_invIB;

                var K = new Matrix3x2();
                K.M11 = mA + mB + iA * rA.Y * rA.Y + iB * rB.Y * rB.Y;
                K.M21 = -iA * rA.X * rA.Y - iB * rB.X * rB.Y;
                K.M12 = K.M21;
                K.M22 = mA + mB + iA * rA.X * rA.X + iB * rB.X * rB.X;

                b2Vec2 impulse = -K.Solve(C);

                cA -= mA * impulse;
                aA -= iA * Vectex.Cross(rA, impulse);

                cB += mB * impulse;
                aB += iB * Vectex.Cross(rB, impulse);
            }

            data.positions[m_indexA].c = cA;
            data.positions[m_indexA].a = aA;
            data.positions[m_indexB].c = cB;
            data.positions[m_indexB].a = aB;

            return positionError <= b2Settings.linearSlop && angularError <= b2Settings.angularSlop;
        }
    }
}