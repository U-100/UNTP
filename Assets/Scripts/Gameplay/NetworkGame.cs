using Unity.Netcode;

namespace UNTP
{
	public class NetworkGame : NetworkBehaviour, IGame
	{
		public enum NetworkGameState
		{
			Lobby,
			Paused,
			Playing,
		}
		private readonly NetworkVariable<NetworkGameState> _state = new();

		public bool isInLobby => this._state.Value == NetworkGameState.Lobby;
		public bool isServer => this.NetworkManager.IsServer;
		public bool isClient => this.NetworkManager.IsClient;
		public bool isPaused => this._state.Value != NetworkGameState.Playing;

		public void StartPlaying() => this._state.Value = NetworkGameState.Playing;

		public void Pause() => PauseServerRpc();
		public void Resume() => ResumeServerRpc();

		[ServerRpc(RequireOwnership = false)]
		private void PauseServerRpc() => this._state.Value = NetworkGameState.Paused;

		[ServerRpc(RequireOwnership = false)]
		private void ResumeServerRpc() => this._state.Value = NetworkGameState.Playing;
	}
}
