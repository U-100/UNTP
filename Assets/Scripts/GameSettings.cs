using UnityEngine;

namespace UNTP
{
	public interface IGameSettings
	{
		WorldSettings worldSettings { get; }
		PlayerSettings playerSettings { get; }
		EnemySettings enemySettings { get; }
	}
	
	[CreateAssetMenu]
	public class GameSettings : ScriptableObject, IGameSettings
	{
		[Header("General")]

		[SerializeField] private NetworkGameBoard _networkGameBoardPrefab;
		public NetworkGameBoard networkGameBoardPrefab => this._networkGameBoardPrefab;


		[Header("Map generator")]

		[SerializeField] private WorldSettings _worldSettings;
		public WorldSettings worldSettings => this._worldSettings;

		[SerializeField] private int _chunkSize;
		public int chunkSize => this._chunkSize;

		[SerializeField] private PhysicalChunk _physicalChunkPrefab;
		public PhysicalChunk physicalChunkPrefab => this._physicalChunkPrefab;

		[SerializeField] private VisualChunk _visualChunkPrefab;
		public VisualChunk visualChunkPrefab => this._visualChunkPrefab;


		[Header("Player")]

		[SerializeField] private NetworkPlayer _networkPlayerPrefab;
		public NetworkPlayer networkPlayerPrefab => this._networkPlayerPrefab;

		[SerializeField] private NetworkPlayerCharacter _networkPlayerCharacterPrefab;
		public NetworkPlayerCharacter networkPlayerCharacterPrefab => this._networkPlayerCharacterPrefab;

		[SerializeField] private PlayerSettings _playerSettings;
		public PlayerSettings playerSettings => this._playerSettings;


		[Header("Enemies")]

		[SerializeField] private NetworkWalker _networkWalkerPrefab;
		public NetworkWalker networkWalkerPrefab => this._networkWalkerPrefab;

		[SerializeField] private NetworkStrider _networkStriderPrefab;
		public NetworkStrider networkStriderPrefab => this._networkStriderPrefab;

		[SerializeField] private EnemySettings _enemySettings;
		public EnemySettings enemySettings => this._enemySettings;
	}
}
