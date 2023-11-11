using Unity.Burst;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	[BurstCompile]
	public struct SquareSpiral
	{
		[BurstCompile]
		public static void Next(in int2 center, ref int2 current)
		{
			int2 d = current - center;

			if (d.y < d.x && d.y > -d.x)
				current += int2(0, 1); // right sector - move forward
			else if(d.y >= d.x && d.y > -d.x)
				current += int2(-1, 0); // front sector - move left
			else if(d.y > d.x && d.y <= -d.x)
				current += int2(0, -1); // left sector - move backwards
			else //if(d.y <= d.x && d.y <= -d.x)
				current += int2(1, 0); // back sector - move right
		}
	}
	
	
	public static class SpatialLogic
	{
		public static int3 GetCellWithFloorNear(IWorldMap worldMap, int3 p)
		{
			int2 horizontalCenter = p.xz;
			for (int2 horizontalCandidate = horizontalCenter; ; SquareSpiral.Next(horizontalCenter, ref horizontalCandidate))
			{
				int3 candidate = int3(horizontalCandidate.x, 0, horizontalCandidate.y);
				Corner candidateCellValue = worldMap.MaskAt(candidate);
				if (candidateCellValue == Corner.None)
					return candidate;

				if (candidateCellValue == Corner.All)
				{
					// let's try the one above it
					candidate.y += 1;
					if (worldMap.MaskAt(candidate) == Corner.None)
						return candidate;
				}
			}
		}

		public static float3 FindSurfaceCellMidUnder(IWorldMap worldMap, float3 position)
		{
			int3 positionUnder = (int3)floor(position);
			positionUnder.y = 0;
			while (worldMap[positionUnder].mask == Corner.All)
				++positionUnder.y;
			float3 horizontalMid = float3(0.5f, 0, 0.5f);
			float3 verticalMid = worldMap[positionUnder].mask == Corner.None ? float3(0) : float3(0, 0.5f, 0); 
			return positionUnder + horizontalMid + verticalMid;
		}
	}
}
