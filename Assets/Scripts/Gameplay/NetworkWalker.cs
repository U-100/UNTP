using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
    public class NetworkWalker : NetworkEnemy, IWalker
    {
        [SerializeField] private GameObject _explosionEffectPrefab;

        private Transform _explosionEffectTransform;
        
        public List<int3> path { get; set; } = new();
        public float? selfDestructionCountdown { get; set; }

        public void InitiateSelfDestruction() => InitiateSelfDestructionClientRpc();
        
        [Rpc(SendTo.ClientsAndHost)]
        private void InitiateSelfDestructionClientRpc()
        {
            if (this._explosionEffectTransform == null)
                this._explosionEffectTransform = Instantiate(this._explosionEffectPrefab).transform;
        }
        
        void Update()
        {
            if (this._explosionEffectTransform != null)
                this._explosionEffectTransform.position = this.position;
        }
    }
}
