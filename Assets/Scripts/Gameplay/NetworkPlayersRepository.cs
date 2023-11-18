using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class NetworkPlayersRepository : NetworkBehaviour, IPlayersRepository
	{
		private INetworkPrefabFactory<NetworkPlayer> _playerFactory;
		private INetworkPrefabFactory<NetworkPlayerCharacter> _playerCharacterFactory;

		private readonly List<NetworkPlayer> _networkPlayers = new();
		private NetworkPlayer _localPlayer;
		
		public void Init(INetworkPrefabFactory<NetworkPlayer> playerFactory, INetworkPrefabFactory<NetworkPlayerCharacter> playerCharacterFactory)
		{
			this._playerFactory = playerFactory;
			this._playerCharacterFactory = playerCharacterFactory;
		}

		public int count => this._networkPlayers.Count;

		public IPlayer this[int playerIndex] => this._networkPlayers[playerIndex];

		public IPlayer localPlayer
		{
			get
			{
				if(this._localPlayer == null)
				{
					foreach (NetworkPlayer networkPlayer in this._networkPlayers)
					{
						if (networkPlayer.IsOwner)
						{
							this._localPlayer = networkPlayer;
							break;
						}
					}
				}

				return this._localPlayer;
			}
		}

		public void CreateCharacterForPlayerAtIndex(int playerIndex, float3 position, quaternion rotation)
		{
			NetworkPlayer networkPlayer = this._networkPlayers[playerIndex];
			NetworkPlayerCharacter networkPlayerCharacter = this._playerCharacterFactory.Create(networkPlayer.OwnerClientId, position, rotation);
			networkPlayerCharacter.NetworkObject.SpawnWithOwnership(networkPlayer.OwnerClientId);
			networkPlayer.SetNetworkPlayerCharacter(networkPlayerCharacter);
		}

		public void CreatePlayerWithClientId(ulong ownerClientId)
		{
			NetworkObject networkPlayerObject = CreateNetworkPlayer(ownerClientId, float3.zero, quaternion.identity);
			networkPlayerObject.SpawnWithOwnership(ownerClientId);
		}

		public void DestroyPlayerWithClientId(ulong ownerClientId)
		{
			int removed = this._networkPlayers.RemoveAll(np => np.OwnerClientId == ownerClientId);
			Debug.Log($"removed: {removed}");
		}
		
		public override void OnNetworkSpawn()
		{
			if(!IsServer)
			{
				this.NetworkManager.AddNetworkPrefabHandler(this._playerFactory.prefab.gameObject, CreateNetworkPlayer, DestroyNetworkPlayer);

				this.NetworkManager.AddNetworkPrefabHandler(
					this._playerCharacterFactory.prefab.gameObject,
					(ownerClientId, position, rotation) => this._playerCharacterFactory.Create(ownerClientId, position, rotation).NetworkObject,
					networkObject => Destroy(networkObject.gameObject)
				);
			}
		}

		private NetworkObject CreateNetworkPlayer(ulong ownerClientId, Vector3 position, Quaternion rotation)
		{
			NetworkPlayer networkPlayer = this._playerFactory.Create(ownerClientId, position, rotation);
			this._networkPlayers.Add(networkPlayer);
			return networkPlayer.NetworkObject;
		}

		private void DestroyNetworkPlayer(NetworkObject networkObject)
		{
			NetworkPlayer networkPlayer = networkObject.GetComponent<NetworkPlayer>();
			this._networkPlayers.Remove(networkPlayer);
			Destroy(networkObject.gameObject);
		}

		public override void OnNetworkDespawn()
		{
			if(!IsServer)
			{
				this.NetworkManager.RemoveNetworkPrefabHandler(this._playerFactory.prefab.gameObject);
				this.NetworkManager.RemoveNetworkPrefabHandler(this._playerCharacterFactory.prefab.gameObject);
			}
		}
	}
}
