using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace UNTP
{
	public class HostGameplay : IGameplay, INetworkUpdateSystem
	{
		private readonly NetworkDiscovery _networkDiscovery;
		private readonly NetworkManager _networkManager;
		private readonly ushort _connectionPort;
		private readonly IGameLogic _gameLogic;

		private readonly INetworkPrefabFactory<NetworkGameBoard> _gameBoardFactory;

		private readonly List<IDisposable> _disposables = new();

		private CancellationTokenSource _serverAdvertisementCts;
		private TaskCompletionSource<bool> _serverTcs;
		private NetworkGameBoard _gameBoard;

		public HostGameplay(NetworkDiscovery networkDiscovery, NetworkManager networkManager, ushort connectionPort, INetworkPrefabFactory<NetworkGameBoard> gameBoardFactory, IGameLogic gameLogic)
		{
			this._networkDiscovery = networkDiscovery;
			this._networkManager = networkManager;
			this._connectionPort = connectionPort;
			this._gameBoardFactory = gameBoardFactory;
			this._gameLogic = gameLogic;

			this._networkManager.OnServerStarted += this.OnServerStarted;
			this._networkManager.OnServerStopped += this.OnServerStopped;
			this._networkManager.OnClientConnectedCallback += this.OnClientConnected;
			this._networkManager.OnClientDisconnectCallback += this.OnClientDisconnected;
		}

		public void Dispose()
		{
			this._networkManager.OnServerStarted -= this.OnServerStarted;
			this._networkManager.OnServerStopped -= this.OnServerStopped;
			this._networkManager.OnClientConnectedCallback -= this.OnClientConnected;
			this._networkManager.OnClientDisconnectCallback -= this.OnClientDisconnected;

			foreach (IDisposable disposable in this._disposables)
				disposable.Dispose();
		}

		public HostGameplay AddDisposable(IDisposable disposable)
		{
			this._disposables.Add(disposable);
			return this;
		}

		public IGameBoard gameBoard => this._gameBoard;

		public async Task Start()
		{
			Debug.Log($"Starting host on port {this._connectionPort}");

			try
			{
				this._networkManager.GetComponent<UnityTransport>().SetConnectionData(null, this._connectionPort, "0.0.0.0"); // listen on any ipv4 address
				this._networkManager.StartHost();

				await RunServerAdvertisement();

				this.RegisterNetworkUpdate();

				await this._serverTcs.Task;
			}
			finally
			{
				this.UnregisterNetworkUpdate();
			}
		}

		public void Play()
		{
			this._serverAdvertisementCts.Cancel();
			this._serverAdvertisementCts.Dispose();
			this._serverAdvertisementCts = null;

			this.gameBoard.game.StartPlaying();
		}

		public void Pause() => this.gameBoard.game.Pause();
		public void Resume() => this.gameBoard.game.Resume();

		public void Stop()
		{
			this._serverAdvertisementCts?.Cancel();
			this._serverAdvertisementCts?.Dispose();
			this._serverAdvertisementCts = null;

			this._networkManager.Shutdown();
		}

		public void NetworkUpdate(NetworkUpdateStage updateStage) => this._gameLogic.Update(this.gameBoard, Time.deltaTime);

		private async Task RunServerAdvertisement()
		{
			this._serverAdvertisementCts = new CancellationTokenSource();

			try
			{
				await this._networkDiscovery.AdvertiseServer(this._serverAdvertisementCts.Token);
			}
			catch (OperationCanceledException) { /*suppressed*/ }
			finally
			{
				Debug.Log($"Server discovery listener stopped");
			}
		}

		private void OnServerStarted()
		{
			Debug.Log("Server started");

			this._serverTcs = new TaskCompletionSource<bool>();

			this._gameBoard = this._gameBoardFactory.Create(this._networkManager.LocalClientId, float3.zero, quaternion.identity);
			this._gameBoard.NetworkObject.Spawn();
		}

		private void OnServerStopped(bool obj)
		{
			Debug.Log("Server stopped");

			this._serverTcs.SetResult(false);
			this._serverTcs = null;

			this._gameBoard = null;
		}

		private void OnClientConnected(ulong clientId)
		{
			Debug.Log($"Client {clientId} connected");

			this._gameBoard.networkPlayersRepository.CreatePlayerWithClientId(clientId);
		}

		private void OnClientDisconnected(ulong clientId)
		{
			this._gameBoard.networkPlayersRepository.DestroyPlayerWithClientId(clientId);

			Debug.Log($"Client {clientId} disconnected");
		}
	}
}
