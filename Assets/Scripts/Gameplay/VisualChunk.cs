using System.Linq;
using Ugol.MarchingCubes;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

namespace UNTP
{
	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
	public struct MeshedTurfVertex
	{
		public Vector3 pos;
		public Vector3 normal;
	}

	public class VisualChunk : MonoBehaviour, IVisualChunk
	{
		[SerializeField] private Material[] _materials;
		[SerializeField] private float _bevel = 0.1f;
		[SerializeField] private GameObject[] _slopePrefabs;
		[SerializeField] private GameObject[] _resourcePrefabs;
		[SerializeField] private GameObject[] _objectPrefabs;

		private struct GenerationContext
		{
			public NativeArray<MeshedTurfVertex> vertices;
			public NativeArray<int> indices;
			public int vertexCount;
			public int indexCount;

			public void AddVertexAtNewIndex(in MeshedTurfVertex vertex)
			{
				this.indices[this.indexCount++] = this.vertexCount;
				this.vertices[this.vertexCount++] = vertex;
			}
		}

		public void Fill(IWorldMap worldMap, int chunkSize)
		{
			GameObject meshGameObject = new GameObject("mesh");
			meshGameObject.transform.SetParent(this.transform, false);

			MeshRenderer meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
			meshRenderer.SetSharedMaterials(this._materials.ToList());// = this._materials[materialId];

			Mesh mesh = new Mesh();
			MeshFilter meshFilter = meshGameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = mesh;

			const int maxVertices = 64000;
			const int maxIndices = 64000;
			GenerationContext generationContext =
				new()
				{
					vertices = new NativeArray<MeshedTurfVertex>(maxVertices, Allocator.Temp),
					indices = new NativeArray<int>(maxIndices, Allocator.Temp),
				};

			int3 min = (int3)floor(this.transform.position);
			int3 size = int3(chunkSize, 3, chunkSize);
			int3 max = min + size;

			Corner[,,] corners = new Corner[2, 2, 2];
			WorldMapCell[,,] allCorners = new WorldMapCell[size.x + 1, size.y + 1, size.z + 1];

			for (int3 q = 0; q.y <= size.y; ++q.y)
			for (q.z = 0; q.z <= size.z; ++q.z)
			for (q.x = 0; q.x <= size.x; ++q.x)
				allCorners[q.x, q.y, q.z] = worldMap[min + q - 1];

			SubMeshDescriptor[] subMeshDescriptors = new SubMeshDescriptor[this._materials.Length];
			
			for (int materialIndex = 0; materialIndex < this._materials.Length; ++materialIndex)
			{
				int materialId = materialIndex + 1;
				subMeshDescriptors[materialIndex].indexStart = generationContext.indexCount;
				
				for (int3 p = min; p.y < max.y; ++p.y)
				{
					for (p.z = min.z; p.z < max.z; ++p.z)
					{
						p.x = min.x;

						corners[0, 0, 0] = allCorners[p.x - min.x + 1 - 1, p.y - min.y + 1 - 1, p.z - min.z + 1 - 1].materialId == materialId ? Corner.All : Corner.None;
						corners[0, 1, 0] = allCorners[p.x - min.x + 1 - 1, p.y - min.y + 1    , p.z - min.z + 1 - 1].materialId == materialId ? Corner.All : Corner.None;

						corners[0, 0, 1] = allCorners[p.x - min.x + 1 - 1, p.y - min.y + 1 - 1, p.z - min.z + 1    ].materialId == materialId ? Corner.All : Corner.None;
						corners[0, 1, 1] = allCorners[p.x - min.x + 1 - 1, p.y - min.y + 1    , p.z - min.z + 1    ].materialId == materialId ? Corner.All : Corner.None;

						for (; p.x < max.x; ++p.x)
						{
							corners[1, 0, 0] = allCorners[p.x - min.x + 1    , p.y - min.y + 1 - 1, p.z - min.z + 1 - 1].materialId == materialId ? Corner.All : Corner.None;
							corners[1, 1, 0] = allCorners[p.x - min.x + 1    , p.y - min.y + 1    , p.z - min.z + 1 - 1].materialId == materialId ? Corner.All : Corner.None;

							corners[1, 0, 1] = allCorners[p.x - min.x + 1    , p.y - min.y + 1 - 1, p.z - min.z + 1    ].materialId == materialId ? Corner.All : Corner.None;
							corners[1, 1, 1] = allCorners[p.x - min.x + 1    , p.y - min.y + 1    , p.z - min.z + 1    ].materialId == materialId ? Corner.All : Corner.None;

							GenerateCellMeshByMarchingCubes(corners, (float3)(p - min), ref generationContext);

							if (this._slopePrefabs != null)
							{
								int slopePrefabIndex = (int)corners[1, 1, 1];
								if (this._slopePrefabs.Length > slopePrefabIndex)
								{
									GameObject slopePrefab = this._slopePrefabs[slopePrefabIndex];
									if (slopePrefab != null)
										Instantiate(slopePrefab, (float3)p, Quaternion.identity, this.transform);
								}
							}

							if (this._objectPrefabs != null)
							{
								int objectId = worldMap[p].objectId;
								if (this._objectPrefabs.Length > objectId)
								{
									GameObject objectPrefab = this._objectPrefabs[objectId];
									if (objectPrefab != null)
										Instantiate(objectPrefab, (float3)p, Quaternion.identity, this.transform);
								}
							}

							// shift right to left
							corners[0, 0, 0] = corners[1, 0, 0];
							corners[0, 1, 0] = corners[1, 1, 0];
							corners[0, 0, 1] = corners[1, 0, 1];
							corners[0, 1, 1] = corners[1, 1, 1];
						}
					}
				}
				
				subMeshDescriptors[materialIndex].indexCount = generationContext.indexCount - subMeshDescriptors[materialIndex].indexStart;
			}

			VertexAttributeDescriptor[] layout = { new(VertexAttribute.Position), new(VertexAttribute.Normal) };
			mesh.SetVertexBufferParams(generationContext.vertexCount, layout);
			mesh.SetVertexBufferData(generationContext.vertices, 0, 0, generationContext.vertexCount);

			mesh.SetIndexBufferParams(generationContext.indexCount, IndexFormat.UInt32);
			mesh.SetIndexBufferData(generationContext.indices, 0, 0, generationContext.indexCount);

			mesh.subMeshCount = this._materials.Length;
			for (int subMeshIndex = 0; subMeshIndex < subMeshDescriptors.Length; subMeshIndex++)
			{
				SubMeshDescriptor subMeshDescriptor = subMeshDescriptors[subMeshIndex];
				mesh.SetSubMesh(subMeshIndex, subMeshDescriptor);
			}

			Bounds meshBounds = new Bounds();
			Vector3 visualMin = -Vector3.one;
			Vector3 visualMax = new Vector3(chunkSize, 5, chunkSize);
			meshBounds.SetMinMax(visualMin, visualMax);
			mesh.bounds = meshBounds;

			for (int3 p = min - int3(0, 1, 0); p.y < max.y; ++p.y)
			{
				for (p.z = min.z; p.z < max.z; ++p.z)
				{
					for (p.x = min.x; p.x < max.x; ++p.x)
					{
						WorldMapCell worldMapCell = allCorners[p.x - min.x + 1, p.y - min.y + 1, p.z - min.z + 1];
						if (this._slopePrefabs != null)
						{
							int slopePrefabIndex = (int)worldMapCell.mask;
							if (this._slopePrefabs.Length > slopePrefabIndex)
							{
								GameObject slopePrefab = this._slopePrefabs[slopePrefabIndex];
								if (slopePrefab != null)
									Instantiate(slopePrefab, (float3)p, Quaternion.identity, this.transform);
							}
						}

						if (this._resourcePrefabs != null)
						{
							int resourceId = worldMapCell.resourceId;
							if (this._resourcePrefabs.Length > resourceId)
							{
								GameObject resourcePrefab = this._resourcePrefabs[resourceId];
								if (resourcePrefab != null)
									Instantiate(resourcePrefab, (float3)p, Quaternion.identity, this.transform);
							}
						}

						if (this._objectPrefabs != null)
						{
							int objectId = worldMapCell.objectId;
							if (this._objectPrefabs.Length > objectId)
							{
								GameObject objectPrefab = this._objectPrefabs[objectId];
								if (objectPrefab != null)
									Instantiate(objectPrefab, (float3)p, Quaternion.identity, this.transform);
							}
						}
					}
				}
			}
		}

