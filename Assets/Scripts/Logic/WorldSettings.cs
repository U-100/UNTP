using System;
using UnityEngine;

namespace UNTP
{
    [Serializable]
    public class WorldSettings
    {
        [SerializeField] private uint _seed;
        public uint seed => this._seed;

        [SerializeField] private float _heightFrequency;
        public float heightFrequency => this._heightFrequency;

        [SerializeField][Range(0, 1)] private float _obstacleThreshold;
        public float obstacleThreshold => this._obstacleThreshold;

        [SerializeField][Range(0, 1)] private float _peakThreshold;
        public float peakThreshold => this._peakThreshold;

        [SerializeField][Range(0, 1)] private float _slopeChance;
        public float slopeChance => this._slopeChance;

        [SerializeField] private float _materialsFrequency;
        public float materialsFrequency => this._materialsFrequency;
        
        [SerializeField] private int _materialsCount;
        public int materialsCount => this._materialsCount;

        [SerializeField] private float _resourcesFrequency;
        public float resourcesFrequency => this._resourcesFrequency;
        
        [SerializeField][Range(0, 1)] private float _resourceChance;
        public float resourceChance => this._resourceChance;
    }
}
