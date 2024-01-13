using Unity.Mathematics;
using Ugol.UnityMathematicsExtensions;

using static Unity.Mathematics.math;

namespace UNTP
{
	public struct MoveSphereResult
	{
		public float3 position;
		public float3? stopNormal;
	}

	public struct MoveSphericalCharacterResult
	{
		public float3 position;
		public quaternion rotation;
		public float3? groundNormal;
	}

	public static class CharacterMovementLogic
	{
		public const float DEFAULT_SKIN_WIDTH = 0.001f;
		public const int DEFAULT_MOVE_ITERATIONS = 3;

		public const float DEFAULT_GRAVITY_SPEED = 5.0f;

		public const float DEFAULT_HORIZONTAL_SLIDE_ANGLE_DEGREES = 75.0f;
		public const float DEFAULT_GRAVITY_SLIDE_ANGLE_DEGREES_WHEN_MOVING = 30.0f;
		public const float DEFAULT_GRAVITY_SLIDE_ANGLE_DEGREES_WHEN_STANDING = 44.99f; // just below 45

		public const float DEFAULT_TURN_SPEED_DEGREES_PER_SEC = 720.0f;

		public const float HORIZONTAL_MOVEMENT_EPSILON = 0.001f;

		public static MoveSphericalCharacterResult MoveSphericalCharacter(
			IGamePhysics gamePhysics,
			float3 position,
			quaternion rotation,
			float radius,
			float3 movement,
			float stepHeight,
			float deltaTime,
			float skinWidth,
			int maxHorizontalIterations
		)
		{
			float3 horizontalMovement = movement;
			horizontalMovement.y = 0;
			bool hasHorizontalMovement = length(horizontalMovement) > HORIZONTAL_MOVEMENT_EPSILON;

			float3 up = float3(0, 1, 0);
			float3 down = float3(0, -1, 0);

			float3 stepUpVector = stepHeight * up;
			float3 stepDownVector = stepHeight * down;

			MoveSphereResult movementResult = MoveSphere(gamePhysics, position + stepUpVector, radius, movement, DEFAULT_HORIZONTAL_SLIDE_ANGLE_DEGREES, skinWidth, maxHorizontalIterations);

			float3 gravityMovement = DEFAULT_GRAVITY_SPEED * down * deltaTime;

			float gravitySlideAngleDegrees = hasHorizontalMovement ? DEFAULT_GRAVITY_SLIDE_ANGLE_DEGREES_WHEN_MOVING : DEFAULT_GRAVITY_SLIDE_ANGLE_DEGREES_WHEN_STANDING;
			MoveSphereResult gravityMovementResult = MoveSphere(gamePhysics, movementResult.position, radius, gravityMovement + stepDownVector, gravitySlideAngleDegrees, skinWidth, maxHorizontalIterations);

			float3 desiredUp = gravityMovementResult.stopNormal ?? mul(rotation, up);
			float3 desiredForward = hasHorizontalMovement ? horizontalMovement : mul(rotation, float3(0, 0, 1));
			desiredForward = normalize(desiredForward - project(desiredForward, desiredUp));
			quaternion desiredRotation = Unity.Mathematics.quaternion.LookRotationSafe(desiredForward, desiredUp);
			quaternion resultRotation = rotation.RotateTowards(desiredRotation, radians(DEFAULT_TURN_SPEED_DEGREES_PER_SEC * deltaTime));

			return new MoveSphericalCharacterResult
			{
				position = gravityMovementResult.position,
				groundNormal = gravityMovementResult.stopNormal,
				rotation = resultRotation
			};
		}

		private static MoveSphereResult MoveSphere(IGamePhysics gamePhysics, float3 position, float radius, float3 movement, float slideAngleDegrees, float skinWidth, int iteration)
		{
			float movementDistance = length(movement);
			if (movementDistance <= skinWidth)
				return new MoveSphereResult { position = position };

			float3 movementDirection = normalize(movement);
			if (gamePhysics.CastSphere(position, radius, movementDirection, movementDistance + skinWidth, LayerMask.DEFAULT, out CastHit castHit))
			{
				float3 advancement = clamp(castHit.distance - skinWidth, 0, castHit.distance) * movementDirection;

				if (iteration > 0 && dot(movementDirection, castHit.normal) > -cos(radians(90 - slideAngleDegrees)))
				{
					float3 leftover = movement - advancement;
					float3 continuation = leftover - project(leftover, castHit.normal);

					//Debug.DrawRay(raycastHit.point, raycastHit.normal, Color.green);

					return MoveSphere(gamePhysics, position + advancement, radius, continuation, slideAngleDegrees, skinWidth, iteration - 1);
				}

				//Debug.DrawRay(raycastHit.point, raycastHit.normal, Color.cyan);

				return new MoveSphereResult { position = position + advancement, stopNormal = castHit.normal };
			}
			
			return new MoveSphereResult { position = position + movement };
		}
	}
}
