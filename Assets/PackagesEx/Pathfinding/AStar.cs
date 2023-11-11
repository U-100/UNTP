using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Ugol.Pathfinding
{
	public static class AStar
	{
		private struct AStarPoint
		{
			public float distance;
			public float heuristic;
			public int3 p;
			public int3 pPrev;
		}

		private class AStarPointSortingComparer : Comparer<AStarPoint>
		{
			public override int Compare(AStarPoint x, AStarPoint y)
			{
				float xValue = x.distance + x.heuristic;
				float yValue = y.distance + y.heuristic;
				return xValue < yValue ? -1 : xValue > yValue ? 1 : 0;
			}
		}

		private static AStarPointSortingComparer ASTAR_POINT_SORTING_COMPARER = new AStarPointSortingComparer();

		public static List<int3> FindPath(IPathfindingAgent pathfindingAgent, int3 from, int3 to)
		{
			List<int3> path = new List<int3>();
			return FindPath(pathfindingAgent, from, to, path) ? path : null;
		}

		public static bool FindPath(IPathfindingAgent pathfindingAgent, int3 from, int3 to, List<int3> path)
		{
			if (!pathfindingAgent.IsPathValidFrom(from) || !pathfindingAgent.IsPathValidTo(to))
			{
				path.Clear();
				return false;
			}

			if (math.all(from == to))
			{
				path.Clear();
				path.Add(from);
				return true;
			}

			List<AStarPoint> openList = new List<AStarPoint> { new AStarPoint { heuristic = 0, p = from } };
			List<AStarPoint> closedList = new List<AStarPoint>();

			while (true)
			{
				if (openList.Count == 0) // nowhere to go, destination NOT reached
				{
					path.Clear();
					return false;
				} 

				AStarPoint current = openList[0]; // take current best path point from open list
				openList.RemoveAt(0);

				// add it to closed list or update closed list if necessary
				int closedIndex = closedList.FindIndex(x => math.all(x.p == current.p));
				if (closedIndex < 0)
				{
					closedIndex = closedList.Count;
					closedList.Add(current);

					if (math.all(current.p == to))
						break; // path found, backtrace
				}
				else if (closedIndex > 0 && current.distance + current.heuristic < closedList[closedIndex].distance + closedList[closedIndex].heuristic)
					closedList[closedIndex] = current;
				else
					closedIndex = -1;

				// add new possible paths to the open list if we have changed closed list
				if (closedIndex >= 0)
				{
					foreach (int3 p in pathfindingAgent.PossibleSteps(current.p))
					{
						if(math.all(p == current.p))
							continue; // don't go back

						int openIndex = openList.FindIndex(x => math.all(x.p == p));
						float distance = current.distance + math.length(p - current.p);
						float heuristic = math.max(math.max(math.abs(to.x - p.x), math.abs(to.y - p.y)), math.abs(to.z - p.z));
						if (openIndex < 0)
						{
							openList.Add(
								new AStarPoint
								{
									distance = distance,
									heuristic = heuristic, 
									p = p,
									pPrev = current.p,
								}
							);
							openList.Sort(ASTAR_POINT_SORTING_COMPARER);
						}
						else if (distance + heuristic < openList[openIndex].distance + openList[openIndex].heuristic)
						{
							openList[openIndex] =
								new AStarPoint
								{
									distance = distance,
									heuristic = heuristic,
									p = p,
									pPrev = current.p,
								};
							openList.Sort(ASTAR_POINT_SORTING_COMPARER);
						}
					}
				}
			}

			if (!math.all(closedList[closedList.Count - 1].p == to))
				throw new Exception("WTF!?!?!? last point in closed list must be destination");

			// path found, we need to backtrace it through the closed list
			AStarPoint closedPoint = closedList[closedList.Count - 1];
			path.Clear();
			path.Add(closedPoint.p);

			while (!math.all(closedPoint.p == from))
			{
				closedPoint = closedList.Find(x => math.all(x.p == closedPoint.pPrev));
				path.Add(closedPoint.p);
			}

			path.Reverse();
			return true;
		}
	}
}
