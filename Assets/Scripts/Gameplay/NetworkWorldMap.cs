using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

using static Unity.Mathematics.math;

namespace UNTP
{
    public class NetworkWorldMap : NetworkBehaviour, IWorldMap
    {
        private IWorldGen _worldGen;
        private int _chunkSize;

        public delegate VisualChunk VisualChunkFactory(Vector3 position);
        private VisualChunkFactory _visualChunkFactory;

        public delegate PhysicalChunk PhysicalChunkFactory(Vector3 position);
        private PhysicalChunkFactory _physicalChunkFactory;


        private readonly Dictionary<int3, WorldMapCell> _dynamicTiles = new();
        private readonly Dictionary<int2, VisualChunk> _visualChunks = new();
        private readonly Dictionary<int2, PhysicalChunk> _physicalChunks = new();


        public void Init(IWorldGen worldGen, int chunkSize, VisualChunkFactory visualChunkFactory, PhysicalChunkFactory physicalChunkFactory)
        {
            this._worldGen = worldGen;
            this._chunkSize = chunkSize;
            this._visualChunkFactory = visualChunkFactory;
            this._physicalChunkFactory = physicalChunkFactory;
        }


        public Corner MaskAt(int3 p) => this._dynamicTiles.TryGetValue(p, out WorldMapCell worldMapCell) ? worldMapCell.mask : this._worldGen.MaskAt(p);
        public int MaterialIdAt(int3 p) => this._dynamicTiles.TryGetValue(p, out WorldMapCell worldMapCell) ? worldMapCell.materialId : this._worldGen.MaterialIdAt(p);
        public int ResourceIdAt(int3 p) => this._dynamicTiles.TryGetValue(p, out WorldMapCell worldMapCell) ? worldMapCell.materialId : this._worldGen.ResourceIdAt(p);

        public WorldMapCell this[int3 p]
        {
            get => this._dynamicTiles.TryGetValue(p, out WorldMapCell worldMapCell)
                ? worldMapCell
                : new WorldMapCell {
                    mask = this._worldGen.MaskAt(p),
                    materialId = this._worldGen.MaterialIdAt(p),
                    resourceId = this._worldGen.ResourceIdAt(p)
                };
            set => SetWorldMapCellAtServerRpc(p, value);
        }

        public void MaterializeVisualRange(float3 min, float3 max)
        {
            int2 visualChunkMin = ToChunkPosition(min);
            int2 visualChunkMax = ToChunkPosition(max);

            for (int2 visualChunkPosition = visualChunkMin; visualChunkPosition.y <= visualChunkMax.y; visualChunkPosition.y += this._chunkSize)
            for (visualChunkPosition.x = visualChunkMin.x; visualChunkPosition.x <= visualChunkMax.x; visualChunkPosition.x += this._chunkSize)
                if (!this._visualChunks.ContainsKey(visualChunkPosition))
                    CreateVisualChunk(visualChunkPosition);
        }

        public void DematerializeVisualRange(float3 min, float3 max)
        {
            int2 visualChunkMin = ToChunkPosition(min);
            int2 visualChunkMax = ToChunkPosition(max);

            for (int2 visualChunkPosition = visualChunkMin; visualChunkPosition.y <= visualChunkMax.y; visualChunkPosition.y += this._chunkSize)
            for (visualChunkPosition.x = visualChunkMin.x; visualChunkPosition.x <= visualChunkMax.x; visualChunkPosition.x += this._chunkSize)
                if (this._visualChunks.TryGetValue(visualChunkPosition, out VisualChunk visualChunk))
                {
                    this._visualChunks.Remove(visualChunkPosition);
                    DestroyVisualChunk(visualChunk);
                }
        }

        public void MaterializePhysicalRange(float3 min, float3 max)
        {
            int2 physicalChunkMin = ToChunkPosition(min);
            int2 physicalChunkMax = ToChunkPosition(max);

            for (int2 physicalChunkPosition = physicalChunkMin; physicalChunkPosition.y <= physicalChunkMax.y; physicalChunkPosition.y += this._chunkSize)
            for (physicalChunkPosition.x = physicalChunkMin.x; physicalChunkPosition.x <= physicalChunkMax.x; physicalChunkPosition.x += this._chunkSize)
                if (!this._physicalChunks.ContainsKey(physicalChunkPosition))
                    CreatePhysicalChunk(physicalChunkPosition);
        }

