using Unity.Mathematics;
using UnityEngine;

namespace UNTP
{
    public class StriderLeg : MonoBehaviour, IStriderLeg
    {
        public float3 position
        {
            get => this.transform.position;
            set => this.transform.position = value;
        }

        public float3 forward
        {
            get => this.transform.forward;
            set => this.transform.forward = value;
        }
        
        public float3 oldPosition { get; set; }
        public float3 targetPosition { get; set; }
        public float movementFraction { get; set; }
    }
}
