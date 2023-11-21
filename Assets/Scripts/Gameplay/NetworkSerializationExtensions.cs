using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
    public static class NetworkSerializationExtensions
    {
        public static void ReadValueSafe(this FastBufferReader reader, out int3 i3)
        {
            reader.ReadValueSafe(out int x);
            reader.ReadValueSafe(out int y);
            reader.ReadValueSafe(out int z);
            i3 = math.int3(x, y, z);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in int3 i3)
        {
            writer.WriteValueSafe(i3.x);
            writer.WriteValueSafe(i3.y);
            writer.WriteValueSafe(i3.z);
        }
        
        public static void ReadValueSafe(this FastBufferReader reader, out float3 f3)
        {
            reader.ReadValueSafe(out float x);
            reader.ReadValueSafe(out float y);
            reader.ReadValueSafe(out float z);
            f3 = math.float3(x, y, z);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in float3 f3)
        {
            writer.WriteValueSafe(f3.x);
            writer.WriteValueSafe(f3.y);
            writer.WriteValueSafe(f3.z);
        }
        
        public static void ReadValueSafe(this FastBufferReader reader, out WorldMapCell worldMapCell)
        {
            reader.ReadValueSafe(out worldMapCell.mask);
            reader.ReadValueSafe(out worldMapCell.materialId);
            reader.ReadValueSafe(out worldMapCell.resourceId);
            reader.ReadValueSafe(out worldMapCell.objectId);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in WorldMapCell worldMapCell)
        {
            writer.WriteValueSafe(worldMapCell.mask);
            writer.WriteValueSafe(worldMapCell.materialId);
            writer.WriteValueSafe(worldMapCell.resourceId);
            writer.WriteValueSafe(worldMapCell.objectId);
        }
    }
}
