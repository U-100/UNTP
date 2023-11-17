using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class NetworkPlayer : NetworkBehaviour, IPlayer
	{
		[SerializeField] private Blueprint _blueprint;

		private readonly NetworkVariable<NetworkBehaviourReference> _networkGameBoardNetworkBehaviourReference = new();
		
		public NetworkGameBoard networkGameBoard
		{
			get => this._networkGameBoardNetworkBehaviourReference.Value.TryGet(out NetworkGameBoard currentNetworkGameBoard) ? currentNetworkGameBoard : null;
			set => this._networkGameBoardNetworkBehaviourReference.Value = value;
		}

		private readonly NetworkVariable<NetworkBehaviourReference> _networkPlayerCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>();

		//TODO: Hack to register other players on remote client. _networkGameBoardNetworkBehaviourReference.OnValueChanged doesn't get triggered sometimes, WTF!??!?!? 
		private bool _putOnBoard;
		void Update()
		{
			if (!this._putOnBoard && this.networkGameBoard != null)
			{
				this.networkGameBoard.PutPlayerOnBoard(this);
				this._putOnBoard = true;
			}
			
		}
		
		public override void OnNetworkSpawn() { }

		public override void OnNetworkDespawn()
		{
			if(this.networkGameBoard != null)
				this.networkGameBoard.RemovePlayerFromBoard(this);
		}

		public void SetNetworkPlayerCharacter(NetworkPlayerCharacter networkPlayerCharacter) => this._networkPlayerCharacterNetworkBehaviourReference.Value = networkPlayerCharacter;

		public IPlayerCharacter character => this._networkPlayerCharacterNetworkBehaviourReference.Value.TryGet(out NetworkPlayerCharacter networkPlayerCharacter) ? networkPlayerCharacter : null;

		public ConstructionState constructionState { get; set; }

		public IBlueprint blueprint => this._blueprint;
	}
}
