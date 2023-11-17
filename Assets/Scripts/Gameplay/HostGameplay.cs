using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace UNTP
{
	public class HostGameplay : IGameplay
	{
		private readonly NetworkDiscovery _networkDiscovery;
		private readonly NetworkManager _networkManager;
		private readonly ushort _connectionPort;
		private readonly IGameLogic _gameLogic;

		public delegate NetworkGameBoard NetworkGameBoardFactory();
		private readonly NetworkGameBoardFactory _networkGameBoardFactory;

		private readonly List<IDisposable> _disposables = new();

		private CancellationTokenSource _serverAdvertisementCts;
		private NetworkGameBoard _networkGameBoard;

		public HostGameplay(NetworkDiscovery networkDiscovery, NetworkManager networkManager, ushort connectionPort, IGameLogic gameLogic, NetworkGameBoardFactory networkGameBoardFactory)
		{
			this._networkDiscovery = networkDiscovery;
			this._networkManager = networkManager;
			this._connectionPort = connectionPort;
			this._gameLogic = gameLogic;
			this._networkGameBoardFactory = networkGameBoardFactory;

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

		public IGameBoard gameBoard => this._networkGameBoard;

		public async Task Start()
		{
			Debug.Log($"Starting host on port {this._connectionPort}");

			try
			{
				this._networkManager.GetComponent<UnityTransport>().SetConnectionData(null, this._connectionPort, "0.0.0.0"); // listen on any ipv4 address
				this._networkManager.StartHost();

				await RunServerAdvertisement();

				while (this._networkManager.IsHost)
				{
					this._gameLogic.Update(this.gameBoard, Time.deltaTime);
					await Task.Yield();
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				throw;
			}
			finally
			{
				Debug.Log("Exiting HostGameplay.Start()");
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

			this._networkGameBoard = this._networkGameBoardFactory();
			this._networkGameBoard.NetworkObject.Spawn();
		}

		private void OnServerStopped(bool obj)
		{
			Debug.Log("Server stopped");

			this._networkGameBoard = null;
		}

		private void OnClientConnected(ulong clientId)
		{
			Debug.Log($"Client {clientId} connected");

			NetworkObject clientPlayerNetworkObject = this._networkManager.SpawnManager.GetPlayerNetworkObject(clientId);
			NetworkPlayer networkPlayer = clientPlayerNetworkObject.GetComponent<NetworkPlayer>();
			networkPlayer.networkGameBoard = this._networkGameBoard;
		}

		private void OnClientDisconnected(ulong clientId)
		{
			Debug.Log($"Client {clientId} disconnected");
		}
	}
}
