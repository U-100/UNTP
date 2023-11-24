using Unity.Mathematics;

namespace UNTP
{
	public interface IGameInput
	{
		public float2 move { get; set; }
		public float2 fireAim { get; set; }
		public bool startConstructionPlacement { get; set; }
		public bool confirmConstructionPlacement { get; set; }
		public bool cancelConstructionPlacement { get; set; }
	}

	public class GameInput : IGameInput
	{
		public float2 move { get; set; }
		public float2 fireAim { get; set; }
		public bool startConstructionPlacement { get; set; }
		public bool confirmConstructionPlacement { get; set; }
		public bool cancelConstructionPlacement { get; set; }
	}
}
