using System.Collections.Generic;
using Unity.Mathematics;

namespace Ugol.Pathfinding
{
	public interface IPathfindingAgent
	{
		bool IsPathValidFrom(int3 from);
		bool IsPathValidTo(int3 to);
		IEnumerable<int3> PossibleSteps(int3 p);
	}
}