        public void DematerializePhysicalRange(float3 min, float3 max)
        {
            int2 physicalChunkMin = ToChunkPosition(min);
            int2 physicalChunkMax = ToChunkPosition(max);

            for (int2 physicalChunkPosition = physicalChunkMin; physicalChunkPosition.y <= physicalChunkMax.y; physicalChunkPosition.y += this._chunkSize)
            for (physicalChunkPosition.x = physicalChunkMin.x; physicalChunkPosition.x <= physicalChunkMax.x; physicalChunkPosition.x += this._chunkSize)
                if (this._physicalChunks.TryGetValue(physicalChunkPosition, out PhysicalChunk physicalChunk))
                {
                    this._physicalChunks.Remove(physicalChunkPosition);
                    DestroyPhysicalChunk(physicalChunk);
                }
        }

        
        public override void OnNetworkSpawn() { }

        public override void OnNetworkDespawn() => ClearChunks();


        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SetWorldMapCellAtServerRpc(int3 p, WorldMapCell worldMapCell)
        {
            this._dynamicTiles[p] = worldMapCell;
            
            ActualizePhysicalRange(p, p);
            
            SetWorldCellAtClientRpc(p, worldMapCell);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void SetWorldCellAtClientRpc(int3 p, WorldMapCell worldMapCell)
        {
            this._dynamicTiles[p] = worldMapCell;

            if(!this.IsHost)
                ActualizePhysicalRange(p, p);
            
            ActualizeVisualRange(p, p);
        }

        private int2 ToChunkPosition(float3 p) => (int2)floor(p.xz / this._chunkSize) * this._chunkSize;
        
        private float3 FromChunkPosition(int2 chunkPosition) => float3(chunkPosition.x, 0, chunkPosition.y);

        private void ActualizeVisualRange(float3 min, float3 max)
        {
            int2 visualChunkMin = ToChunkPosition(min);
            int2 visualChunkMax = ToChunkPosition(max);

            for (int2 visualChunkPosition = visualChunkMin; visualChunkPosition.y <= visualChunkMax.y; visualChunkPosition.y += this._chunkSize)
            for (visualChunkPosition.x = visualChunkMin.x; visualChunkPosition.x <= visualChunkMax.x; visualChunkPosition.x += this._chunkSize)
                if (this._visualChunks.TryGetValue(visualChunkPosition, out VisualChunk visualChunk))
                {
                    DestroyVisualChunk(visualChunk);
                    CreateVisualChunk(visualChunkPosition);
                }
        }

        private void ActualizePhysicalRange(float3 min, float3 max)
        {
            int2 physicalChunkMin = ToChunkPosition(min);
            int2 physicalChunkMax = ToChunkPosition(max);

            for (int2 physicalChunkPosition = physicalChunkMin; physicalChunkPosition.y <= physicalChunkMax.y; physicalChunkPosition.y += this._chunkSize)
            for (physicalChunkPosition.x = physicalChunkMin.x; physicalChunkPosition.x <= physicalChunkMax.x; physicalChunkPosition.x += this._chunkSize)
                if (this._physicalChunks.TryGetValue(physicalChunkPosition, out PhysicalChunk physicalChunk))
                {
                    DestroyPhysicalChunk(physicalChunk);
                    CreatePhysicalChunk(physicalChunkPosition);
                }
        }

        private IVisualChunk CreateVisualChunk(int2 chunkPosition)
        {
            VisualChunk visualChunk = this._visualChunkFactory(FromChunkPosition(chunkPosition));
            visualChunk.Fill(this, this._chunkSize);
            this._visualChunks[chunkPosition] = visualChunk;
            return visualChunk;
        }

        private void DestroyVisualChunk(VisualChunk visualChunk)
        {
            Destroy(visualChunk.gameObject);
        }

        private IPhysicalChunk CreatePhysicalChunk(int2 chunkPosition)
        {
            PhysicalChunk physicalChunk = this._physicalChunkFactory(FromChunkPosition(chunkPosition));
            physicalChunk.Fill(this, this._chunkSize);
            this._physicalChunks[chunkPosition] = physicalChunk;
            return physicalChunk;
        }

        private void DestroyPhysicalChunk(PhysicalChunk physicalChunk)
        {
            Destroy(physicalChunk.gameObject);
        }

        private void ClearChunks()
        {
            // NOTE: protection from null chunks only works in ClearChunks(), everywhere else destruction of a null chunk is an error
            
            foreach (VisualChunk visualChunk in this._visualChunks.Values)
                if (visualChunk != null) // protection from errors when stopping in edit mode - some/all of the chunks might already be destroyed by this time
                    DestroyVisualChunk(visualChunk);

            this._visualChunks.Clear();

            foreach (PhysicalChunk physicalChunk in this._physicalChunks.Values)
                if (physicalChunk != null) // protection from errors when stopping in edit mode - some/all of the chunks might already be destroyed by this time
                    DestroyPhysicalChunk(physicalChunk);

            this._physicalChunks.Clear();
        }
    }
}
