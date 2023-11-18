using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace UNTP
{
	public class ClientGameplay : IGameplay
	{
		private readonly NetworkDiscovery _networkDiscovery;
		private readonly NetworkManager _networkManager;
		private readonly ushort _connectionPort;
		private readonly INetworkPrefabFactory<NetworkGameBoard> _gameBoardFactory;
		private readonly IGameLogic _gameLogic;

		private readonly List<IDisposable> _disposables = new();

		private CancellationTokenSource _searchForServersCts;
		private NetworkGameBoard _gameBoard;

		public ClientGameplay(NetworkDiscovery networkDiscovery, NetworkManager networkManager, ushort connectionPort, INetworkPrefabFactory<NetworkGameBoard> gameBoardFactory, IGameLogic gameLogic)
		{
			this._networkDiscovery = networkDiscovery;
			this._networkManager = networkManager;
			this._connectionPort = connectionPort;
			this._gameBoardFactory = gameBoardFactory;
			this._gameLogic = gameLogic;

			this._networkManager.OnClientStarted += this.OnClientStarted;
			this._networkManager.OnClientStopped += this.OnClientStopped;
			
			this._networkManager.AddNetworkPrefabHandler(
				this._gameBoardFactory.prefab.gameObject,
				(ownerClientId, position, rotation) => this._gameBoardFactory.Create(ownerClientId, position, rotation).NetworkObject,
				networkObject => UnityEngine.Object.Destroy(networkObject.gameObject)
			);
		}

		public void Dispose()
		{
			this._networkManager.OnClientStarted -= this.OnClientStarted;
			this._networkManager.OnClientStopped -= this.OnClientStopped;

			this._networkManager.RemoveNetworkPrefabHandler(this._gameBoardFactory.prefab.gameObject);

			foreach (IDisposable disposable in this._disposables)
				disposable.Dispose();
		}

		public ClientGameplay AddDisposable(IDisposable disposable)
		{
			this._disposables.Add(disposable);
			return this;
		}

		public IGameBoard gameBoard => this._gameBoard ??= this._networkManager.LocalClient.PlayerObject != null ? this._networkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().networkGameBoard : null;

		public async Task Start()
		{
			this._searchForServersCts = new CancellationTokenSource();

			IPEndPoint ipEndPoint = null;
			try
			{
				await Task.Delay(1000, this._searchForServersCts.Token); // intentional delay for testing

				ipEndPoint = await FindFirstServer(this._searchForServersCts.Token);
			}
			catch (OperationCanceledException) { /*suppressed*/ }
			finally
			{
				this._searchForServersCts?.Dispose();
				this._searchForServersCts = null;
			}

			if (ipEndPoint != null)
			{
				this._networkManager.GetComponent<UnityTransport>().SetConnectionData(ipEndPoint.Address.ToString(), this._connectionPort);
				this._networkManager.StartClient();

				Debug.Log($"Client connected to {ipEndPoint.Address}:{this._connectionPort}");

				while (this._networkManager.IsClient)
				{
					if(this.gameBoard != null)
						this._gameLogic.Update(this.gameBoard, Time.deltaTime);

					await Task.Yield();
				}
			}
		}

		public void Play() => throw new Exception("ClientGameplay can not process the Play() call, only HostGameplay can");

		public void Stop()
		{
			this._searchForServersCts?.Cancel();
			this._searchForServersCts?.Dispose();
			this._searchForServersCts = null;

			this._networkManager.Shutdown();
		}

		public void Pause() => this.gameBoard.game.Pause();
		public void Resume() => this.gameBoard.game.Resume();

		private async Task<IPEndPoint> FindFirstServer(CancellationToken ct)
		{
			await foreach (IPEndPoint ipEndPoint in this._networkDiscovery.FindServers(ct))
				return ipEndPoint;

			ct.ThrowIfCancellationRequested();
			throw new Exception("Couldn't find server");
		}

		private void OnClientStarted()
		{
			Debug.Log("Client started");
		}

		private void OnClientStopped(bool isHost)
		{
			Debug.Log("Client stopped");

			this._gameBoard = null;
		}
	}
}
