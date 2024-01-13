using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace UNTP
{
    public class GamePhysics : IGamePhysics
    {
        public bool CheckBox(float3 center, float3 halfExtents, int layerMask) => Physics.CheckBox(center, halfExtents, Quaternion.identity, Physics.AllLayers);

        public bool CastSphere(float3 position, float radius, float3 movementDirection, float movementDistance, int layerMask, out CastHit castHit)
        {
            bool result = Physics.SphereCast(position, radius, movementDirection, out RaycastHit raycastHit, movementDistance, layerMask);
            castHit.distance = raycastHit.distance;
            castHit.normal = raycastHit.normal;
            return result;
        }

        public bool CastRay(float3 position, float3 target, int layerMask, out CastHit castHit)
        {
            float3 direction = target - position;
            bool result = Physics.Raycast(position, Vector3.Normalize(direction), out RaycastHit raycastHit, length(direction), layerMask);
            castHit.distance = raycastHit.distance;
            castHit.normal = raycastHit.normal;
            return result;
        }
    }
}
