using Ugol.BehaviourTree;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
    public static class PlayerLogic
    {
        private static Random _random = new(113);

        public static void ServerUpdate(IGameBoard board, float deltaTime)
        {
            SpawnPlayerCharacters(board);

            for (int playerIndex = 0; playerIndex < board.players.count; ++playerIndex)
                ServerUpdatePlayerCharacter(board, board.players[playerIndex].character, deltaTime);
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
        
        private static void ServerUpdatePlayerCharacter(IGameBoard board, IPlayerCharacter playerCharacter, float deltaTime)
        {
            if (length(playerCharacter.shooting) > 0)
            {
                playerCharacter.timeSinceLastShot += deltaTime;
                if (playerCharacter.timeSinceLastShot > board.settings.playerSettings.shotCooldown)
                {
                    playerCharacter.timeSinceLastShot -= board.settings.playerSettings.shotCooldown;
            
                    // perform a shot here
                    float3 target = FindShotTarget(board, playerCharacter);
            
                    if (board.physics.CastRay(playerCharacter.position, target, LayerMask.DEFAULT | LayerMask.ENEMY, out CastHit castHit))
                    {
                        target = lerp(playerCharacter.position, target, castHit.distance / length(target - playerCharacter.position));

                        IEnemy enemyHit = castHit.collider?.asEnemy;
                        if (enemyHit != null)
                            enemyHit.health -= board.settings.playerSettings.shotDamage;
                    }
                    playerCharacter.Shoot(playerCharacter.position, target, castHit.normal);
                }
            }
            else
                playerCharacter.timeSinceLastShot = 0;
        }

        private static float3 FindShotTarget(IGameBoard board, IPlayerCharacter playerCharacter)
        {
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
                    
            return playerCharacter.position + shotDirection * board.settings.playerSettings.shotDistance;
        }

        public static Status ClientUpdate(IGameBoard board, float deltaTime) =>
            board.players.localPlayer.character != null // if this client's player character is present
            && UpdateChunksNearPlayer(board)
            && (
                Move(board, deltaTime)
                + Aim(board, deltaTime)
                + Construct(board, deltaTime)
            );

        private static Status UpdateChunksNearPlayer(IGameBoard board)
        {
            IPlayerCharacter localPlayerCharacter = board.players.localPlayer.character;

            board.worldMap.MaterializePhysicalRange(localPlayerCharacter.position - 2, localPlayerCharacter.position + 2);
            board.worldMap.MaterializeVisualRange(localPlayerCharacter.position - board.settings.playerSettings.visionSize, localPlayerCharacter.position + board.settings.playerSettings.visionSize);

            return Status.COMPLETE;
        }

        private static Status Move(IGameBoard board, float deltaTime)
        {
            float2 moveInput = board.input.move;
            float3 velocity = float3(moveInput.x, 0, moveInput.y) * board.settings.playerSettings.speed;
            float3 movement = velocity * deltaTime;

            IPlayer localPlayer = board.players.localPlayer;

            MoveSphericalCharacterResult moveSphericalCharacterResult =
                CharacterMovementLogic.MoveSphericalCharacter(
                    board.physics,
                    localPlayer.character.position,
                    localPlayer.character.rotation,
                    board.settings.playerSettings.radius,
                    movement,
                    board.settings.playerSettings.stepHeight,
                    deltaTime,
                    CharacterMovementLogic.DEFAULT_SKIN_WIDTH,
                    CharacterMovementLogic.DEFAULT_MOVE_ITERATIONS
                );

            localPlayer.character.position = moveSphericalCharacterResult.position;
            localPlayer.character.rotation = moveSphericalCharacterResult.rotation;

            return Status.RUNNING;
        }

        private static Status Aim(IGameBoard board, float deltaTime)
        {
            float2 inputFireAim = board.input.fireAim;

            IPlayer localPlayer = board.players.localPlayer;
            IPlayerCamera localPlayerCamera = localPlayer.playerCamera;
            IPlayerCharacter localPlayerCharacter = localPlayer.character;

            float3 horizontalCameraForward = normalizesafe(localPlayerCamera.forward * float3(1, 0, 1));
            float3 horizontalCameraRight = normalizesafe(localPlayerCamera.right * float3(1, 0, 1));
            
            localPlayerCharacter.shooting = horizontalCameraRight * inputFireAim.x + horizontalCameraForward * inputFireAim.y;
            
            //UnityEngine.Debug.DrawRay(localPlayerCharacter.position, localPlayerCharacter.shooting, UnityEngine.Color.red);

            localPlayerCharacter.aimPosition = lengthsq(localPlayerCharacter.shooting) > 0 ? FindShotTarget(board, localPlayerCharacter) : localPlayerCharacter.defaultAimPosition;
            
            return Status.RUNNING;
        }

        private static Status Construct(IGameBoard board, float deltaTime) => StartConstruction(board) && ApplyConstructionState(board, deltaTime) && FinishConstruction(board);

        private static Status FinishConstruction(IGameBoard board) => CompleteConstruction(board) + CancelConstruction(board);

        private static Status StartConstruction(IGameBoard board) =>
            board.players.localPlayer.constructionState != ConstructionState.NoConstruction ||
            board.input.startConstructionPlacement && StartConstructionPlacement(board);

        private static Status StartConstructionPlacement(IGameBoard board)
        {
            IPlayer localPlayer = board.players.localPlayer;
            float3 blueprintCellPos = BlueprintCellPos(board);
            localPlayer.blueprint.position = blueprintCellPos;
            localPlayer.constructionState = ConstructionState.ConstructionAllowed;

            return Status.COMPLETE;
        }

        private static Status ApplyConstructionState(IGameBoard board, float deltaTime)
        {
            IPlayer localPlayer = board.players.localPlayer;

            float3 newBlueprintPos = BlueprintCellPos(board);
            float3 oldBlueprintPos = localPlayer.blueprint.position;

            float fullDistance = length(newBlueprintPos - oldBlueprintPos);
            float maxDistance = board.settings.playerSettings.speed * 4.0f * deltaTime;
            float distance = min(fullDistance, maxDistance);

            float3 interpolatedBlueprintPos = fullDistance > 0 ? lerp(oldBlueprintPos, newBlueprintPos, distance / fullDistance) : newBlueprintPos;

            localPlayer.blueprint.position = interpolatedBlueprintPos;

            if (board.physics.CheckBox(newBlueprintPos + float3(0.5f), float3(0.45f), LayerMask.ALL))
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

        private static Status CompleteConstruction(IGameBoard board)
        {
            if (board.input.confirmConstructionPlacement)
            {
                IPlayer localPlayer = board.players.localPlayer;
                localPlayer.constructionState = ConstructionState.NoConstruction;
                localPlayer.blueprint.active = false;

                int3 blueprintPosition = (int3)BlueprintCellPos(board);
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

        private static float3 BlueprintCellPos(IGameBoard board)
        {
            IPlayerCharacter localPlayerCharacter = board.players.localPlayer.character;
            return floor(localPlayerCharacter.position + localPlayerCharacter.forward * (board.settings.playerSettings.radius + 1.0f));
        }

        private static Status CancelConstruction(IGameBoard board)
        {
            if (board.input.cancelConstructionPlacement)
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
