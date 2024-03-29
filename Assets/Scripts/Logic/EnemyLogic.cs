using Ugol.BehaviourTree;
using Ugol.Pathfinding;
using Ugol.UnityMathematicsExtensions;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	public static class EnemyLogic
	{
		private static Random _random = new(113);

		public static void UpdateEnemies(IGameBoard board, float deltaTime)
		{
			SpawnEnemies(board);
			UpdatePhysicalChunksNearEnemies(board);
			BehaveEnemies(board, deltaTime);
		}

		private static void SpawnEnemies(IGameBoard board)
		{
			if (board.enemies.count < board.settings.enemySettings.enemiesPerPlayer * board.players.count)
			{
				int randomPlayerIndex = _random.NextInt(0, board.players.count);
				IPlayer randomPlayer = board.players[randomPlayerIndex];

				float randomAngle = _random.NextFloat(0.0f, radians(360.0f));
				float randomRange = _random.NextFloat(board.settings.enemySettings.spawnDistanceRange[0], board.settings.enemySettings.spawnDistanceRange[1]);
				float3 proposedEnemyPosition = randomPlayer.character.position + mul(Unity.Mathematics.quaternion.RotateY(randomAngle), float3(1, 0, 0)) * randomRange;
				
				if (board.enemies.count == 0)
				{
					float3 striderPosition = SpatialLogic.FindSurfaceCellMidUnder(board.worldMap, proposedEnemyPosition) + float3(0, 1, 0);
					IStrider strider = board.enemies.CreateStrider(striderPosition);
					strider.health = board.settings.enemySettings.striderHealth;
					SetupStriderLegs(board, strider);
				}
				else
				{
					float3 walkerPosition = SpatialLogic.GetCellWithFloorNear(board.worldMap, (int3)proposedEnemyPosition) + float3(0.5f);
					IWalker walker = board.enemies.CreateWalker(walkerPosition);
					walker.health = board.settings.enemySettings.walkerHealth;
				}
			}
		}

		private static void UpdatePhysicalChunksNearEnemies(IGameBoard board)
		{
			for (int enemyIndex = 0; enemyIndex < board.enemies.count; ++enemyIndex)
			{
				IEnemy enemy = board.enemies[enemyIndex];
				board.worldMap.MaterializePhysicalRange(enemy.position - 2, enemy.position + 2);
			}
		}

		private static void BehaveEnemies(IGameBoard board, float deltaTime)
		{
			for (int enemyIndex = 0; enemyIndex < board.enemies.count;)
			{
				Status status = board.enemies[enemyIndex] switch
				{
					IWalker walker => UpdateWalker(board, walker, deltaTime),
					IStrider strider => UpdateStrider(board, strider, deltaTime),
					_ => throw new System.Exception($"don't know how to update {board.enemies[enemyIndex].GetType()}")
				};
				
				if (status == Status.COMPLETE)
				{
					// destroy enemy upon completing its behaviour
					board.enemies.DestroyEnemyAtIndex(enemyIndex);
					continue;
				}

				// current enemy still alive, update next enemy
				++enemyIndex;
			}
		}

		private static bool FindNearestPlayer(IGameBoard board, float3 position, out float3 nearestPlayerPosition, out float distanceToNearestPlayer)
		{
			if(board.players.count == 0 || board.players[0].character == null)
			{
				nearestPlayerPosition = default;
				distanceToNearestPlayer = 0;
				return false; // there are no players
			}

			// find nearest player
			nearestPlayerPosition = board.players[0].character.position;
			distanceToNearestPlayer = distance(nearestPlayerPosition, position);

			for (int playerIndex = 1; playerIndex < board.players.count; playerIndex++)
			{
				IPlayerCharacter playerCharacter = board.players[playerIndex].character;
				if(playerCharacter == null)
					continue;

				float distanceToPlayer = distance(playerCharacter.position, position);
				if (distanceToPlayer < distanceToNearestPlayer)
				{
					nearestPlayerPosition = playerCharacter.position;
					distanceToNearestPlayer = distanceToPlayer;
				}
			}

			return true;
		}
		
		private static Status UpdateWalker(IGameBoard board, IWalker walker, float deltaTime)
		{
			float selfDestructionTime = 1.0f;
			if (walker.selfDestructionCountdown != null)
			{
				walker.selfDestructionCountdown -= deltaTime;
				if (walker.selfDestructionCountdown <= 0)
					return Status.COMPLETE;
				
				if (walker.selfDestructionCountdown <= selfDestructionTime * 0.5f)
					return Status.RUNNING;
			}
			
			if(!FindNearestPlayer(board, walker.position, out float3 nearestPlayerPosition, out float distanceToNearestPlayer))
				return Status.RUNNING; // just wait until we find nearest player

			// close enough and not detonating yet
			if (walker.selfDestructionCountdown == null && distanceToNearestPlayer < board.settings.enemySettings.speed * selfDestructionTime + board.settings.enemySettings.radius)
			{
				walker.InitiateSelfDestruction();
				walker.selfDestructionCountdown = selfDestructionTime;
				return Status.RUNNING;
			}
			
			// not detonating yet and has no health left
			if (walker.selfDestructionCountdown == null && walker.health <= 0)
			{
				walker.InitiateSelfDestruction();
				walker.selfDestructionCountdown = selfDestructionTime;
				return Status.RUNNING;
			}

			// rush is allowed when we have target player and we are close enough horizontally and vertically
			if (abs(nearestPlayerPosition.y - walker.position.y) < board.settings.enemySettings.radius && distanceToNearestPlayer < board.settings.enemySettings.horizontalRushDistance)
			{
				if (walker.selfDestructionCountdown == null)
				{
					walker.InitiateSelfDestruction();
					walker.selfDestructionCountdown = 2.0f;
				}
				return MoveTo(board, walker, nearestPlayerPosition, deltaTime);
			}

			if (walker.path.Count == 0 || distance(walker.path[^1], nearestPlayerPosition) > board.settings.enemySettings.pathEndToleranceDistance)
			{
				// we need a new path to follow
				int3 from = (int3)floor(walker.position - float3(0, board.settings.enemySettings.radius, 0));
				int3 maxPathAreaSize = 10;
				int3 to = (int3)floor(nearestPlayerPosition);
				SlopeWalker slopeWalker = new SlopeWalker(board.worldMap, from, maxPathAreaSize);
				AStar.FindPath(slopeWalker, from, to, walker.path);
			}

			// follow a path to player
			if (walker.path.Count > 0)
			{
				float3 position = walker.position;
				float3 targetCellMiddle = walker.path[0] + float3(0.5f);

				if (distance(targetCellMiddle, position) < board.settings.enemySettings.pathToleranceDistance)
					walker.path.RemoveAt(0);

				if (walker.path.Count > 0)
				{
					// draw path for debug purposes
					//float3 p = position;
					//foreach (var pathPoint in enemy.path)
					//{
					//	float3 pp = pathPoint + float3(0.5f, board.settings.enemySettings.radius, 0.5f);
					//	UnityEngine.Debug.DrawLine(p, pp, UnityEngine.Color.yellow);
					//	p = pp;
					//}

					float3 targetPosition = walker.path[0] + float3(0.5f, board.settings.enemySettings.radius, 0.5f);

					//UnityEngine.Debug.DrawLine(position, targetPosition, UnityEngine.Color.cyan);

					return MoveTo(board, walker, targetPosition, deltaTime);
				}
			}

			// fallback behaviour - just move in player direction
			return MoveTo(board, walker, nearestPlayerPosition, deltaTime);
		}

		private const float STRIDER_HEIGHT = 3.0f;
		private const float STRIDER_SPEED = 0.5f;
		private const float STRIDER_TURN_SPEED_DEGREES_PER_SEC = 30.0f;
		private const float STRIDER_LEG_SPEED = 3.5f;

		private static void SetupStriderLegs(IGameBoard board, IStrider strider)
		{
			for (int legIndex = 0; legIndex < strider.legs.Count; legIndex++)
			{
				IStriderLeg leg = strider.legs[legIndex];
				leg.position = leg.targetPosition = leg.oldPosition = GetDesiredLegPosition(board, strider, legIndex);
				leg.movementFraction = 1.0f;
			}
		}
		
		private static Status UpdateStrider(IGameBoard board, IStrider strider, float deltaTime)
		{
			if(!FindNearestPlayer(board, strider.position, out float3 nearestPlayerPosition, out float _))
				return Status.RUNNING; // if there are no players - just wait
			
			// aim at the nearest player
			strider.aimPosition = nearestPlayerPosition;
            
			// move towards nearest player
			float3 horizontalDirectionToTarget = nearestPlayerPosition - strider.position;
			horizontalDirectionToTarget.y = 0;
			if (length(horizontalDirectionToTarget) > 1)
			{
				horizontalDirectionToTarget = normalize(horizontalDirectionToTarget);

				float3 up = float3(0, 1, 0);
				quaternion desiredRotation = Unity.Mathematics.quaternion.LookRotationSafe(horizontalDirectionToTarget, up);

				strider.rotation = strider.rotation.RotateTowards(desiredRotation, radians(STRIDER_TURN_SPEED_DEGREES_PER_SEC * deltaTime));

				float speedByDirectionFactor = clamp(dot(horizontalDirectionToTarget, strider.forward), 0, 1);
            
				float3 horizontalMovement = strider.forward * STRIDER_SPEED * speedByDirectionFactor * deltaTime;
				strider.position += horizontalMovement;
			}            

			for (int legIndex = 0; legIndex < strider.legs.Count; legIndex++)
			{
				IStriderLeg leg = strider.legs[legIndex];

				if (legIndex != strider.currentMovingLegIndex)
				{
					leg.position = leg.targetPosition;
					float3 legForward = 0;
					legForward.xz = normalize((leg.position - strider.position).xz);
					leg.forward = legForward;
				}

				UnityEngine.Debug.DrawLine(strider.position, leg.targetPosition, legIndex == strider.currentMovingLegIndex ? UnityEngine.Color.cyan: UnityEngine.Color.green);
			}

			IStriderLeg currentMovingLeg = strider.legs[strider.currentMovingLegIndex];
			// float availableDistance = STRIDER_LEG_SPEED * deltaTime;
			// float3 deltaToTarget = currentMovingLeg.targetPosition - currentMovingLeg.position;
			// float currentDistance = length(deltaToTarget);
			// currentMovingLeg.position =
			// 	currentDistance < availableDistance
			// 		? currentMovingLeg.targetPosition
			// 		: currentMovingLeg.position + deltaToTarget / currentDistance * availableDistance;

			float availableFraction = STRIDER_LEG_SPEED * deltaTime / distance(currentMovingLeg.targetPosition, currentMovingLeg.oldPosition);
			currentMovingLeg.movementFraction = clamp(currentMovingLeg.movementFraction + availableFraction, 0, 1);
			float3 midpoint = (currentMovingLeg.oldPosition + currentMovingLeg.targetPosition) / 2 + float3(0, 2, 0);
			float3 p0 = lerp(currentMovingLeg.oldPosition, midpoint, currentMovingLeg.movementFraction); // somewhat bezier-like
			float3 p1 = lerp(midpoint, currentMovingLeg.targetPosition, currentMovingLeg.movementFraction);
			float3 pResult = lerp(p0, p1, currentMovingLeg.movementFraction);
			currentMovingLeg.position = pResult;

			float3 currentMovingLegForward = 0;
			currentMovingLegForward.xz = normalize((currentMovingLeg.position - strider.position).xz);
			currentMovingLeg.forward = currentMovingLegForward;

			const float STRIDER_LEG_CLOSE_ENOUGH_TO_TARGET = 0.001f;
			if (all(abs(currentMovingLeg.targetPosition - currentMovingLeg.position) < STRIDER_LEG_CLOSE_ENOUGH_TO_TARGET))
			{
				// pick new target for the most displaced leg and declare that leg as the one that is currently moving
				int mostMisplacedLegIndex = -1;
				float mostMisplacedLegDistanceToDesiredPosition = 0;
				float3 mostMisplacedLegDesiredPosition = 0;
				for (int legIndex = 0; legIndex < strider.legs.Count; legIndex++)
				{
					IStriderLeg leg = strider.legs[legIndex];
			
					float3 desiredLegPosition = GetDesiredLegPosition(board, strider, legIndex);
					float distanceToDesiredPosition = distance(leg.position, desiredLegPosition);
			
					if (distanceToDesiredPosition > mostMisplacedLegDistanceToDesiredPosition && legIndex != strider.currentMovingLegIndex)
					{
						mostMisplacedLegIndex = legIndex;
						mostMisplacedLegDistanceToDesiredPosition = distanceToDesiredPosition;
						mostMisplacedLegDesiredPosition = desiredLegPosition;
					}
			
					//UnityEngine.Debug.DrawLine(strider.position, desiredLegPosition, UnityEngine.Color.green);
				}
			
				if (mostMisplacedLegIndex >= 0 && mostMisplacedLegDistanceToDesiredPosition > STRIDER_LEG_CLOSE_ENOUGH_TO_TARGET)
				{
					strider.currentMovingLegIndex = mostMisplacedLegIndex;
					strider.legs[mostMisplacedLegIndex].oldPosition = strider.legs[mostMisplacedLegIndex].position;
					strider.legs[mostMisplacedLegIndex].targetPosition = mostMisplacedLegDesiredPosition;
					strider.legs[mostMisplacedLegIndex].movementFraction = 0;
				}
			}

			// move vertically
			// float averageLegY = 0;
			// float? minLegYCandidate = null;
			// foreach (IStriderLeg leg in strider.legs)
			// {
			// 	averageLegY += leg.position.y;
			// 	minLegYCandidate = minLegYCandidate.HasValue ? min(minLegYCandidate.Value, leg.position.y) : leg.position.y;
			// }
			// averageLegY /= max(strider.legs.Count, 1);
			// float minLegY = minLegYCandidate ?? 0;
			//
			// float desiredY = STRIDER_HEIGHT + minLegY + 0.2f * (averageLegY - minLegY);

			float desiredY = max(SpatialLogic.FindSurfaceCellMidUnder(board.worldMap, strider.position).y + 1, 2);

			float deltaY = desiredY - strider.position.y;
			if (abs(deltaY) > 0.1f)
			{
				float verticalSpeed = deltaY * STRIDER_SPEED;
				float3 verticalMovement = float3(0, 1, 0) * verticalSpeed * deltaTime;
				strider.position += verticalMovement;
			}

			return Status.RUNNING;
		}

		private const float MIN_HORIZONTAL_LEG_DISTANCE = 1f;//2.0f;
		private const float OPTIMAL_LEG_DISTANCE = 3f;//3.75f;
		
		private static float3 GetDesiredLegPosition(IGameBoard board, IStrider strider, int legIndex)
		{
			float desiredLegAngle = (legIndex + 0.5f) * radians(360.0f / strider.legs.Count);

			float tempLegDistance = 0;
			float3 desiredLegPosition = strider.position;
			for (float horizontalLegDistance = MIN_HORIZONTAL_LEG_DISTANCE; horizontalLegDistance < OPTIMAL_LEG_DISTANCE; horizontalLegDistance += 0.5f)
			{
				float3 horizontalLegPosition = strider.position + mul(Unity.Mathematics.quaternion.RotateY(desiredLegAngle), strider.forward) * horizontalLegDistance;
				float3 legPosition = SpatialLogic.FindSurfaceCellMidUnder(board.worldMap, horizontalLegPosition);
				float legDistance = distance(legPosition, strider.position);
				if (tempLegDistance == 0 || /*legDistance < OPTIMAL_LEG_DISTANCE && */abs(legDistance - OPTIMAL_LEG_DISTANCE) < abs(tempLegDistance - OPTIMAL_LEG_DISTANCE))
				{
					desiredLegPosition = legPosition;
					tempLegDistance = legDistance;
				}
			}

			// float3 desiredLegHorizontalPosition = strider.position + mul(Unity.Mathematics.quaternion.RotateY(desiredLegAngle), strider.forward) * 2.0f;
			// float3 surfaceCellMidUnder = FindSurfaceCellMidUnder(board, desiredLegHorizontalPosition);
			// float3 desiredLegPosition = surfaceCellMidUnder;
			
			return desiredLegPosition;
		}

		private static Status MoveTo(IGameBoard board, IEnemy enemy, float3 targetPosition, float deltaTime)
		{
			float3 position = enemy.position;
			float3 horizontalDirectionToTarget = targetPosition - position;
			horizontalDirectionToTarget.y = 0;
			horizontalDirectionToTarget = normalize(horizontalDirectionToTarget);

			//UnityEngine.Debug.DrawRay(position, horizontalDirectionToTarget, Color.green);

			float3 movement = horizontalDirectionToTarget * board.settings.enemySettings.speed * deltaTime;

			quaternion rotation = enemy.rotation;

			//UnityEngine.Debug.DrawRay(position, horizontalDirectionToTarget, UnityEngine.Color.green);

			MoveSphericalCharacterResult moveSphericalCharacterResult =
				CharacterMovementLogic.MoveSphericalCharacter(
					board.physics,
					position,
					rotation,
					board.settings.enemySettings.radius,
					movement,
					board.settings.enemySettings.stepHeight,
					deltaTime,
					CharacterMovementLogic.DEFAULT_SKIN_WIDTH,
					CharacterMovementLogic.DEFAULT_MOVE_ITERATIONS
				);

			enemy.position = moveSphericalCharacterResult.position;
			enemy.rotation = moveSphericalCharacterResult.rotation;

			return Status.RUNNING;
		}
	}
}
