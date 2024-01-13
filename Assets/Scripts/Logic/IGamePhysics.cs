using System;
using Unity.Mathematics;

namespace UNTP
{
	public static class LayerMask
	{
		public const int ALL = DEFAULT | PLAYER | ENEMY;

		public const int DEFAULT = 1;
		public const int PLAYER = 1 << 21;
		public const int ENEMY = 1 << 22;
	}
	
	public struct CastHit
	{
		public float distance;
		public float3 normal;
	}
	
	public interface IGamePhysics
	{
		bool CheckBox(float3 center, float3 halfExtents, int layerMask);
		bool CastSphere(float3 position, float radius, float3 movementDirection, float movementDistance, int layerMask, out CastHit castHit);
		bool CastRay(float3 position, float3 target, int layerMask, out CastHit castHit);
	}
}
