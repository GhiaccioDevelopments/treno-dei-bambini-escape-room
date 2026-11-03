using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>Punto di comparsa del player (id es. "inizio", "oggetto_scarpe").</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public string id;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.2f, 0.3f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, transform.forward);
        }
    }
}
