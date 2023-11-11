using Unity.Mathematics;
using UnityEngine;

namespace UNTP
{
	public class Blueprint : MonoBehaviour, IBlueprint
	{
		void Awake()
		{
			this.active = false;
		}


		public bool active
		{
			get => this.gameObject.activeSelf;
			set => this.gameObject.SetActive(value);
		}

		public float3 position
		{
			get => this.transform.position;
			set => this.transform.position = value;
		}
	}
}
