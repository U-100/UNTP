using System;
using Unity.Mathematics;
using UnityEngine;

namespace UNTP
{
    [Serializable]
    public class EnemySettings
    {
        [SerializeField] private float _walkerHealth = 100.0f;
        public float walkerHealth => this._walkerHealth;

        [SerializeField] private float _striderHealth = 1000.0f;
        public float striderHealth => this._striderHealth;

        [SerializeField] private float _radius = 0.3f;
        public float radius => this._radius;

        [SerializeField] private float _speed = 0.5f;
        public float speed => this._speed;

        [SerializeField] private float _stepHeight = 0.1f;
        public float stepHeight => this._stepHeight;

        [SerializeField] private float _touchDistance = 0.7f;
        public float touchDistance => this._touchDistance;

        [SerializeField] private float _pathToleranceDistance = 0.5f;
        public float pathToleranceDistance => this._pathToleranceDistance;

        [SerializeField] private float _pathEndToleranceDistance = 2.0f;
        public float pathEndToleranceDistance => this._pathEndToleranceDistance;

        [SerializeField] private float _horizontalRushDistance = 1.0f;
        public float horizontalRushDistance => this._horizontalRushDistance;

		
        [Header("Spawn")]

        [SerializeField] private int _enemiesPerPlayer = 3;
        public int enemiesPerPlayer => this._enemiesPerPlayer;

        [SerializeField] private float2 _spawnDistanceRange = math.float2(3.0f, 5.0f);
        public float2 spawnDistanceRange => this._spawnDistanceRange;
    }
}
