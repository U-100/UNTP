using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public class NetworkPlayer : NetworkBehaviour, IPlayer, IPlayerCamera
	{
		[SerializeField] private Blueprint _blueprint;

		private readonly NetworkVariable<NetworkBehaviourReference> _networkPlayerCharacterNetworkBehaviourReference = new();
		private float _fov;
		private float _aspect;

		private NetworkPlayerCharacter GetNetworkPlayerCharacter() => this._networkPlayerCharacterNetworkBehaviourReference.Value.TryGet(out NetworkPlayerCharacter networkPlayerCharacter) ? networkPlayerCharacter : null;
		
		public void SetNetworkPlayerCharacter(NetworkPlayerCharacter networkPlayerCharacter) => this._networkPlayerCharacterNetworkBehaviourReference.Value = networkPlayerCharacter;

		public IPlayerCamera playerCamera => this.character == null ? null : this;
		
		public IPlayerCharacter character => GetNetworkPlayerCharacter();

		public ConstructionState constructionState { get; set; }

		public IBlueprint blueprint => this._blueprint;

		float3 IPlayerCamera.position => GetNetworkPlayerCharacter().playerCamera.transform.position;
		quaternion IPlayerCamera.rotation => GetNetworkPlayerCharacter().playerCamera.transform.rotation;
		float3 IPlayerCamera.forward => GetNetworkPlayerCharacter().playerCamera.transform.forward;
		float3 IPlayerCamera.up => GetNetworkPlayerCharacter().playerCamera.transform.up;
		float3 IPlayerCamera.right => GetNetworkPlayerCharacter().playerCamera.transform.right;
		float IPlayerCamera.fov => GetNetworkPlayerCharacter().playerCamera.State.Lens.FieldOfView;
		float IPlayerCamera.aspect => GetNetworkPlayerCharacter().playerCamera.State.Lens.Aspect;
	}
}
