using Unity.Mathematics;
using Unity.Netcode;

namespace UNTP
{
    public abstract class NetworkEnemy : NetworkBehaviour, IEnemy, ICollider
    {
        public float3 position
        {
            get => this.transform.position;
            set => this.transform.position = value;
        }

        public quaternion rotation
        {
            get => this.transform.rotation;
            set => this.transform.rotation = value;
        }

        public float3 forward
        {
            get => this.transform.forward;
            set => this.transform.forward = value;
        }

        public float health { get; set; }
        
        public IEnemy asEnemy => this;
    }
}
