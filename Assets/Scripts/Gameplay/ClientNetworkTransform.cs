using Unity.Netcode.Components;
using UnityEngine;

namespace UNTP
{
	[DisallowMultipleComponent]
	public class ClientNetworkTransform : NetworkTransform
	{
		protected override bool OnIsServerAuthoritative() => false;
	}
}
