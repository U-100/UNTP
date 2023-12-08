using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

using static Unity.Mathematics.math;

namespace UNTP
{
	// simple player character object whose position/rotation are synced over the network
	public class NetworkPlayerCharacter : NetworkBehaviour, IPlayerCharacter
	{
		[SerializeField] private CinemachineCamera _playerCamera;
		[SerializeField] private VisualEffect _shotEffect;

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
		
		public float timeSinceLastShot { get; set; }

		public void Shoot(float3 from, float3 target, float3 hitNormal) => ShootServerRpc(from, target, hitNormal);

		[ServerRpc]
		private void ShootServerRpc(float3 from, float3 target, float3 hitNormal) => ShootClientRpc(from, target, hitNormal);

		[ClientRpc]
		private void ShootClientRpc(float3 from, float3 target, float3 hitNormal)
		{
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
		}
	}
}
