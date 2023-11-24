using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class NetworkGameBoard : NetworkBehaviour, IGameBoard
	{
		[SerializeField] private NetworkGame _networkGame = null;
		[SerializeField] private NetworkWorldMap _networkWorldMap = null;
		[SerializeField] private NetworkPlayersRepository _networkPlayersRepository = null;
		[SerializeField] private NetworkEnemyRepository _networkEnemyRepository = null;

		public IGame game => this._networkGame;
		public IGameSettings settings { get; set; }
		public IGameInput input { get; set; }
		public IGamePhysics physics { get; set; }
		public IWorldMap worldMap => this._networkWorldMap;
		public IPlayersRepository players => this._networkPlayersRepository;
		public IEnemyRepository enemies => this._networkEnemyRepository;

		public NetworkPlayersRepository networkPlayersRepository => this._networkPlayersRepository;
	}
}
