using Unity.Burst;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	public interface IWorldGen
	{
		Corner MaskAt(int3 p);
		int MaterialIdAt(int3 p);
		int ResourceIdAt(int3 p);
	}

	[BurstCompile]
	public struct WorldGenImpl
	{
		public uint seed;
		public float heightFrequency;
		public float obstacleThreshold;
		public float peakThreshold;
		public float slopeChance;
		public float materialsFrequency;
		public int materialsCount;
		public float resourcesFrequency;
		public float resourceChance;

		private const int MATERIAL_SEED_OFFSET = 113;
		private const int RESOURCE_SEED_OFFSET = 117;

		[BurstCompile]
		public static Corner MaskAt(in WorldGenImpl worldGenImpl, in int3 p)
		{
			Corner corner = worldGenImpl.RoughLandscape(p);

			if (p.y == 0 && corner == Corner.None && worldGenImpl.GetFloat01(p.xz) < worldGenImpl.slopeChance)
				return worldGenImpl.RampAt(p);

			return corner;
		}
		
		[BurstCompile]
		public static int MaterialIdAt(in WorldGenImpl worldGenImpl, in int3 p) => worldGenImpl.RoughLandscape(p) == Corner.All ? MaterialIdSpaceAt(worldGenImpl, p) : 0;

		private static int MaterialIdSpaceAt(in WorldGenImpl worldGenImpl, in int3 p)
		{
			float f = SampleNoise(p.xz, worldGenImpl.materialsFrequency, worldGenImpl.seed + MATERIAL_SEED_OFFSET);
			int materialId = (int)floor(f * worldGenImpl.materialsCount) + 1;
			return materialId;
		}
		
		[BurstCompile]
		public static int ResourceIdAt(in WorldGenImpl worldGenImpl, in int3 p)
		{
			if(worldGenImpl.RoughLandscape(p) == Corner.All)
			{
				float f = SampleNoise(p.xz, worldGenImpl.resourcesFrequency, worldGenImpl.seed + RESOURCE_SEED_OFFSET);
				if (f < worldGenImpl.resourceChance)
					return MaterialIdSpaceAt(worldGenImpl, p);
			}

			return 0;
		}

		private readonly float GetFloat01(in int2 a)
		{
			uint h = hash(uint3(this.seed, (uint2)a));
			return remap(uint.MinValue, uint.MaxValue, 0, 1, h);
		}

		private readonly Corner RampAt(in int3 p)
		{
			Corner cornerLeft = RoughLandscape(p + int3(-1, 0, 0));
			Corner? cornerRight = null;
			if (cornerLeft == Corner.All)
			{
				if ((cornerRight = RoughLandscape(p + int3(1, 0, 0))) == Corner.None)
				{
					Corner cornerLeftUp = RoughLandscape(p + int3(-1, 1, 0));
					if (cornerLeftUp == Corner.None)
						return Corner.Left;
				}
			}
				
			if (cornerLeft == Corner.None)
			{
				if ((cornerRight ?? RoughLandscape(p + int3(1, 0, 0))) == Corner.All)
				{
					Corner cornerRightUp = RoughLandscape(p + int3(1, 1, 0));
					if (cornerRightUp == Corner.None)
						return Corner.Right;
				}
			}

			Corner cornerNear = RoughLandscape(p + int3(0, 0, -1 ));
			Corner? cornerFar = null;
			if (cornerNear == Corner.All)
			{
				if ((cornerFar = RoughLandscape(p + int3(0, 0, 1))) == Corner.None)
				{
					Corner cornerNearUp = RoughLandscape(p + int3(0, 1, -1));
					if (cornerNearUp == Corner.None)
						return Corner.Near;
				}
			}

			if (cornerNear == Corner.None)
			{
				if ((cornerFar ?? RoughLandscape(p + int3(0, 0, 1))) == Corner.All)
				{
					Corner cornerFarUp = RoughLandscape(p + int3(0, 1, 1));
					if(cornerFarUp == Corner.None)
						return Corner.Far;
				}
			}

			return Corner.None;
		}

		private readonly Corner RoughLandscape(in int3 p) =>
			p.y switch
			{
				< 0 => Corner.All,
				0 => SampleNoise(p.xz, this.heightFrequency, this.seed) > this.obstacleThreshold ? Corner.All : Corner.None,
				1 => SampleNoise(p.xz, this.heightFrequency, this.seed) > this.peakThreshold ? Corner.All : Corner.None,
				_ => Corner.None,
			};

		private static float SampleNoise(in float2 p, float frequency, uint seed) => remap(-1, 1, 0, 1, noise.snoise(float3(p * frequency, seed)));
	}

	public class WorldGen : IWorldGen
	{
		private readonly WorldGenImpl _worldGenImpl;

		public WorldGen(WorldSettings worldSettings)
		{
			this._worldGenImpl = new WorldGenImpl
			{
				seed = worldSettings.seed,
				heightFrequency = worldSettings.heightFrequency,
				obstacleThreshold = worldSettings.obstacleThreshold,
				peakThreshold = worldSettings.peakThreshold,
				slopeChance = worldSettings.slopeChance,
				materialsFrequency = worldSettings.materialsFrequency,
				materialsCount = worldSettings.materialsCount,
				resourcesFrequency = worldSettings.resourcesFrequency,
				resourceChance = worldSettings.resourceChance,
			};
		}

		public Corner MaskAt(int3 p) => WorldGenImpl.MaskAt(this._worldGenImpl, p);
		public int MaterialIdAt(int3 p) => WorldGenImpl.MaterialIdAt(this._worldGenImpl, p);
		public int ResourceIdAt(int3 p) => WorldGenImpl.ResourceIdAt(this._worldGenImpl, p);
	}
}
