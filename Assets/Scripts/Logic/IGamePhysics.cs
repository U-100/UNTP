using Unity.Mathematics;

namespace UNTP
{
	public struct CastHit
	{
		public float distance;
		public float3 normal;
	}
	
	public interface IGamePhysics
	{
		bool CheckBox(float3 center, float3 halfExtents);
		bool CastSphere(float3 position, float radius, float3 movementDirection, out CastHit castHit, float movementDistance);
		bool CastRay(float3 position, float3 target, out CastHit castHit);
	}
}
