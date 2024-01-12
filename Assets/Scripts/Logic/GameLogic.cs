namespace UNTP
{
	public interface IGameLogic
	{
		void Update(IGameBoard gameBoard, float deltaTime);
	}

	public class GameLogic : IGameLogic
	{
		public void Update(IGameBoard board, float deltaTime)
		{
			if (board.game.isServer)
			{
				if (!board.game.isPaused)
				{
					ServerPlayerLogic.UpdatePlayers(board, deltaTime);
					EnemyLogic.UpdateEnemies(board, deltaTime);
				}
			}

			if (board.game.isClient)
				LocalPlayerLogic.Update(board, deltaTime);
		}
	}
}
