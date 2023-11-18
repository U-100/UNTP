using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
    public interface INetworkPrefabFactory<out TNetworkBehaviour> where TNetworkBehaviour : NetworkBehaviour
    {
        TNetworkBehaviour prefab { get; }
        TNetworkBehaviour Create(ulong ownerClientId, float3 position, quaternion rotation);
    }
}
