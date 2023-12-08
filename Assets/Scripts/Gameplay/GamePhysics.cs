using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace UNTP
{
    public class GamePhysics : IGamePhysics
    {
        public bool CheckBox(float3 center, float3 halfExtents) => Physics.CheckBox(center, halfExtents);

        public bool CastSphere(float3 position, float radius, float3 movementDirection, out CastHit castHit, float movementDistance)
        {
            bool result = Physics.SphereCast(position, radius, movementDirection, out RaycastHit raycastHit, movementDistance);
            castHit.distance = raycastHit.distance;
            castHit.normal = raycastHit.normal;
            return result;
        }

        public bool CastRay(float3 position, float3 target, out CastHit castHit)
        {
            float3 direction = target - position;
            bool result = Physics.Raycast(position, Vector3.Normalize(direction), out RaycastHit raycastHit, length(direction));
            castHit.distance = raycastHit.distance;
            castHit.normal = raycastHit.normal;
            return result;
        }
    }
}
