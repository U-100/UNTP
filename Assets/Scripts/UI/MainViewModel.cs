using System;

namespace UNTP
{
	public class MainViewModel : IMainViewModel, IDisposable
	{
		public delegate IGameplay GameplayFactory();
		private readonly GameplayFactory _hostGameplayFactory;
		private readonly GameplayFactory _clientGameplayFactory;

		private IGameplay _gameplay;

		public delegate IHudViewModel HudViewModelFactory(IGameplay gameplay);
		private readonly HudViewModelFactory _hudViewModelFactory;

		private IHudViewModel _hud;

		public MainViewModel(GameplayFactory hostGameplayFactory, GameplayFactory clientGameplayFactory, HudViewModelFactory hudViewModelFactory)
		{
			this._hostGameplayFactory = hostGameplayFactory;
			this._clientGameplayFactory = clientGameplayFactory;
			this._hudViewModelFactory = hudViewModelFactory;
		}

		public void Dispose() { }

		public MainViewModelState state
		{
			get
			{
				return this._gameplay switch
				{
					null => MainViewModelState.Idle,
					{ gameBoard: null } => MainViewModelState.ClientLobby,
					{ gameBoard: {game: { isServer: true } and { isInLobby: true } } } => MainViewModelState.HostLobby, // we need to check this before checking client condition, because host is both server and client
					{ gameBoard: {game: { isClient: true } and { isInLobby: true } } }=> MainViewModelState.ClientLobby,
					{ gameBoard: {game: { isPaused: true } } }=> MainViewModelState.GamePaused,
					_ => MainViewModelState.GamePlaying,
				};
			}
		}

		public int playersCount => this._gameplay?.gameBoard?.players.count ?? 0; // only valid to call on the host

		public bool connected => this._gameplay?.gameBoard != null; // this might be not exactly correct, but we'll see

		public IHudViewModel hud => this._hud;

		public void StartHost()
		{
			if (this._gameplay != null)
				throw new Exception("Can not StartHost() when another game is already in progress");

			StartGameplay(this._hostGameplayFactory);
		}

		public void StartClient()
		{
			if (this._gameplay != null)
				throw new Exception("Can not StartClient() when another game is in progress");

			StartGameplay(this._clientGameplayFactory);
		}

		private async void StartGameplay(GameplayFactory gameplayFactory)
		{
			using IGameplay gameplay = gameplayFactory();
			this._gameplay = gameplay;

			using IHudViewModel hudViewModel = this._hudViewModelFactory(gameplay);
			this._hud = hudViewModel;
			
			try
			{
				await this._gameplay.Start();
			}
			finally
			{
				this._gameplay = null;
				this._hud = null;
			}
		}

		public void Play() => this._gameplay.Play(); // should only be called on a host, will throw if called on a client

		public void Stop() => this._gameplay.Stop(); // this doesn't happen immediately, so we wait a bit before disposing this._gameplay, see StartGameplay()

		public void Pause() => this._gameplay.Pause();

		public void Resume() => this._gameplay.Resume();
	}
}
