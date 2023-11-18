using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
    public class NetworkPrefabInstanceFactory<TNetworkBehaviour> : INetworkPrefabFactory<TNetworkBehaviour> where TNetworkBehaviour : NetworkBehaviour
    {
        private readonly InitializerDelegate _initializerDelegate;

        public delegate void InitializerDelegate(TNetworkBehaviour networkBehaviour, ulong ownerClientId, float3 position, quaternion rotation);

        public NetworkPrefabInstanceFactory(TNetworkBehaviour prefab, InitializerDelegate initializerDelegate = null)
        {
            this.prefab = prefab;
            this._initializerDelegate = initializerDelegate;
        }
		
        public TNetworkBehaviour prefab { get; }

        public TNetworkBehaviour Create(ulong ownerClientId, float3 position, quaternion rotation)
        {
            TNetworkBehaviour instance = UnityEngine.Object.Instantiate(this.prefab, position, rotation);
            this._initializerDelegate?.Invoke(instance, ownerClientId, position, rotation);
            return instance;
        }
    }
}
