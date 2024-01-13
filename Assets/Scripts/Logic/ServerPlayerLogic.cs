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

            for (int playerIndex = 0; playerIndex < board.players.count; ++playerIndex)
                UpdatePlayerCharacter(board, board.players[playerIndex].character, deltaTime);
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
        
        private static void UpdatePlayerCharacter(IGameBoard board, IPlayerCharacter playerCharacter, float deltaTime)
        {
            if (length(playerCharacter.shooting) > 0)
            {
                playerCharacter.timeSinceLastShot += deltaTime;
                if (playerCharacter.timeSinceLastShot > board.settings.playerSettings.shotCooldown)
                {
                    playerCharacter.timeSinceLastShot -= board.settings.playerSettings.shotCooldown;
            
                    // perform a shot here
                    float3 shotDirection = playerCharacter.shooting;
            
                    // find the enemy closest to shot direction
                    float minHorizontalDirDot = cos(radians(board.settings.playerSettings.horizontalAutoAimDegrees));
                    IEnemy targetEnemy = null;
                    float targetError = 0;
                    for (int enemyIndex = 0; enemyIndex < board.enemies.count; ++enemyIndex)
                    {
                        IEnemy enemy = board.enemies[enemyIndex];
                        float3 enemyDirection = enemy.position - playerCharacter.position;
                        float3 horizontalEnemyDirection = normalizesafe(enemyDirection * float3(1, 0, 1));
                        
                        float horizontalDirDot = dot(horizontalEnemyDirection, shotDirection);
                        if (horizontalDirDot > minHorizontalDirDot && length(enemyDirection) < 2 * board.settings.playerSettings.shotDistance)
                        {
                            float error = length(enemyDirection - shotDirection);
                            if (targetEnemy == null || error < targetError)
                            {
                                targetEnemy = enemy;
                                targetError = error;
                            }
                        }
                    }
                    
                    if (targetEnemy != null)
                        shotDirection = normalizesafe(targetEnemy.position - playerCharacter.position);
                    
                    float3 target = playerCharacter.position + shotDirection * board.settings.playerSettings.shotDistance;
            
                    if (board.physics.CastRay(playerCharacter.position, target, LayerMask.DEFAULT | LayerMask.ENEMY, out CastHit castHit))
                        target = playerCharacter.position + shotDirection * castHit.distance;
                    playerCharacter.Shoot(playerCharacter.position, target, castHit.normal);
                }
            }
            else
                playerCharacter.timeSinceLastShot = 0;
        }
    }
}
