using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public static class GameplayScopeExtensions
	{
		public static T AddDisposableTo<T>(this T t, GameplayScope gameplayScope) where T : IDisposable => gameplayScope.AddDisposable(t);
	}

	public class GameplayScope : IDisposable
	{
		private readonly HashSet<IDisposable> _disposables = new();

		public GameplayScope(GameSettings gameSettings, NetworkManager networkManager)
		{
			Debug.Log("Creating GameplayScope");

			this.gameSettings = gameSettings;
			this.networkManager = networkManager;
		}

		public void Dispose()
		{
			Debug.Log("Disposing GameplayScope");

			foreach (IDisposable disposable in this._disposables)
			{
				try
				{
					disposable.Dispose();
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			}
			
			this._disposables.Clear();
		}

		public T AddDisposable<T>(T disposable) where T : IDisposable
		{
			if (this._disposables.Contains(disposable))
				throw new Exception("Attempt to add a disposable that has already been added");

			this._disposables.Add(disposable);
			return disposable;
		}


		private NetworkPrefabInstanceFactory<NetworkGameBoard> _networkGameBoardFactory;
		public NetworkPrefabInstanceFactory<NetworkGameBoard> networkGameBoardFactory => this._networkGameBoardFactory ?? new NetworkPrefabInstanceFactory<NetworkGameBoard>(this.gameSettings.networkGameBoardPrefab, this.InitNetworkGameBoard);

		public void InitNetworkGameBoard(NetworkGameBoard networkGameBoard, ulong ownerClientId, float3 position, quaternion rotation)
		{
			GameObject gameObject = networkGameBoard.gameObject;

			gameObject.GetComponent<NetworkWorldMap>().Init(this.worldGen, this.gameSettings.chunkSize, this.CreateVisualChunk, this.CreatePhysicalChunk);
			gameObject.GetComponent<NetworkPlayersRepository>().Init(this.networkPlayerCharacterFactory);
			gameObject.GetComponent<NetworkEnemyRepository>().Init(this.networkWalkerFactory, this.networkStriderFactory);
		}

		public GameSettings gameSettings { get; }
		public NetworkManager networkManager { get; }

		public ushort connectionPort => 7777;
		public ushort discoveryPort => 47777;
		public long appId = 123321;

		private NetworkDiscovery _networkDiscovery;
		public NetworkDiscovery networkDiscovery => this._networkDiscovery ??= new NetworkDiscovery(this.appId, this.discoveryPort);

		private GameLogic _gameLogic;
		public IGameLogic gameLogic => this._gameLogic ??= new GameLogic(this.gameSettings.playerSettings, this.localPlayerLogic, this.enemyLogic);

		private LocalPlayerLogic _localPlayerLogic;
		public LocalPlayerLogic localPlayerLogic => this._localPlayerLogic ??= new LocalPlayerLogic(this.gameSettings.playerSettings, this.gameInputSource, this.gamePhysics);

		private EnemyLogic _enemyLogic;
		public EnemyLogic enemyLogic => this._enemyLogic ??= new EnemyLogic(this.gameSettings.enemySettings, this.gamePhysics);

		private GamePhysics _gamePhysics;
		public IGamePhysics gamePhysics => this._gamePhysics ??= new GamePhysics();

		private GameInputSource _gameInputSource;
		public IGameInputSource gameInputSource => this._gameInputSource ??= new GameInputSource().AddDisposableTo(this);

		private WorldGen _worldGen;
		public IWorldGen worldGen => this._worldGen ??= new WorldGen(this.gameSettings.worldSettings);

		private NetworkPrefabInstanceFactory<NetworkPlayerCharacter> _networkPlayerCharacterFactory;
		public NetworkPrefabInstanceFactory<NetworkPlayerCharacter> networkPlayerCharacterFactory => this._networkPlayerCharacterFactory ?? new NetworkPrefabInstanceFactory<NetworkPlayerCharacter>(this.gameSettings.networkPlayerCharacterPrefab);

		private NetworkPrefabInstanceFactory<NetworkWalker> _networkWalkerFactory;
		public NetworkPrefabInstanceFactory<NetworkWalker> networkWalkerFactory => this._networkWalkerFactory ?? new NetworkPrefabInstanceFactory<NetworkWalker>(this.gameSettings.networkWalkerPrefab);

		private NetworkPrefabInstanceFactory<NetworkStrider> _networkStriderFactory;
		public NetworkPrefabInstanceFactory<NetworkStrider> networkStriderFactory => this._networkStriderFactory ?? new NetworkPrefabInstanceFactory<NetworkStrider>(this.gameSettings.networkStriderPrefab);

		VisualChunk CreateVisualChunk(Vector3 position) => UnityEngine.Object.Instantiate(this.gameSettings.visualChunkPrefab, position, Quaternion.identity);
		
		PhysicalChunk CreatePhysicalChunk(Vector3 position) => UnityEngine.Object.Instantiate(this.gameSettings.physicalChunkPrefab, position, Quaternion.identity);
	}
}
