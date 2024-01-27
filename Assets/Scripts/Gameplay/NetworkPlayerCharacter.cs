using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

using static Unity.Mathematics.math;

namespace UNTP
{
    public class NetworkPlayerCharacter : NetworkBehaviour, IPlayerCharacter
    {
        [SerializeField] private CinemachineCamera _playerCamera;
        [SerializeField] private VisualEffect _shotEffect;
        [SerializeField] private Transform _aimPositionTransform;
        [SerializeField] private Animator _animator;

        private readonly NetworkVariable<ForceNetworkSerializeByMemcpy<float3>> _shooting = new(float3(0), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        private Transform _cachedTransform;
        private Vector3 _savedPosition;
        private Vector3 _defaultRelativeAimPosition;

        private int? _speedAnimatorParamHash;

        private VFXEventAttribute _vfxEventAttribute;

        private readonly ExposedProperty _vfxPropertySource = "source";
        private readonly ExposedProperty _vfxPropertyTarget = "target";
        private readonly ExposedProperty _vfxPropertyHitNormal = "hitNormal";

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

        public float3 shooting
        {
            get => this._shooting.Value;
            set => this._shooting.Value = value;
        }
        
        public float timeSinceLastShot { get; set; }

        public void Shoot(float3 from, float3 target, float3 hitNormal) => ShootClientRpc(from, target, hitNormal);

        [ClientRpc]
        private void ShootClientRpc(float3 from, float3 target, float3 hitNormal)
        {
            this._aimPositionTransform.position = target;

            this._vfxEventAttribute.SetVector3(this._vfxPropertySource, from);
            this._vfxEventAttribute.SetVector3(this._vfxPropertyTarget, target);
            this._vfxEventAttribute.SetVector3(this._vfxPropertyHitNormal, hitNormal);
            this._shotEffect.Play(this._vfxEventAttribute);
        }

        public CinemachineCamera playerCamera => this._playerCamera;

        public override void OnNetworkSpawn()
        {
            this._playerCamera.gameObject.SetActive(this.IsClient && this.IsOwner);
        }

        void Start()
        {
            this._vfxEventAttribute = this._shotEffect.CreateVFXEventAttribute();

            this._cachedTransform = this.transform;
            this._savedPosition = this._cachedTransform.position;
            this._defaultRelativeAimPosition = this._aimPositionTransform.position - this._savedPosition;
        }

        void Update()
        {
            float3 positionDelta = this.transform.position - this._savedPosition;
            this._savedPosition = this._cachedTransform.position;

            if (Time.deltaTime > Mathf.Epsilon)
            {
                float speed = clamp(length(positionDelta) / Time.deltaTime, 0, 3);
                this._animator.SetFloat(this._speedAnimatorParamHash ??= Animator.StringToHash("speed"), speed);
            }

            this._aimPositionTransform.position = Vector3.MoveTowards(this._aimPositionTransform.position, this._cachedTransform.TransformPoint(this._defaultRelativeAimPosition), 10 * Time.deltaTime);
        }
    }
}
