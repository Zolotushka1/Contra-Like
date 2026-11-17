using ContraLike.Combat;
using UnityEngine;

namespace ContraLike.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(Health), typeof(PlayerController2D))]
    [AddComponentMenu("Contra-Like/Герой/Возвращение после смерти")]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        private Health health;
        private PlayerController2D movement;

        private void OnEnable()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerController2D>();
            health.Died += OnDied;
            movement.Respawned += health.RestoreHealth;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            if (movement != null) movement.Respawned -= health.RestoreHealth;
        }

        private void OnDied(Health _) => movement.RestartOrRespawn();
    }
}
