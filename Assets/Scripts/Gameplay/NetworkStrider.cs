using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UNTP
{
    public class NetworkStrider : NetworkEnemy, IStrider
    {
        [SerializeField] private Transform _aimPositionTransform;
        [SerializeField] private List<StriderLeg> _legs = new();

        public float3 aimPosition
        {
            get => this._aimPositionTransform.position;
            set => this._aimPositionTransform.position = value;
        }
        
        public IReadOnlyList<IStriderLeg> legs => this._legs;
        
        public int currentMovingLegIndex { get; set; }
    }
}
