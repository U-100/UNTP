using System.Collections.Generic;
using Ugol.Pathfinding;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UNTP
{
	public class SlopeWalker : IPathfindingAgent
	{
		private readonly IWorldTerrain _worldTerrain;
		private readonly int3 _center;
		private readonly int3 _size;

		public SlopeWalker(IWorldTerrain worldTerrain, int3 center, int3 size)
		{
			this._worldTerrain = worldTerrain;
			this._center = center;
			this._size = size;
		}

		public bool IsPathValidFrom(int3 from) => IsPathPossibleAt(from);

		public bool IsPathValidTo(int3 to) => IsPathPossibleAt(to);

		public IEnumerable<int3> PossibleSteps(int3 p)
		{
			if (any(abs(p - this._center) >= this._size))
				yield break;

			Corner mask = this._worldTerrain.MaskAt(p);

			int3 pLeft = p + int3(-1, 0, 0);
			Corner maskLeft = this._worldTerrain.MaskAt(pLeft);
			if (maskLeft == Corner.None)
			{
				int3 pBelowLeft = pLeft + int3(0, -1, 0);
				Corner maskBelowLeft = this._worldTerrain.MaskAt(pBelowLeft);
				yield return maskBelowLeft == Corner.All ? pLeft : pBelowLeft;
			}
			else if ((maskLeft & Corner.Right) == Corner.None)
				yield return pLeft;
			else if ((mask & Corner.Left) == Corner.Left && (maskLeft & Corner.All) == Corner.All)
			{
				int3 pLeftTop = pLeft + int3(0, 1, 0);
				Corner maskLeftTop = this._worldTerrain.MaskAt(pLeftTop);
				if(maskLeftTop == Corner.None)
					yield return pLeftTop;
			}

			int3 pRight = p + int3(1, 0, 0);
			Corner maskRight = this._worldTerrain.MaskAt(pRight);
			if (maskRight == Corner.None)
			{
				int3 pBelowRight = pRight + int3(0, -1, 0);
				Corner maskBelowRight = this._worldTerrain.MaskAt(pBelowRight);
				yield return maskBelowRight == Corner.All ? pRight : pBelowRight;
			}
			else if ((maskRight & Corner.Left) == Corner.None)
				yield return pRight;
			else if ((mask & Corner.Right) == Corner.Right && (maskRight & Corner.All) == Corner.All)
			{
				int3 pRightTop = pRight + int3(0, 1, 0);
				Corner maskRightTop = this._worldTerrain.MaskAt(pRightTop);
				if(maskRightTop == Corner.None)
					yield return pRightTop;
			}

			int3 pNear = p + int3(0, 0, -1);
			Corner maskNear = this._worldTerrain.MaskAt(pNear);
			if (maskNear == Corner.None)
			{
				int3 pBelowNear = pNear + int3(0, -1, 0);
				Corner maskBelowNear = this._worldTerrain.MaskAt(pBelowNear);
				yield return maskBelowNear == Corner.All ? pNear : pBelowNear;
			}
			else if ((maskNear & Corner.Far) == Corner.None)
				yield return pNear;
			else if ((mask & Corner.Near) == Corner.Near && (maskNear & Corner.All) == Corner.All)
			{
				int3 pNearTop = pNear + int3(0, 1, 0);
				Corner maskNearTop = this._worldTerrain.MaskAt(pNearTop);
				if(maskNearTop == Corner.None)
					yield return pNearTop;
			}

			int3 pFar = p + int3(0, 0, 1);
			Corner maskFar = this._worldTerrain.MaskAt(pFar);
			if (maskFar == Corner.None)
			{
				int3 pBelowFar = pFar + int3(0, -1, 0);
				Corner maskBelowFar = this._worldTerrain.MaskAt(pBelowFar);
				yield return maskBelowFar == Corner.All ? pFar : pBelowFar;
			}
			else if ((maskFar & Corner.Near) == Corner.None)
				yield return pFar;
			else if ((mask & Corner.Far) == Corner.Far && (maskFar & Corner.All) == Corner.All)
			{
				int3 pFarTop = pFar + int3(0, 1, 0);
				Corner maskFarTop = this._worldTerrain.MaskAt(pFarTop);
				if(maskFarTop == Corner.None)
					yield return pFarTop;
			}

			if (mask == Corner.None)
			{
				int3 pLeftNear = p + int3(-1, 0, -1);
				Corner maskLeftNear = this._worldTerrain.MaskAt(pLeftNear);
				if (maskLeftNear == Corner.None && maskLeft == Corner.None && maskNear == Corner.None)
				{
					int3 pLeftNearBottom = p + int3(-1, -1, -1);
					Corner maskLeftNearBottom = this._worldTerrain.MaskAt(pLeftNearBottom);
					if(maskLeftNearBottom == Corner.All)
						yield return pLeftNear;
				}

				int3 pLeftFar = p + int3(-1, 0, 1);
				Corner maskLeftFar = this._worldTerrain.MaskAt(pLeftFar);
				if (maskLeftFar == Corner.None && maskLeft == Corner.None && maskFar == Corner.None)
				{
					int3 pLeftFarBottom = p + int3(-1, -1, 1);
					Corner maskLeftFarBottom = this._worldTerrain.MaskAt(pLeftFarBottom);
					if(maskLeftFarBottom == Corner.All)
						yield return pLeftFar;
				}

				int3 pRightNear = p + int3(1, 0, -1);
				Corner maskRightNear = this._worldTerrain.MaskAt(pRightNear);
				if (maskRightNear == Corner.None && maskRight == Corner.None && maskNear == Corner.None)
				{
					int3 pRightNearBottom = p + int3(1, -1, -1);
					Corner maskRightNearBottom = this._worldTerrain.MaskAt(pRightNearBottom);
					if(maskRightNearBottom == Corner.All)
						yield return pRightNear;
				}

				int3 pRightFar = p + int3(1, 0, 1);
				Corner maskRightFar = this._worldTerrain.MaskAt(pRightFar);
				if (maskRightFar == Corner.None && maskRight == Corner.None && maskFar == Corner.None)
				{
					int3 pRightFarBottom = p + int3(1, -1, 1);
					Corner maskRightFarBottom = this._worldTerrain.MaskAt(pRightFarBottom);
					if(maskRightFarBottom == Corner.All)
						yield return pRightFar;
				}
			}
		}

		private bool IsPathPossibleAt(int3 p)
		{
			if (any(abs(p - this._center) >= this._size))
				return false;

			Corner mask = this._worldTerrain.MaskAt(p);

			if (mask == Corner.All)
				return false;

			if (mask == Corner.None)
				if (this._worldTerrain.MaskAt(p + int3(0, -1, 0)) != Corner.All) // empty cells must have solid ground to path through them
					return false;

			return true;
		}
	}
}
