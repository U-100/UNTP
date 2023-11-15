﻿using System;
using UnityEngine;

namespace UNTP
{
    [Serializable]
    public class PlayerSettings
    {
        [SerializeField] private float _radius = 0.3f;
        public float radius => this._radius;

        [SerializeField] private float _speed = 1.0f;
        public float speed => this._speed;

        [SerializeField] private float _stepHeight = 0.1f;
        public float stepHeight => this._stepHeight;

        [SerializeField] private float _visionSize = 10.0f;
        public float visionSize => this._visionSize;
    }
}