using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
	public abstract class b2Joint
	{
		public readonly bool m_collideConnected;
		public readonly b2JointEdge m_edgeA = new b2JointEdge();
		public readonly b2JointEdge m_edgeB = new b2JointEdge();
		public b2Body m_bodyA;
		public b2Body m_bodyB;
		protected float m_invMass1, m_invI1;
		protected float m_invMass2, m_invI2;

		public bool m_islandFlag;

		protected b2Vec2 m_localCenter1, m_localCenter2;
		public b2Joint m_next;
		public b2Joint m_prev;

		protected b2Joint(b2JointDef def)
		{
			m_prev = null;
			m_next = null;
			m_bodyA = def.bodyA;
			m_bodyB = def.bodyB;
			m_collideConnected = def.collideConnected;
			m_islandFlag = false;
			UserData = def.UserData;
		}

		public abstract b2Vec2 GetAnchorA { get; }

		public abstract b2Vec2 GetAnchorB { get; }

		public object UserData
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public static void LinearStiffness(
			out float stiffness,
			out float damping,
			in float frequencyHz,
			in float dampingRatio,
			in b2Body bodyA,
			in b2Body bodyB)
		{
			float massA = bodyA.GetMass();
			float massB = bodyB.GetMass();
			float mass;

			if (massA > 0.0f && massB > 0.0f)
			{
				mass = massA * massB / (massA + massB);
			}
			else if (massA > 0.0f)
			{
				mass = massA;
			}
			else
			{
				mass = massB;
			}

			float omega = 2.0f * b2Settings.Pi * frequencyHz;
			stiffness = mass * omega * omega;
			damping = 2.0f * mass * dampingRatio * omega;
		}

		public static void AngularStiffness(
			out float stiffness,
			out float damping,
			in float frequencyHz,
			in float dampingRatio,
			in b2Body bodyA,
			in b2Body bodyB)
		{
			float IA = bodyA.GetInertia();
			float IB = bodyB.GetInertia();
			float I;

			if (IA > 0.0f && IB > 0.0f)
			{
				I = IA * IB / (IA + IB);
			}
			else if (IA > 0.0f)
			{
				I = IA;
			}
			else
			{
				I = IB;
			}

			float omega = 2.0f * b2Settings.Pi * frequencyHz;
			stiffness = I * omega * omega;
			damping = 2.0f * I * dampingRatio * omega;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public b2Body GetBodyA() => m_bodyA;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public b2Body GetBodyB() => m_bodyB;

		public abstract b2Vec2 GetReactionForce(float inv_dt);

		public abstract float GetReactionTorque(float inv_dt);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public b2Joint GetNext() => m_next;

		public static b2Joint Create(b2JointDef def)
		{
			return def switch
            {
                b2DistanceJointDef d  => new b2DistanceJoint(d),
                b2MouseJointDef d     => new b2MouseJoint(d),
                b2PrismaticJointDef d => new b2PrismaticJoint(d),
                b2RevoluteJointDef d  => new b2RevoluteJoint(d),
                b2PulleyJointDef d    => new b2PulleyJoint(d),
                b2GearJointDef d      => new b2GearJoint(d),
                b2WheelJointDef d     => new b2WheelJoint(d),
                b2WeldJointDef d      => new b2WeldJoint(d),
				b2FrictionJointDef d  => new b2FrictionJoint(d),
                _ => throw new NotImplementedException($"b2JointDef '{def.GetType().Name}' is not implemented.")
            };
		}

		public abstract void InitVelocityConstraints(in SolverData data);
		public abstract void SolveVelocityConstraints(in SolverData data);

		public abstract bool SolvePositionConstraints(in SolverData data);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ComputeXForm(ref b2Transform xf, b2Vec2 center, b2Vec2 localCenter, float angle)
		{
			xf.q = Matrex.CreateRotation(angle);
			xf.p = center - b2Vec2.Transform(localCenter, xf.q);
		}

		public void Draw(b2DebugDraw draw)
		{
			b2Transform xf1 = m_bodyA.GetTransform();
			b2Transform xf2 = m_bodyB.GetTransform();
			b2Vec2 x1 = xf1.p;
			b2Vec2 x2 = xf2.p;
			b2Vec2 p1 = GetAnchorA;
			b2Vec2 p2 = GetAnchorB;

			var color = new b2Color(0.5f, 0.8f, 0.8f);

			switch (this)
			{
				case b2DistanceJoint j:
					draw.DrawSegment(p1, p2, color);
					break;
				case b2PulleyJoint pulley: {
					b2Vec2 s1 = pulley.GroundAnchorA;
					b2Vec2 s2 = pulley.GroundAnchorB;
					draw.DrawSegment(s1, p1, color);
					draw.DrawSegment(s2, p2, color);
					draw.DrawSegment(s1, s2, color);
				}
					break;

				case b2MouseJoint j: {
					var c = new b2Color();
					c.Set(0.0f, 1.0f, 0.0f);
					draw.DrawPoint(p1, 4.0f, c);
					draw.DrawPoint(p2, 4.0f, c);

					c.Set(0.8f, 0.8f, 0.8f);
					draw.DrawSegment(p1, p2, c);
				}
					break;

				default:
					draw.DrawSegment(x1, p1, color);
					draw.DrawSegment(p1, p2, color);
					draw.DrawSegment(x2, p2, color);
					break;
			}
		}
	}
}