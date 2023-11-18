using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
	public class NetworkEnemyRepository : NetworkBehaviour, IEnemyRepository
	{
		private NetworkWalker _walkerPrefab;
		public delegate NetworkWalker WalkerFactory(float3 position, quaternion rotation);
		private WalkerFactory _walkerFactory;

		private NetworkStrider _striderPrefab;
		public delegate NetworkStrider StriderFactory(float3 position, quaternion rotation);
		private StriderFactory _striderFactory;

		private readonly List<NetworkEnemy> _enemies = new();

		public NetworkEnemyRepository Init(NetworkWalker walkerPrefab, WalkerFactory walkerFactory, NetworkStrider striderPrefab, StriderFactory striderFactory)
		{
			this._walkerPrefab = walkerPrefab;
			this._walkerFactory = walkerFactory;
			this._striderPrefab = striderPrefab;
			this._striderFactory = striderFactory;
			return this;
		}

		public int count => this._enemies.Count;

		public IEnemy this[int index] => this._enemies[index];

		public IWalker CreateWalker(float3 position)
		{
			NetworkWalker walker = this._walkerFactory(position, quaternion.identity);
			walker.NetworkObject.Spawn();
			this._enemies.Add(walker);
			return walker;
		}

		public IStrider CreateStrider(float3 position)
		{
			NetworkStrider strider = this._striderFactory(position, quaternion.identity);
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
			this.NetworkManager.AddNetworkPrefabHandler(
				this._walkerPrefab.gameObject,
				(_, position, rotation) => this._walkerFactory(position, rotation).NetworkObject,
				networkObject => UnityEngine.Object.Destroy(networkObject.gameObject)
			);

			this.NetworkManager.AddNetworkPrefabHandler(
				this._striderPrefab.gameObject,
				(_, position, rotation) => this._striderFactory(position, rotation).NetworkObject,
				networkObject => UnityEngine.Object.Destroy(networkObject.gameObject)
			);
		}

		public override void OnNetworkDespawn()
		{
			this.NetworkManager.RemoveNetworkPrefabHandler(this._walkerPrefab.gameObject);
			this.NetworkManager.RemoveNetworkPrefabHandler(this._striderPrefab.gameObject);
		}
	}
}
