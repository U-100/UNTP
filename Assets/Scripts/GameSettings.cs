using UnityEngine;

namespace UNTP
{
	[CreateAssetMenu]
	public class GameSettings : ScriptableObject
	{
		[Header("General")]

		[SerializeField] private GameObject _networkGameBoardPrefab;
		public GameObject networkGameBoardPrefab => this._networkGameBoardPrefab;


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
