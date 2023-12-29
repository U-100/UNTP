using Ugol.BehaviourTree;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UNTP
{
    public static class LocalPlayerLogic
    {
        public static Status Update(IGameBoard board, float deltaTime) =>
            board.players.localPlayer.character != null // if this client's player character is present
            && UpdateChunksNearPlayer(board)
            && (
                Move(board, deltaTime)
                + Shoot(board, deltaTime)
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

        private static Status Shoot(IGameBoard board, float deltaTime)
        {
            IPlayerCharacter localPlayerCharacter = board.players.localPlayer.character;

            float2 inputFireAim = board.input.fireAim;
            if (length(inputFireAim) > 0)
            {
                localPlayerCharacter.timeSinceLastShot += deltaTime;
                if (localPlayerCharacter.timeSinceLastShot > board.settings.playerSettings.shotCooldown)
                {
                    localPlayerCharacter.timeSinceLastShot -= board.settings.playerSettings.shotCooldown;

                    // perform a shot here
                    float3 shotDirection = float3(inputFireAim.x, 0, inputFireAim.y);

                    // find the enemy closest to shot direction
                    IEnemy targetEnemy = null;
                    float targetError = 0;
                    for (int enemyIndex = 0; enemyIndex < board.enemies.count; ++enemyIndex)
                    {
                        IEnemy enemy = board.enemies[enemyIndex];
                        float3 enemyDirection = enemy.position - localPlayerCharacter.position;
                        float dotDir = dot(enemyDirection, shotDirection);
                        if (dotDir > 0 && length(enemyDirection) < 2 * board.settings.playerSettings.shotDistance)
                        {
                            float error = length(enemyDirection - dotDir * shotDirection);
                            if (targetEnemy == null || error < targetError)
                            {
                                targetEnemy = enemy;
                                targetError = error;
                            }
                        }
                    }

                    if (targetEnemy != null)
                        shotDirection = normalize(targetEnemy.position - localPlayerCharacter.position);
                    
                    float3 target = localPlayerCharacter.position + shotDirection * board.settings.playerSettings.shotDistance;

                    if (board.physics.CastRay(localPlayerCharacter.position, target, out CastHit castHit))
                        target = localPlayerCharacter.position + shotDirection * castHit.distance;
                    localPlayerCharacter.Shoot(localPlayerCharacter.position, target, castHit.normal);
                }
            }
            else
                localPlayerCharacter.timeSinceLastShot = 0;

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

            if (board.physics.CheckBox(newBlueprintPos + float3(0.5f), float3(0.45f)))
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
