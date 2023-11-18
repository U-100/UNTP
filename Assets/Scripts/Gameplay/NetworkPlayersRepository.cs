using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
	public class NetworkPlayersRepository : NetworkBehaviour, IPlayersRepository
	{
		private INetworkPrefabFactory<NetworkPlayerCharacter> _playerCharacterFactory;

		private readonly List<NetworkPlayer> _networkPlayers = new();
		
		public void Init(INetworkPrefabFactory<NetworkPlayerCharacter> playerCharacterFactory) => this._playerCharacterFactory = playerCharacterFactory;

		public int count => this._networkPlayers.Count;

		public IPlayer this[int playerIndex] => this._networkPlayers[playerIndex];

		public IPlayer localPlayer => this.NetworkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();

		public void CreateCharacterForPlayerAtIndex(int playerIndex, float3 position, quaternion rotation)
		{
			NetworkPlayer networkPlayer = this._networkPlayers[playerIndex];
			NetworkPlayerCharacter networkPlayerCharacter = this._playerCharacterFactory.Create(networkPlayer.OwnerClientId, position, rotation);
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
			if(!IsServer)
				this.NetworkManager.AddNetworkPrefabHandler(
					this._playerCharacterFactory.prefab.gameObject,
					(ownerClientId, position, rotation) => this._playerCharacterFactory.Create(ownerClientId, position, rotation).NetworkObject,
					networkObject => Destroy(networkObject.gameObject)
				);
		}

		public override void OnNetworkDespawn()
		{
			if(!IsServer)
				this.NetworkManager.RemoveNetworkPrefabHandler(this._playerCharacterFactory.prefab.gameObject);
		}
	}
}
