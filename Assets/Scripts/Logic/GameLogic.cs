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
					PlayerLogic.ServerUpdate(board, deltaTime);
					EnemyLogic.UpdateEnemies(board, deltaTime);
				}
			}

			if (board.game.isClient)
				PlayerLogic.ClientUpdate(board, deltaTime);
		}
	}
}
