using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
	public class NetworkPlayersRepository : NetworkBehaviour, IPlayersRepository
	{
		private NetworkPlayerCharacter _playerCharacterPrefab;
		public delegate NetworkPlayerCharacter NetworkPlayerCharacterFactory(float3 position, quaternion rotation);
		private NetworkPlayerCharacterFactory _playerCharacterFactory;

		private readonly List<NetworkPlayer> _networkPlayers = new();
		
		public NetworkPlayersRepository Init(NetworkPlayerCharacter playerCharacterPrefab, NetworkPlayerCharacterFactory playerCharacterFactory)
		{
			this._playerCharacterPrefab = playerCharacterPrefab;
			this._playerCharacterFactory = playerCharacterFactory;
			return this;
		}

		public int count => this._networkPlayers.Count;

		public IPlayer this[int playerIndex] => this._networkPlayers[playerIndex];

		public IPlayer localPlayer => this.NetworkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();

		public void CreateCharacterForPlayerAtIndex(int playerIndex, float3 position, quaternion rotation)
		{
			NetworkPlayer networkPlayer = this._networkPlayers[playerIndex];
			NetworkPlayerCharacter networkPlayerCharacter = this._playerCharacterFactory(position, rotation);
			networkPlayerCharacter.NetworkObject.SpawnWithOwnership(networkPlayer.OwnerClientId);
			networkPlayer.SetNetworkPlayerCharacter(networkPlayerCharacter);
		}

		public void PutPlayerOnBoard(NetworkPlayer player)
		{
			// if (!this._networkPlayers.Contains(player))
			// 	throw new Exception($"Player is already on the board");
			
			if (!this._networkPlayers.Contains(player))
				this._networkPlayers.Add(player);
		}

		public void RemovePlayerFromBoard(NetworkPlayer player)
		{
			// if (!this._networkPlayers.Contains(player))
			// 	throw new Exception("Player is not on the board");
			
			this._networkPlayers.Remove(player);
		}

		public override void OnNetworkSpawn()
		{
			this.NetworkManager.AddNetworkPrefabHandler(
				this._playerCharacterPrefab.gameObject,
				(_, position, rotation) => this._playerCharacterFactory(position, rotation).NetworkObject,
				networkObject => UnityEngine.Object.Destroy(networkObject.gameObject)
			);
		}

		public override void OnNetworkDespawn()
		{
			this.NetworkManager.RemoveNetworkPrefabHandler(this._playerCharacterPrefab.gameObject);
		}
	}
}
