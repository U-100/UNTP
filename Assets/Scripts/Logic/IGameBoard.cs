using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UNTP
{
	public interface IGameBoard
	{
		IGame game { get; }
		IGameSettings settings { get; }
		IGameInput input { get; }
		IGamePhysics physics { get; }
		IWorldMap worldMap { get; }
		IPlayersRepository players { get; }
		IEnemyRepository enemies { get; }
	}

	public interface IGame
	{
		bool isInLobby { get; }
		bool isServer { get; }
		bool isClient { get; }
		bool isPaused { get; }

		void StartPlaying();
		void Pause();
		void Resume();
	}

	public interface IWorldTerrain
	{
		Corner MaskAt(int3 p);
		int MaterialIdAt(int3 p);
	}

	public struct WorldMapCell
	{
		public Corner mask;
		public int materialId;
		public int resourceId;
		public int objectId;
	}
	
	public interface IWorldMap : IWorldTerrain
	{
		WorldMapCell this[int3 p] { get; set; }
		
		void MaterializeVisualRange(float3 min, float3 max);
		void DematerializeVisualRange(float3 min, float3 max);

		void MaterializePhysicalRange(float3 min, float3 max);
		void DematerializePhysicalRange(float3 min, float3 max);
	}
	
	[Flags]
	public enum Corner
	{
		None = 0x00,

		LeftNear = 0x01,
		LeftFar = 0x02,
		RightFar = 0x04,
		RightNear = 0x08,

		Left = LeftNear | LeftFar,
		Right = RightNear | RightFar,
		Near = LeftNear | RightNear,
		Far = LeftFar | RightFar,

		All = LeftNear | LeftFar | RightNear | RightFar,
	}

	public interface IVisualChunk { }

	public interface IPhysicalChunk { }

	public interface IPlayersRepository
	{
		int count { get; }
		IPlayer this[int playerIndex] { get; }
		IPlayer localPlayer { get; }
		void CreateCharacterForPlayerAtIndex(int playerIndex, float3 position, quaternion rotation);
	}

	public interface IPlayer
	{
		IPlayerCamera playerCamera { get; }
		IPlayerCharacter character { get; }
		ConstructionState constructionState { get; set; }
		IBlueprint blueprint { get; }
	}

	public interface IPlayerCamera
	{
		float3 position { get; }
		quaternion rotation { get; }
		float3 forward { get; }
		float3 up { get; }
		float3 right { get; }
		
		float fov { get; }
		float aspect { get; }
	}
	
	public interface IPlayerCharacter
	{
		float3 position { get; set; }
		quaternion rotation { get; set; }
		float3 forward { get; set; }

		float3 shooting { get; set; }
		
		float timeSinceLastShot { get; set; }
		
		void Shoot(float3 from, float3 target, float3 hitNormal);
	}

	public enum ConstructionState
	{
		NoConstruction,
		ConstructionRestricted,
		ConstructionAllowed,
	}

	public interface IBlueprint
	{
		bool active { get; set; }
		float3 position { get; set; }
	}

	public interface IEnemyRepository
	{
		int count { get; }
		IEnemy this[int index] { get; }

		IWalker CreateWalker(float3 position);
		IStrider CreateStrider(float3 position);
		
		void DestroyEnemyAtIndex(int enemyIndex);
	}

	public interface IEnemy
	{
		float3 position { get; set; }
		quaternion rotation { get; set; }
		float3 forward { get; set; }
	}

	public interface IWalker : IEnemy
	{
		List<int3> path { get; set; }
		float? selfDestructionCountdown { get; set; }

		void InitiateSelfDestruction();
	}

	public interface IStrider : IEnemy
	{
		float3 aimPosition { get; set; }
		IReadOnlyList<IStriderLeg> legs { get; }
		int currentMovingLegIndex { get; set; }
	}

	public interface IStriderLeg
	{
		float3 position { get; set; }
		float3 forward { get; set; }
		
		float3 oldPosition { get; set; }
		float3 targetPosition { get; set; }
		float movementFraction { get; set; }
	}
}
