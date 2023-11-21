using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	// simple player character object whose position/rotation are synced over the network
	public class NetworkPlayerCharacter : NetworkBehaviour, IPlayerCharacter
	{
		[SerializeField] private CinemachineCamera _playerCamera;
		[SerializeField] private GameObject _shotEffect; 

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

		public void Shoot(float3 from, float3 direction)
		{
			GameObject shotEffect = Instantiate(this._shotEffect, from, quaternion.LookRotationSafe(direction, Vector3.up));
			Destroy(shotEffect, 1.0f);
		}
		
		public CinemachineCamera playerCamera => this._playerCamera;
		
		public override void OnNetworkSpawn()
		{
			this._playerCamera.gameObject.SetActive(this.IsClient && this.IsOwner);
		}
	}
}
