using ContraLike.Enemies;
using UnityEngine;

namespace ContraLike.Arenas
{
    [DisallowMultipleComponent, AddComponentMenu("Contra-Like/Арены/Точка появления врагов")]
    public sealed class ArenaSpawnPoint : MonoBehaviour
    {
        [InspectorName("Каких врагов выпускать")]
        public EnemyMovementType movementType;

        private void OnDrawGizmos()
        {
            Gizmos.color = movementType == EnemyMovementType.Flying ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            var zone = GetComponentInParent<ArenaZone>();
            if (zone != null)
            {
                var direction = Mathf.Sign(zone.transform.position.x - transform.position.x);
                var end = transform.position + Vector3.right * direction * 0.8f;
                Gizmos.DrawLine(transform.position, end);
                Gizmos.DrawLine(end, end + new Vector3(-direction * 0.2f, 0.15f)); Gizmos.DrawLine(end, end + new Vector3(-direction * 0.2f, -0.15f));
            }
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.25f, name);
#endif
        }
    }
}
