using UnityEngine;

namespace UNTP
{
    public class DestroyAfterDelay : MonoBehaviour
    {
        [SerializeField] private float destructionDelay = 1f;

        private void Awake() => Destroy(this.gameObject, this.destructionDelay);
    }
}
