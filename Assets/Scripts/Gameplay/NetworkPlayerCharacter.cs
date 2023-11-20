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

		public CinemachineCamera playerCamera => this._playerCamera;
		
		public override void OnNetworkSpawn()
		{
			this._playerCamera.gameObject.SetActive(this.IsClient && this.IsOwner);
		}
	}
}
