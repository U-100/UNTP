using System;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class Main : MonoBehaviour
	{
		[SerializeField]
		private UI _ui;

		[SerializeField]
		private NetworkManager _networkManager;

		[SerializeField]
		private GameSettings _gameSettings;

		private MainViewModel _mainViewModel;

		void Start()
		{
			try
			{
				this._mainViewModel = new MainViewModel(this.CreateHostGameplay, this.CreateClientGameplay, this.CreateHudViewModel);
				this._ui.viewModel = this._mainViewModel;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex, this);

#if UNITY_EDITOR
				UnityEditor.EditorApplication.ExitPlaymode();
#endif
			}
		}

		private IHudViewModel CreateHudViewModel(IGameplay gameplay) => new HudViewModel(gameplay);

		void OnDestroy()
		{
			this._mainViewModel?.Dispose();
		}

		private HostGameplay CreateHostGameplay()
		{
			GameplayScope gameplayScope = new GameplayScope(this._gameSettings, this._networkManager);

			HostGameplay hostGameplay =
				new HostGameplay(
					gameplayScope.networkDiscovery,
					gameplayScope.networkManager,
					gameplayScope.connectionPort,
					gameplayScope.gameLogic,
					gameplayScope.CreateNetworkGameBoard
				);

			return hostGameplay.AddDisposable(gameplayScope);
		}

		private ClientGameplay CreateClientGameplay()
		{
			GameplayScope gameplayScope = new GameplayScope(this._gameSettings, this._networkManager);

			ClientGameplay clientGameplay =
				new ClientGameplay(
					gameplayScope.networkDiscovery,
					gameplayScope.networkManager,
					gameplayScope.connectionPort,
					gameplayScope.gameSettings.networkGameBoardPrefab,
					gameplayScope.CreateNetworkGameBoard,
					gameplayScope.gameLogic
				);

			return clientGameplay.AddDisposable(gameplayScope);
		}
	}
}
