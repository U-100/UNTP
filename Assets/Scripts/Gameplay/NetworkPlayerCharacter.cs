using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace UNTP
{
	// simple player character object whose position/rotation are synced over the network
	public class NetworkPlayerCharacter : NetworkBehaviour, IPlayerCharacter
	{
		[SerializeField] private CinemachineCamera _playerCamera;
		[SerializeField] private VisualEffect _shotEffect; 

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

		public void Shoot(float3 from, float3 direction) => ShootServerRpc(from, direction);

		[ServerRpc]
		private void ShootServerRpc(float3 from, float3 direction) => ShootClientRpc(from, direction);

		[ClientRpc]
		private void ShootClientRpc(float3 from, float3 direction)
		{
			this._shotEffect.Play();
		}
		
		public CinemachineCamera playerCamera => this._playerCamera;
		
		public override void OnNetworkSpawn()
		{
			this._playerCamera.gameObject.SetActive(this.IsClient && this.IsOwner);
		}
	}
}
