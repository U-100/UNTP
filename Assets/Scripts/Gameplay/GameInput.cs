using System;
using Unity.Mathematics;
using UnityEngine;

namespace UNTP
{
	public struct GameInput
	{
		public float2 move;
		public bool startConstructionPlacement;
		public bool confirmConstructionPlacement;
		public bool cancelConstructionPlacement;
	}

	public interface IGameInputSource
	{
		GameInput input { get; }
	}

	public class GameInputSource : IGameInputSource, IDisposable
	{
		private PlayerInputActions _playerInputActions;

		public GameInputSource()
		{
			this._playerInputActions = new PlayerInputActions();
			this._playerInputActions.Enable();
		}

		public GameInput input =>
			new GameInput
			{
				move = this._playerInputActions.Player.Move.ReadValue<Vector2>(),
				startConstructionPlacement = this._playerInputActions.Player.StartConstructionPlacement.WasPerformedThisFrame(),
				confirmConstructionPlacement = this._playerInputActions.Player.ConfirmConstructionPlacement.WasPerformedThisFrame(),
				cancelConstructionPlacement = this._playerInputActions.Player.CancelConstructionPlacement.WasPerformedThisFrame(),
			};

		public void Dispose()
		{
			this._playerInputActions?.Disable();
			
			if(Application.isPlaying) // protection from errors when stopping in edit mode
				this._playerInputActions?.Dispose();
			
			this._playerInputActions = null;
		}
	}
}
