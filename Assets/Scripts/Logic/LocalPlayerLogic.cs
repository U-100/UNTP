using Ugol.BehaviourTree;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	public class LocalPlayerLogic
	{
		private readonly PlayerSettings _playerSettings;
		private readonly IGameInputSource _gameInputSource;
		private readonly IGamePhysics _gamePhysics;

		public LocalPlayerLogic(PlayerSettings playerSettings, IGameInputSource gameInputSource, IGamePhysics gamePhysics)
		{
			this._playerSettings = playerSettings;
			this._gameInputSource = gameInputSource;
			this._gamePhysics = gamePhysics;
		}

		public Status Update(IGameBoard board, float deltaTime) =>
			board.players.localPlayer.character != null
			&& UpdateChunksNearPlayer(board)
			&& (
				Move(board, deltaTime)
				+ Shoot(board, deltaTime)
				+ Construct(board, deltaTime)
			);

		private Status UpdateChunksNearPlayer(IGameBoard board)
		{
			IPlayerCharacter localPlayerCharacter = board.players.localPlayer.character;

			if (localPlayerCharacter == null) // if this client's player character is missing
				return Status.FAILED;

			// update chunks near this client's player character
			board.worldMap.MaterializePhysicalRange(localPlayerCharacter.position - 2, localPlayerCharacter.position + 2);
			board.worldMap.MaterializeVisualRange(localPlayerCharacter.position - this._playerSettings.visionSize, localPlayerCharacter.position + this._playerSettings.visionSize);

			return Status.COMPLETE;
		}

		private Status Move(IGameBoard board, float deltaTime)
		{
			// move and rotate this.client's NetworkPlayerCharacter
			float2 moveInput = this._gameInputSource.input.move;
			float3 velocity = float3(moveInput.x, 0, moveInput.y) * this._playerSettings.speed;
			float3 movement = velocity * deltaTime;

			IPlayer localPlayer = board.players.localPlayer;

			MoveSphericalCharacterResult moveSphericalCharacterResult =
				CharacterMovementLogic.MoveSphericalCharacter(
					this._gamePhysics,
					localPlayer.character.position,
					localPlayer.character.rotation,
					this._playerSettings.radius,
					movement,
					this._playerSettings.stepHeight,
					deltaTime,
					CharacterMovementLogic.DEFAULT_SKIN_WIDTH,
					CharacterMovementLogic.DEFAULT_MOVE_ITERATIONS
				);

			localPlayer.character.position = moveSphericalCharacterResult.position;
			localPlayer.character.rotation = moveSphericalCharacterResult.rotation;

			return Status.RUNNING;
		}

		private Status Shoot(IGameBoard board, float deltaTime)
		{
			IPlayerCharacter localPlayerCharacter = board.players.localPlayer.character;
			
			float2 aimInput = this._gameInputSource.input.aim;
			float3 aimDirection = /*length(aimInput) > 0 ? normalize(float3(aimInput.x, 0, aimInput.y)) : */localPlayerCharacter.forward;

			if(this._gameInputSource.input.shoot)
				localPlayerCharacter.Shoot(localPlayerCharacter.position, aimDirection);
			
			return Status.RUNNING;
		}

		private Status Construct(IGameBoard board, float deltaTime) => StartConstruction(board) && ApplyConstructionState(board, deltaTime) && FinishConstruction(board);

		private Status FinishConstruction(IGameBoard board) => CompleteConstruction(board) + CancelConstruction(board);

		private Status StartConstruction(IGameBoard board) => board.players.localPlayer.constructionState != ConstructionState.NoConstruction || this._gameInputSource.input.startConstructionPlacement && StartConstructionPlacement(board.players.localPlayer);

		private Status StartConstructionPlacement(IPlayer localPlayer)
		{
			float3 blueprintCellPos = BlueprintCellPos(localPlayer.character);
			localPlayer.blueprint.position = blueprintCellPos;
			localPlayer.constructionState = ConstructionState.ConstructionAllowed;

			return Status.COMPLETE;
		}

		private Status ApplyConstructionState(IGameBoard board, float deltaTime)
		{
			IPlayer localPlayer = board.players.localPlayer;

			float3 newBlueprintPos = BlueprintCellPos(localPlayer.character);
			float3 oldBlueprintPos = localPlayer.blueprint.position;

			float fullDistance = length(newBlueprintPos - oldBlueprintPos);
			float maxDistance = this._playerSettings.speed * 4.0f * deltaTime;
			float distance = min(fullDistance, maxDistance);

			float3 interpolatedBlueprintPos = fullDistance > 0 ? lerp(oldBlueprintPos, newBlueprintPos, distance / fullDistance) : newBlueprintPos;

			localPlayer.blueprint.position = interpolatedBlueprintPos;

			if (this._gamePhysics.CheckBox(newBlueprintPos + float3(0.5f), float3(0.45f)))
			{
				localPlayer.blueprint.active = false;
				localPlayer.constructionState = ConstructionState.ConstructionRestricted;
			}
			else
			{
				if (!localPlayer.blueprint.active) // if blueprint was previously hidden
					localPlayer.blueprint.position = newBlueprintPos; // then reposition it properly upon activation
				
				localPlayer.blueprint.active = true;
				localPlayer.constructionState = ConstructionState.ConstructionAllowed;
			}

			return Status.COMPLETE;
		}

		private Status CompleteConstruction(IGameBoard board)
		{
			if (this._gameInputSource.input.confirmConstructionPlacement)
			{
				IPlayer localPlayer = board.players.localPlayer;
				localPlayer.constructionState = ConstructionState.NoConstruction;
				localPlayer.blueprint.active = false;

				int3 blueprintPosition = (int3)BlueprintCellPos(localPlayer.character);
				board.worldMap[blueprintPosition] =
					new WorldMapCell
					{
						mask = Corner.All,
						objectId = 1
					};
				return Status.COMPLETE;
			}

			return Status.RUNNING;
		}

		private float3 BlueprintCellPos(IPlayerCharacter playerCharacter) => floor(playerCharacter.position + playerCharacter.forward * (this._playerSettings.radius + 1.0f));

		private Status CancelConstruction(IGameBoard board)
		{
			if (this._gameInputSource.input.cancelConstructionPlacement)
			{
				IPlayer localPlayer = board.players.localPlayer;
				localPlayer.constructionState = ConstructionState.NoConstruction;
				localPlayer.blueprint.active = false;
				return Status.COMPLETE;
			}

			return Status.RUNNING;
		}
	}
}
