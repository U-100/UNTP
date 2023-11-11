using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class NetworkPlayer : NetworkBehaviour, IPlayer
	{
		[SerializeField] private Blueprint _blueprint = null;

		private readonly NetworkVariable<NetworkBehaviourReference> _networkGameBoardNetworkBehaviourReference = new();
		
		public NetworkGameBoard networkGameBoard
		{
			get => this._networkGameBoardNetworkBehaviourReference.Value.TryGet(out NetworkGameBoard currentNetworkGameBoard) ? currentNetworkGameBoard : null;
			set => this._networkGameBoardNetworkBehaviourReference.Value = value;
		}

		private readonly NetworkVariable<NetworkBehaviourReference> _networkPlayerCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>();


		public void SetNetworkPlayerCharacter(NetworkPlayerCharacter networkPlayerCharacter) => this._networkPlayerCharacterNetworkBehaviourReference.Value = networkPlayerCharacter;

		public IPlayerCharacter character => this._networkPlayerCharacterNetworkBehaviourReference.Value.TryGet(out NetworkPlayerCharacter networkPlayerCharacter) ? networkPlayerCharacter : null;

		public ConstructionState constructionState { get; set; }

		public IBlueprint blueprint => this._blueprint;
	}
}
