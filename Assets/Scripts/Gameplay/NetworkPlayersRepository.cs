using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
	public class NetworkPlayersRepository : NetworkBehaviour, IPlayersRepository
	{
		public delegate NetworkPlayerCharacter NetworkPlayerCharacterFactory(float3 position, quaternion rotation);
		private NetworkPlayerCharacterFactory _networkPlayerCharacterFactory;


		public NetworkPlayersRepository Init(NetworkPlayerCharacterFactory networkPlayerCharacterFactory)
		{
			this._networkPlayerCharacterFactory = networkPlayerCharacterFactory;
			return this;
		}


		public int count => this.NetworkManager.ConnectedClientsIds.Count;

		public IPlayer this[int playerIndex] => this.NetworkManager.ConnectedClientsList[playerIndex].PlayerObject.GetComponent<NetworkPlayer>();

		public IPlayer localPlayer => this.NetworkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();

		public void CreateCharacterForPlayerAtIndex(int playerIndex, float3 position, quaternion rotation)
		{
			NetworkClient networkClient = this.NetworkManager.ConnectedClientsList[playerIndex];
			NetworkPlayer networkPlayer = networkClient.PlayerObject.GetComponent<NetworkPlayer>();
			NetworkPlayerCharacter networkPlayerCharacter = this._networkPlayerCharacterFactory(position, rotation);
			networkPlayerCharacter.NetworkObject.SpawnWithOwnership(networkClient.ClientId);
			networkPlayer.SetNetworkPlayerCharacter(networkPlayerCharacter);
		}
	}
}
