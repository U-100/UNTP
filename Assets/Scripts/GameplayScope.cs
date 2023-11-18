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

		public NetworkGameBoard CreateNetworkGameBoard()
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(this.gameSettings.networkGameBoardPrefab.gameObject);

			gameObject.GetComponent<NetworkWorldMap>().Init(this.worldGen, this.gameSettings.chunkSize, this.CreateVisualChunk, this.CreatePhysicalChunk);
			gameObject.GetComponent<NetworkPlayersRepository>().Init(this.gameSettings.networkPlayerCharacterPrefab, this.CreateNetworkPlayerCharacter);
			gameObject.GetComponent<NetworkEnemyRepository>().Init(this.gameSettings.networkWalkerPrefab, this.CreateNetworkWalker, this.gameSettings.networkStriderPrefab, this.CreateNetworkStrider);

			return gameObject.GetComponent<NetworkGameBoard>();
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

		NetworkPlayerCharacter CreateNetworkPlayerCharacter(float3 position, quaternion rotation) => UnityEngine.Object.Instantiate(this.gameSettings.networkPlayerCharacterPrefab, position, rotation);

		VisualChunk CreateVisualChunk(Vector3 position) => UnityEngine.Object.Instantiate(this.gameSettings.visualChunkPrefab, position, Quaternion.identity);
		
		PhysicalChunk CreatePhysicalChunk(Vector3 position) => UnityEngine.Object.Instantiate(this.gameSettings.physicalChunkPrefab, position, Quaternion.identity);

		NetworkWalker CreateNetworkWalker(float3 position, quaternion rotation) => UnityEngine.Object.Instantiate(this.gameSettings.networkWalkerPrefab, position, rotation);
		NetworkStrider CreateNetworkStrider(float3 position, quaternion rotation) => UnityEngine.Object.Instantiate(this.gameSettings.networkStriderPrefab, position, rotation);
	}
}
