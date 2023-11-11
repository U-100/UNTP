using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class DelegatingNetworkPrefabInstanceHandler : INetworkPrefabInstanceHandler
	{
		private readonly InstantiateDelegate _instantiateDelegate;
		private readonly DestroyDelegate _destroyDelegate;

		public delegate NetworkObject InstantiateDelegate(ulong ownerClientId, Vector3 position, Quaternion rotation);
		public delegate void DestroyDelegate(NetworkObject networkObject);

		public DelegatingNetworkPrefabInstanceHandler(InstantiateDelegate instantiateDelegate, DestroyDelegate destroyDelegate)
		{
			this._instantiateDelegate = instantiateDelegate;
			this._destroyDelegate = destroyDelegate;
		}

		public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) => this._instantiateDelegate(ownerClientId, position, rotation);
		public void Destroy(NetworkObject networkObject) => this._destroyDelegate(networkObject);
	}

	public static class NetworkManagerDelegatingPrefabInstanceHandlerExtensions
	{
		public static void AddNetworkPrefabHandler(
			this NetworkManager networkManager,
			GameObject networkPrefab,
			DelegatingNetworkPrefabInstanceHandler.InstantiateDelegate instantiateDelegate,
			DelegatingNetworkPrefabInstanceHandler.DestroyDelegate destroyDelegate
		)
		{
			networkManager.AddNetworkPrefab(networkPrefab);
			networkManager.PrefabHandler.AddHandler(networkPrefab, new DelegatingNetworkPrefabInstanceHandler(instantiateDelegate, destroyDelegate));
		}

		public static void RemoveNetworkPrefabHandler(this NetworkManager networkManager, GameObject networkPrefab)
		{
			networkManager.PrefabHandler.RemoveHandler(networkPrefab);
			networkManager.RemoveNetworkPrefab(networkPrefab);
		}
	}
}
