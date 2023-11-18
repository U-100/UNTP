using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class NetworkEnemyRepository : NetworkBehaviour, IEnemyRepository
	{
		private INetworkPrefabFactory<NetworkWalker> _walkerFactory;

		private INetworkPrefabFactory<NetworkStrider> _striderFactory;

		private readonly List<NetworkEnemy> _enemies = new();

		public void Init(INetworkPrefabFactory<NetworkWalker> walkerFactory, INetworkPrefabFactory<NetworkStrider> striderFactory)
		{
			this._walkerFactory = walkerFactory;
			this._striderFactory = striderFactory;
		}

		public int count => this._enemies.Count;

		public IEnemy this[int index] => this._enemies[index];

		public IWalker CreateWalker(float3 position)
		{
			NetworkWalker walker = this._walkerFactory.Create(this.NetworkManager.LocalClientId, position, quaternion.identity);
			walker.NetworkObject.Spawn();
			this._enemies.Add(walker);
			return walker;
		}

		public IStrider CreateStrider(float3 position)
		{
			NetworkStrider strider = this._striderFactory.Create(this.NetworkManager.LocalClientId, position, quaternion.identity);
			strider.NetworkObject.Spawn();
			this._enemies.Add(strider);
			return strider;
		}

		public void DestroyEnemyAtIndex(int enemyIndex)
		{
			NetworkEnemy enemy = this._enemies[enemyIndex];
			this._enemies.RemoveAt(enemyIndex);
			Destroy(enemy.gameObject);
		}

		public override void OnNetworkSpawn()
		{
			if (!IsServer)
			{
				this.NetworkManager.AddNetworkPrefabHandler(this._walkerFactory.prefab.gameObject, CreateNetworkWalker, DestroyNetworkWalker);
				this.NetworkManager.AddNetworkPrefabHandler(this._striderFactory.prefab.gameObject, CreateNetworkStrider, DestroyNetworkStrider);
			}
		}

		private NetworkObject CreateNetworkWalker(ulong ownerClientId, Vector3 position, Quaternion rotation)
		{
			NetworkWalker networkWalker = this._walkerFactory.Create(ownerClientId, position, rotation);
			this._enemies.Add(networkWalker);
			return networkWalker.NetworkObject;
		}

		private void DestroyNetworkWalker(NetworkObject networkObject)
		{
			NetworkWalker networkWalker = networkObject.GetComponent<NetworkWalker>();
			this._enemies.Remove(networkWalker);
			Destroy(networkObject.gameObject);
		}

		private NetworkObject CreateNetworkStrider(ulong ownerClientId, Vector3 position, Quaternion rotation)
		{
			NetworkStrider networkStrider = this._striderFactory.Create(ownerClientId, position, rotation);
			this._enemies.Add(networkStrider);
			return networkStrider.NetworkObject;
		}

		private void DestroyNetworkStrider(NetworkObject networkObject)
		{
			NetworkStrider networkStrider = networkObject.GetComponent<NetworkStrider>();
			this._enemies.Remove(networkStrider);
			Destroy(networkObject.gameObject);
		}

		public override void OnNetworkDespawn()
		{
			if (!IsServer)
			{
				this.NetworkManager.RemoveNetworkPrefabHandler(this._walkerFactory.prefab.gameObject);
				this.NetworkManager.RemoveNetworkPrefabHandler(this._striderFactory.prefab.gameObject);
			}
		}
	}
}
