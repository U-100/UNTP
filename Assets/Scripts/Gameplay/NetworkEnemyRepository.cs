using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
	public class NetworkEnemyRepository : NetworkBehaviour, IEnemyRepository
	{
		public delegate NetworkWalker WalkerFactory(float3 position, quaternion rotation);
		private WalkerFactory _walkerFactory;

		public delegate NetworkStrider StriderFactory(float3 position, quaternion rotation);
		private StriderFactory _striderFactory;

		private List<NetworkEnemy> _enemies;


		public NetworkEnemyRepository Init(WalkerFactory walkerFactory, StriderFactory striderFactory)
		{
			this._walkerFactory = walkerFactory;
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
			if (this.IsServer)
				this._enemies = new List<NetworkEnemy>(); // in theory we only need the enemies array on server
		}
	}
}
