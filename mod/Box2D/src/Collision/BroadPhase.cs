#define ALLOWUNSAFE

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Box2D
{
	public partial class b2BroadPhase
	{
		private readonly b2DynamicTree m_tree;
		private int[] m_moveBuffer;
		private int m_moveCapacity;
		private int m_moveCount;
		private Pair[] m_pairBuffer;
		private int m_pairCapacity;
		private int m_pairCount;

		private int m_proxyCount;
		private int m_queryProxyId;
		private Func<int, bool> m_queryCallback;

		public b2BroadPhase()
		{
			m_proxyCount = 0;

			m_pairCapacity = 16;
			m_pairCount = 0;
			m_pairBuffer = new Pair[m_pairCapacity];

			m_moveCapacity = 16;
			m_moveCount = 0;
			m_moveBuffer = new int[m_moveCapacity];

			m_tree = new b2DynamicTree();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public object GetUserData(int proxyId) => m_tree.GetUserData(proxyId);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TestOverlap(int proxyIdA, int proxyIdB)
		{
			b2AABB aabbA = m_tree.GetFatAABB(proxyIdA);
			b2AABB aabbB = m_tree.GetFatAABB(proxyIdB);
			return Collision.TestOverlap(aabbA, aabbB);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public b2AABB GetFatAABB(int proxyId) => m_tree.GetFatAABB(proxyId);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetProxyCount() => m_proxyCount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetTreeHeight() => m_tree.Height;

		public void UpdatePairs(Action<object, object> AddPair)
		{
			m_pairCount = 0;

			for (var i = 0; i < m_moveCount; ++i)
			{
				m_queryProxyId = m_moveBuffer[i];
				if (m_queryProxyId == -1)
				{
					continue;
				}

				b2AABB fatAABB = m_tree.GetFatAABB(m_queryProxyId);

				m_tree.Query(m_queryCallback ??= QueryCallback, fatAABB);
			}

			Array.Sort(m_pairBuffer, 0, m_pairCount, PairLessThan.Instance);

			for (var i = 0; i < m_pairCount; ++i)
			{
				Pair primaryPair = m_pairBuffer[i];
				object userDataA = m_tree.GetUserData(primaryPair.proxyIdA);
				object userDataB = m_tree.GetUserData(primaryPair.proxyIdB);
				AddPair(userDataA, userDataB);
			}

			for (var i = 0; i < m_moveCount; ++i)
			{
				int proxyId = m_moveBuffer[i];
				if (proxyId == -1)
				{
					continue;
				}

				m_tree.ClearMoved(proxyId);
			}

			m_moveCount = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Query(Func<int, bool> queryCallback, in b2AABB aabb)
		{
			m_tree.Query(queryCallback, in aabb);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RayCast(Func<b2RayCastInput, int, float> RayCastCallback, in b2RayCastInput input)
		{
			m_tree.RayCast(RayCastCallback, in input);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ShiftOrigin(in b2Vec2 newOrigin)
		{
			m_tree.ShiftOrigin(in newOrigin);
		}

		public int CreateProxy(in b2AABB aabb, object userData)
		{
			int proxyId = m_tree.CreateProxy(aabb, userData);
			++m_proxyCount;
			BufferMove(proxyId);
			return proxyId;
		}

		public void DestroyProxy(int proxyId)
		{
			UnBufferMove(proxyId);
			--m_proxyCount;
			m_tree.DestroyProxy(proxyId);
		}

		public void MoveProxy(int proxyId, in b2AABB aabb, in b2Vec2 displacement)
		{
			bool buffer = m_tree.MoveProxy(proxyId, aabb, displacement);
			if (buffer)
			{
				BufferMove(proxyId);
			}
		}

		public void TouchProxy(int proxyId)
		{
			BufferMove(proxyId);
		}

		private void BufferMove(int proxyId)
		{
			if (m_moveCount == m_moveCapacity)
			{
				int[] oldBuffer = m_moveBuffer;
				m_moveCapacity *= 2;
				m_moveBuffer = new int[m_moveCapacity];
				Array.Copy(oldBuffer, m_moveBuffer, m_moveCount);
			}

			m_moveBuffer[m_moveCount] = proxyId;
			++m_moveCount;
		}

		private void UnBufferMove(int proxyId)
		{
			for (var i = 0; i < m_moveCount; ++i)
			{
				if (m_moveBuffer[i] == proxyId)
				{
					m_moveBuffer[i] = -1;
				}
			}
		}

		private bool QueryCallback(int proxyId)
		{
			if (proxyId == m_queryProxyId)
			{
				return true;
			}

			bool moved = m_tree.WasMoved(proxyId);
			if (moved && proxyId > m_queryProxyId)
			{
				return true;
			}

			if (m_pairCount == m_pairCapacity)
			{
				Pair[] oldBuffer = m_pairBuffer;
				m_pairCapacity = m_pairCapacity + (m_pairCapacity >> 1);
				m_pairBuffer = new Pair[m_pairCapacity];
				Array.Copy(oldBuffer, m_pairBuffer, m_pairCount);
			}

			m_pairBuffer[m_pairCount].proxyIdA = Math.Min(proxyId, m_queryProxyId);
			m_pairBuffer[m_pairCount].proxyIdB = Math.Max(proxyId, m_queryProxyId);
			++m_pairCount;

			return true;
		}

		sealed class PairLessThan : System.Collections.Generic.IComparer<Pair>
		{
			public static readonly PairLessThan Instance = new PairLessThan();

			public int Compare(Pair a, Pair b)
			{
				if (a.proxyIdA != b.proxyIdA) return a.proxyIdA < b.proxyIdA ? -1 : 1;
				return a.proxyIdB < b.proxyIdB ? -1 : a.proxyIdB > b.proxyIdB ? 1 : 0;
			}
		}
	}
}