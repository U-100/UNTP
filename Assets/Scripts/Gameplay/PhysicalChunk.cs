using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace UNTP
{
	public class PhysicalChunk : MonoBehaviour, IPhysicalChunk
	{
		[SerializeField] private GameObject[] _collisionPrefabs;

		public void Fill(IWorldMap worldMap, int chunkSize)
		{
			if (this._collisionPrefabs != null)
			{
				int3 min = (int3)floor(this.transform.position);
				int3 max = min + int3(chunkSize, 2, chunkSize);

				for (int3 p = min; p.y < max.y; ++p.y)
				for (p.z = min.z; p.z < max.z; ++p.z)
				for (p.x = min.x; p.x < max.x; ++p.x)
				{
					Corner corner = worldMap.MaskAt(p);

					int collisionPrefabIndex = (int)corner;
					if (this._collisionPrefabs.Length > collisionPrefabIndex)
					{
						GameObject collisionPrefab = this._collisionPrefabs[collisionPrefabIndex];
						if (collisionPrefab != null)
							Instantiate(collisionPrefab, (float3)p, Quaternion.identity, this.transform);
					}
				}
			}
		}
	}
}
