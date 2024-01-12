using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
    public static class ServerPlayerLogic
    {
        private static Random _random = new(113);

        public static void UpdatePlayers(IGameBoard board, float deltaTime)
        {
            SpawnPlayerCharacters(board);
        }

        private static void SpawnPlayerCharacters(IGameBoard board)
        {
            for (int playerIndex = 0; playerIndex < board.players.count; playerIndex++)
            {
                IPlayer player = board.players[playerIndex];
                if (player.character == null)
                {
                    int3 proposedSpawnPosition = int3(_random.NextInt(0, (int)board.settings.playerSettings.visionSize), 0, _random.NextInt(0, (int)board.settings.playerSettings.visionSize));

                    int3 playerSpawnPosition = SpatialLogic.GetCellWithFloorNear(board.worldMap, proposedSpawnPosition);

                    float3 playerCharacterPosition = playerSpawnPosition + float3(0.5f, board.settings.playerSettings.radius * 1.01f, 0.5f);

                    board.players.CreateCharacterForPlayerAtIndex(playerIndex, playerCharacterPosition, Unity.Mathematics.quaternion.identity);
                }
            }
        }
    }
}
