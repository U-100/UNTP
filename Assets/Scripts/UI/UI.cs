using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace UNTP
{
	public enum MainViewModelState
	{
		Idle,
		HostLobby,
		ClientLobby,
		GamePlaying,
		GamePaused,
	}

	public interface IMainViewModel
	{
		MainViewModelState state { get; }

		bool connected { get; }
		int playersCount { get; }

		IHudViewModel hud { get; }
		
		void StartHost();
		void StartClient();

		void Play();
		void Stop();
		void Pause();
		void Resume();
	}

	public enum HudIndicatorKind
	{
		Ally,
		SmallEnemy,
		BigEnemy,
	}
	
	public struct HudIndicatorData
	{
		public HudIndicatorKind kind;
		public float2 positionScreenFactor;
	}

	public interface IHudViewModel : IDisposable
	{
		ConstructionState constructionState { get; }
		
		public IReadOnlyList<HudIndicatorData> CalculateIndicators();

		void SetFireAim(float2 fireAim);
	}

	public class UI : MonoBehaviour
	{
		[SerializeField]
		private float _hudLeftStickMaxDelta = 50;


		private VisualElement _rootVisualElement;


		private VisualElement _mainMenu;
		private Button _mainMenuHostButton;
		private Button _mainMenuJoinButton;


		private VisualElement _loading;


		private VisualElement _hostLobby;
		private Button _hostLobbyPlayButton;
		private Button _hostLobbyCancelButton;
		private Label _hostLobbyPlayersCounterLabel;


		private VisualElement _clientLobby;
		private Button _clientLobbyCancelButton;


		private VisualElement _errorDialog;
		private Button _errorDialogCloseButton;
		private Label _errorDialogMessageLabel;


		private VisualElement _pauseDialog;
		private Button _pauseDialogResumeButton;
		private Button _pauseDialogQuitButton;
		private Label _clientLobbyConnectedLabel;


		private VisualElement _hud;
		
		private VisualElement _hudIndicatorsArea;
		
		private VisualElement _hudLeftStickOuterKnob;
		private VisualElement _hudLeftStickInnerKnob;
		private VisualElement _hudLeftStickTouchArea;
		private Button _hudPauseButton;
		private Button _hudStartConstructionPlacementButton;
		private Button _hudConfirmConstructionPlacementButton;
		private Button _hudCancelConstructionPlacementButton;
		private HudFireStick _hudFireStick;

		private Gamepad _fakeGamepad;
		

		public IMainViewModel viewModel { get; set; }

		void OnEnable()
		{
			this._rootVisualElement = GetComponent<UIDocument>().rootVisualElement;


			this._mainMenu = this._rootVisualElement.Q("MainMenu");

			this._mainMenuHostButton = this._mainMenu.Q<Button>("HostButton");
			this._mainMenuHostButton.clicked += this.StartHost;

			this._mainMenuJoinButton = this._mainMenu.Q<Button>("JoinButton");
			this._mainMenuJoinButton.clicked += this.StartClient;


			this._loading = this._rootVisualElement.Q("Loading");


			this._hostLobby = this._rootVisualElement.Q("HostLobby");

			this._hostLobbyPlayButton = this._hostLobby.Q<Button>("HostLobbyPlayButton");
			this._hostLobbyPlayButton.clicked += this.OnHostLobbyPlay;

			this._hostLobbyCancelButton = this._hostLobby.Q<Button>("HostLobbyCancelButton");
			this._hostLobbyCancelButton.clicked += this.OnHostLobbyCancel;

			this._hostLobbyPlayersCounterLabel = this._hostLobby.Q<Label>("HostLobbyPlayersCounter");


			this._clientLobby = this._rootVisualElement.Q("ClientLobby");

			this._clientLobbyCancelButton = this._clientLobby.Q<Button>("ClientLobbyCancelButton");
			this._clientLobbyCancelButton.clicked += this.OnClientLobbyCancel;

			this._clientLobbyConnectedLabel = this._clientLobby.Q<Label>("ClientLobbyConnected");


			this._errorDialog = this._rootVisualElement.Q("ErrorDialog");
			this._errorDialogCloseButton = this._errorDialog.Q<Button>("ErrorDialogCloseButton");
			this._errorDialogMessageLabel = this._errorDialog.Q<Label>("ErrorDialogMessage");


			this._pauseDialog = this._rootVisualElement.Q("PauseDialog");

			this._pauseDialogResumeButton = _pauseDialog.Q<Button>("PauseDialogResumeButton");
			this._pauseDialogResumeButton.clicked += this.OnPauseDialogResume;

			this._pauseDialogQuitButton = _pauseDialog.Q<Button>("PauseDialogQuitButton");
			this._pauseDialogQuitButton.clicked += this.OnPauseDialogQuit;


			this._hud = this._rootVisualElement.Q("Hud");

			this._hudIndicatorsArea = this._hud.Q<VisualElement>("HudIndicatorsArea");

			this._hudLeftStickOuterKnob = this._hud.Q<VisualElement>("HudLeftStickOuterKnob");
			this._hudLeftStickInnerKnob = this._hud.Q<VisualElement>("HudLeftStickInnerKnob");
			this._hudLeftStickTouchArea = this._hud.Q<VisualElement>("HudLeftStickTouchArea");

			this._hudLeftStickTouchArea.RegisterCallback<PointerDownEvent>(this.OnHudLeftStickTouchAreaPointerDown);
			this._hudLeftStickTouchArea.RegisterCallback<PointerMoveEvent>(this.OnHudLeftStickTouchAreaPointerMove);
			this._hudLeftStickTouchArea.RegisterCallback<PointerUpEvent>(this.OnHudLeftStickTouchAreaPointerUp);
			this._hudLeftStickTouchArea.RegisterCallback<PointerLeaveEvent>(this.OnHudLeftStickTouchAreaPointerLeave);

			this._hudPauseButton = this._hud.Q<Button>("HudPauseButton");
			this._hudPauseButton.clicked += this.OnHudPause;

			this._hudStartConstructionPlacementButton = this._hud.Q<Button>("HudStartConstructionPlacementButton");
			this._hudStartConstructionPlacementButton.clicked += this.OnHudStartConstructionPlacement;

			this._hudConfirmConstructionPlacementButton = this._hud.Q<Button>("HudConfirmConstructionPlacementButton");
			this._hudConfirmConstructionPlacementButton.clicked += this.OnHudConfirmConstructionPlacement;

			this._hudCancelConstructionPlacementButton = this._hud.Q<Button>("HudCancelConstructionPlacementButton");
			this._hudCancelConstructionPlacementButton.clicked += this.OnHudCancelConstructionPlacement;

			this._hudFireStick = this._hud.Q<HudFireStick>("HudFireStick");

			this._fakeGamepad = InputSystem.AddDevice<Gamepad>();
		}

		void OnDisable()
		{
			this._mainMenuHostButton.clicked -= this.StartHost;
			this._mainMenuJoinButton.clicked -= this.StartClient;

			this._hostLobbyPlayButton.clicked -= this.OnHostLobbyPlay;
			this._hostLobbyCancelButton.clicked -= this.OnHostLobbyCancel;

			this._clientLobbyCancelButton.clicked -= this.OnClientLobbyCancel;

			this._pauseDialogResumeButton.clicked -= this.OnPauseDialogResume;
			this._pauseDialogQuitButton.clicked -= this.OnPauseDialogQuit;

			this._hudLeftStickTouchArea.UnregisterCallback<PointerDownEvent>(this.OnHudLeftStickTouchAreaPointerDown);
			this._hudLeftStickTouchArea.UnregisterCallback<PointerMoveEvent>(this.OnHudLeftStickTouchAreaPointerMove);
			this._hudLeftStickTouchArea.UnregisterCallback<PointerUpEvent>(this.OnHudLeftStickTouchAreaPointerUp);
			this._hudLeftStickTouchArea.UnregisterCallback<PointerLeaveEvent>(this.OnHudLeftStickTouchAreaPointerLeave);

			InputSystem.RemoveDevice(this._fakeGamepad);
		}

		void Update()
		{
			if (this.viewModel == null) return;

			UpdateMainMenu();
			UpdateHostGameView();
			UpdateClientGameView();
			UpdatePauseView();
			UpdateHud();
		}


		private static (bool activated, bool deactivated) SetScreenEnabled(VisualElement screenVisualElement, bool screenEnabled)
		{
			bool wasScreenEnabled = screenVisualElement.enabledInHierarchy;
			screenVisualElement.SetEnabled(screenVisualElement.visible = screenEnabled);

			return (activated: !wasScreenEnabled && screenEnabled, deactivated: wasScreenEnabled && !screenEnabled);
		}

		private void UpdateMainMenu()
		{
			SetScreenEnabled(this._mainMenu, this.viewModel.state == MainViewModelState.Idle);
		}

		private void StartHost() => this.viewModel.StartHost();
		private void StartClient() => this.viewModel.StartClient();


		private void UpdateHostGameView()
		{
			SetScreenEnabled(this._hostLobby, this.viewModel.state == MainViewModelState.HostLobby);

			if (this._hostLobby.visible)
				this._hostLobbyPlayersCounterLabel.text = this.viewModel.playersCount.ToString();
		}

		private void OnHostLobbyPlay() => this.viewModel.Play();
		private void OnHostLobbyCancel() => this.viewModel.Stop();


		private void UpdateClientGameView()
		{
			SetScreenEnabled(this._clientLobby, this.viewModel.state == MainViewModelState.ClientLobby);

			if (this._clientLobby.visible)
				this._clientLobbyConnectedLabel.visible = this.viewModel.connected;
			else
				this._clientLobbyConnectedLabel.style.visibility = StyleKeyword.Null;
		}

		private void OnClientLobbyCancel() => this.viewModel.Stop();


		private void UpdatePauseView()
		{
			SetScreenEnabled(this._pauseDialog, this.viewModel.state == MainViewModelState.GamePaused);
		}

		private void OnPauseDialogQuit() => this.viewModel.Stop();
		private void OnPauseDialogResume() => this.viewModel.Resume();


		private void UpdateHud()
		{
			(bool activated, bool deactivated) = SetScreenEnabled(this._hud, this.viewModel.state == MainViewModelState.GamePlaying);

			if (activated)
			{
				this._hudLeftStickOuterKnob.visible = false;
				InputSystem.QueueDeltaStateEvent(this._fakeGamepad.leftStick, Vector2.zero);
			}

			if (this._hud.visible)
			{
				this._hudStartConstructionPlacementButton.visible = this.viewModel.hud?.constructionState == ConstructionState.NoConstruction;
				this._hudConfirmConstructionPlacementButton.visible = this.viewModel.hud?.constructionState == ConstructionState.ConstructionAllowed;
				this._hudCancelConstructionPlacementButton.visible = this.viewModel.hud?.constructionState != ConstructionState.NoConstruction;

				this.viewModel.hud?.SetFireAim(this._hudFireStick.value);

				IReadOnlyList<HudIndicatorData> indicators = this.viewModel.hud?.CalculateIndicators();
				int indicatorsCount = indicators?.Count ?? 0;
				
				// ensure that we have no more hud indicators then we have enemies
				while (this._hudIndicatorsArea.childCount > indicatorsCount)
					this._hudIndicatorsArea.RemoveAt(indicatorsCount);

				// ensure that we have enough hud indicators for all enemies
				while (this._hudIndicatorsArea.childCount < indicatorsCount)
					this._hudIndicatorsArea.Add(new HudIndicator());

				if (indicators is not null)
				{
					for (int indicatorIndex = 0; indicatorIndex < indicatorsCount; ++indicatorIndex)
					{
						if (this._hudIndicatorsArea.ElementAt(indicatorIndex) is HudIndicator hudIndicator)
						{
							HudIndicatorData hudIndicatorData = indicators[indicatorIndex];
							hudIndicator.EnableInClassList("ally-indicator", hudIndicatorData.kind == HudIndicatorKind.Ally);
							hudIndicator.EnableInClassList("big-enemy-indicator", hudIndicatorData.kind == HudIndicatorKind.BigEnemy);
							hudIndicator.EnableInClassList("small-enemy-indicator", hudIndicatorData.kind == HudIndicatorKind.SmallEnemy);
							// TODO: don't forget to add classes for new enum values, or rework this code

							hudIndicator.xFactor = hudIndicatorData.positionScreenFactor.x;
							hudIndicator.yFactor = hudIndicatorData.positionScreenFactor.y;
						}
					}
				}
			}
			else
			{
				this._hudStartConstructionPlacementButton.style.visibility = StyleKeyword.Null;
				this._hudConfirmConstructionPlacementButton.style.visibility = StyleKeyword.Null;
				this._hudCancelConstructionPlacementButton.style.visibility = StyleKeyword.Null;
			}
		}

		private bool hudLeftStickActive => this._hudLeftStickOuterKnob.visible;

		private Vector2 hudLeftStickPosition => new Vector2(this._hudLeftStickOuterKnob.style.left.value.value, this._hudLeftStickOuterKnob.style.top.value.value);

		private void OnHudLeftStickTouchAreaPointerDown(PointerDownEvent evt)
		{
			this._hudLeftStickOuterKnob.visible = true;

			this._hudLeftStickOuterKnob.style.left = evt.position.x;
			this._hudLeftStickOuterKnob.style.top = evt.position.y;

			this._hudLeftStickInnerKnob.style.left = 0;
			this._hudLeftStickInnerKnob.style.top = 0;

			InputSystem.QueueDeltaStateEvent(this._fakeGamepad.leftStick, Vector2.zero);
		}

		private void OnHudLeftStickTouchAreaPointerUp(PointerUpEvent evt)
		{
			this._hudLeftStickOuterKnob.visible = false;
			InputSystem.QueueDeltaStateEvent(this._fakeGamepad.leftStick, Vector2.zero);
		}

		private void OnHudLeftStickTouchAreaPointerLeave(PointerLeaveEvent evt)
		{
			this._hudLeftStickOuterKnob.visible = false;
			InputSystem.QueueDeltaStateEvent(this._fakeGamepad.leftStick, Vector2.zero);
		}

		private void OnHudLeftStickTouchAreaPointerMove(PointerMoveEvent evt)
		{
			if (this.hudLeftStickActive)
			{
				Vector2 evtPos = new Vector2(evt.position.x, evt.position.y);
				Vector2 delta = Vector2.ClampMagnitude(evtPos - this.hudLeftStickPosition, this._hudLeftStickMaxDelta);

				this._hudLeftStickInnerKnob.style.left = delta.x;
				this._hudLeftStickInnerKnob.style.top = delta.y;

				InputSystem.QueueDeltaStateEvent(this._fakeGamepad.leftStick, new Vector2(delta.x / this._hudLeftStickMaxDelta, (-1) * delta.y / this._hudLeftStickMaxDelta));
			}
		}

		private void OnHudPause() => this.viewModel.Pause();

		private void OnHudStartConstructionPlacement() => PressInputDeviceButton(this._fakeGamepad.triangleButton);

		private void OnHudConfirmConstructionPlacement() => PressInputDeviceButton(this._fakeGamepad.crossButton);

		private void OnHudCancelConstructionPlacement() => PressInputDeviceButton(this._fakeGamepad.circleButton);

		private static void PressInputDeviceButton(ButtonControl buttonControl)
		{
			using (StateEvent.From(buttonControl.device, out InputEventPtr eventPtr))
			{
				buttonControl.WriteValueIntoEvent(1f, eventPtr);
				InputSystem.QueueEvent(eventPtr);

				buttonControl.WriteValueIntoEvent(0f, eventPtr);
				InputSystem.QueueEvent(eventPtr);
			}
		}
	}
}