		private void GenerateCellMeshByMarchingCubes(Corner[,,] corners, Vector3 visualPos, ref GenerationContext generationContext)
		{
			Bounds boundsLeft = new Bounds(visualPos + new Vector3(0, 0.5f, 0.5f), new Vector3(2 * this._bevel, 1 - 2 * this._bevel, 1 - 2 * this._bevel));
			int iCubeLeft = (corners[1, 1, 1] == Corner.All ? MCBit.R : 0) | (corners[0, 1, 1] == Corner.All ? MCBit.L : 0);
			GenerateMarchingCubeInBounds(iCubeLeft, boundsLeft, ref generationContext);

			Bounds boundsNear = new Bounds(visualPos + new Vector3(0.5f, 0.5f, 0), new Vector3(1 - 2 * this._bevel, 1 - 2 * this._bevel, 2 * this._bevel));
			int iCubeNear = (corners[1, 1, 1] == Corner.All ? MCBit.F : 0) | (corners[1, 1, 0] == Corner.All ? MCBit.N : 0);
			GenerateMarchingCubeInBounds(iCubeNear, boundsNear, ref generationContext);

			Bounds boundsBottom = new Bounds(visualPos + new Vector3(0.5f, 0, 0.5f), new Vector3(1 - 2 * this._bevel, 2 * this._bevel, 1 - 2 * this._bevel));
			int iCubeBottom = (corners[1, 1, 1] == Corner.All ? MCBit.T : 0) | (corners[1, 0, 1] == Corner.All ? MCBit.B : 0);
			GenerateMarchingCubeInBounds(iCubeBottom, boundsBottom, ref generationContext);

			Bounds boundsLeftBottom = new Bounds(visualPos + new Vector3(0, 0, 0.5f), new Vector3(2 * this._bevel, 2 * this._bevel, 1 - 2 * this._bevel));
			int iCubeLeftBottom =
				  (corners[0, 0, 1] == Corner.All ? MCBit.LB : 0)
				| (corners[1, 0, 1] == Corner.All ? MCBit.RB : 0)
				| (corners[0, 1, 1] == Corner.All ? MCBit.LT : 0)
				| (corners[1, 1, 1] == Corner.All ? MCBit.RT : 0)
				;
			GenerateMarchingCubeInBounds(iCubeLeftBottom, boundsLeftBottom, ref generationContext);

			Bounds boundsLeftNear = new Bounds(visualPos + new Vector3(0, 0.5f, 0), new Vector3(2 * this._bevel, 1 - 2 * this._bevel, 2 * this._bevel));
			int iCubeLeftNear =
				  (corners[0, 1, 0] == Corner.All ? MCBit.LN : 0)
				| (corners[1, 1, 0] == Corner.All ? MCBit.RN : 0)
				| (corners[0, 1, 1] == Corner.All ? MCBit.LF : 0)
				| (corners[1, 1, 1] == Corner.All ? MCBit.RF : 0)
				;
			GenerateMarchingCubeInBounds(iCubeLeftNear, boundsLeftNear, ref generationContext);

			Bounds boundsNearBottom = new Bounds(visualPos + new Vector3(0.5f, 0, 0), new Vector3(1 - 2 * this._bevel, 2 * this._bevel, 2 * this._bevel));
			int iCubeNearBottom =
				  (corners[1, 0, 0] == Corner.All ? MCBit.NB : 0)
				| (corners[1, 0, 1] == Corner.All ? MCBit.FB : 0)
				| (corners[1, 1, 0] == Corner.All ? MCBit.NT : 0)
				| (corners[1, 1, 1] == Corner.All ? MCBit.FT : 0)
				;
			GenerateMarchingCubeInBounds(iCubeNearBottom, boundsNearBottom, ref generationContext);

			Bounds boundsLeftNearBottom = new Bounds(visualPos + new Vector3(0, 0, 0), new Vector3(2 * this._bevel, 2 * this._bevel, 2 * this._bevel));
			int iCubeLeftNearBottom =
				  (corners[0, 0, 0] == Corner.All ? MCBit.LNB : 0)
				| (corners[1, 0, 0] == Corner.All ? MCBit.RNB : 0)
				| (corners[0, 0, 1] == Corner.All ? MCBit.LFB : 0)
				| (corners[1, 0, 1] == Corner.All ? MCBit.RFB : 0)
				| (corners[0, 1, 0] == Corner.All ? MCBit.LNT : 0)
				| (corners[1, 1, 0] == Corner.All ? MCBit.RNT : 0)
				| (corners[0, 1, 1] == Corner.All ? MCBit.LFT : 0)
				| (corners[1, 1, 1] == Corner.All ? MCBit.RFT : 0)
				;
			GenerateMarchingCubeInBounds(iCubeLeftNearBottom, boundsLeftNearBottom, ref generationContext);
		}

		private void GenerateMarchingCubeInBounds(int iCube, Bounds bounds, ref GenerationContext generationContext)
		{
			int[] tris = MCubes.TRIS[iCube];
			for (int i = 0; i < tris.Length; i += 3)
			{
				Vector3 p0 = bounds.min + Vector3.Scale(MCubes.VERTS[tris[i]], bounds.size);
				Vector3 p1 = bounds.min + Vector3.Scale(MCubes.VERTS[tris[i + 1]], bounds.size);
				Vector3 p2 = bounds.min + Vector3.Scale(MCubes.VERTS[tris[i + 2]], bounds.size);

				Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

				generationContext.AddVertexAtNewIndex(new MeshedTurfVertex { pos = p0, normal = normal });
				generationContext.AddVertexAtNewIndex(new MeshedTurfVertex { pos = p1, normal = normal });
				generationContext.AddVertexAtNewIndex(new MeshedTurfVertex { pos = p2, normal = normal });
			}
		}
	}
}
