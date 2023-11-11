using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	public interface IGameLogic
	{
		void Update(IGameBoard gameBoard, float deltaTime);
	}

	public class GameLogic : IGameLogic
	{
		private readonly PlayerSettings _playerSettings;
		private readonly LocalPlayerLogic _localPlayerLogic;
		private readonly EnemyLogic _enemyLogic;

		private Random _random = new(113);

		public GameLogic(PlayerSettings playerSettings, LocalPlayerLogic localPlayerLogic, EnemyLogic enemyLogic)
		{
			this._playerSettings = playerSettings;
			this._localPlayerLogic = localPlayerLogic;
			this._enemyLogic = enemyLogic;
		}

		public void Update(IGameBoard board, float deltaTime)
		{
			if (board.game.isServer)
			{
				if (!board.game.isPaused)
				{
					SpawnPlayerCharacters(board);
					this._enemyLogic.UpdateEnemies(board, deltaTime);
				}
			}

			if (board.game.isClient)
				this._localPlayerLogic.Update(board, deltaTime);
		}

		private void SpawnPlayerCharacters(IGameBoard board)
		{
			for (int playerIndex = 0; playerIndex < board.players.count; playerIndex++)
			{
				IPlayer player = board.players[playerIndex];
				if (player.character == null)
				{
					int3 proposedSpawnPosition = int3(this._random.NextInt(0, (int)this._playerSettings.visionSize), 0, this._random.NextInt(0, (int)this._playerSettings.visionSize));

					int3 playerSpawnPosition = SpatialLogic.GetCellWithFloorNear(board.worldMap, proposedSpawnPosition);

					float3 playerCharacterPosition = playerSpawnPosition + float3(0.5f, this._playerSettings.radius * 1.01f, 0.5f);

					board.players.CreateCharacterForPlayerAtIndex(playerIndex, playerCharacterPosition, Unity.Mathematics.quaternion.identity);
				}
			}
		}
	}
}
