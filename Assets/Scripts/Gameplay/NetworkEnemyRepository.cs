using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;

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
				this.NetworkManager.AddNetworkPrefabHandler(
					this._walkerFactory.prefab.gameObject,
					(ownerClientId, position, rotation) => this._walkerFactory.Create(ownerClientId, position, rotation).NetworkObject,
					networkObject => Destroy(networkObject.gameObject)
				);

				this.NetworkManager.AddNetworkPrefabHandler(
					this._striderFactory.prefab.gameObject,
					(ownerClientId, position, rotation) => this._striderFactory.Create(ownerClientId, position, rotation).NetworkObject,
					networkObject => Destroy(networkObject.gameObject)
				);
			}
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
