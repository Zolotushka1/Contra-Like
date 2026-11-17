using System.Collections;
using ContraLike.Audio;
using ContraLike.Combat;
using ContraLike.Enemies;
using UnityEngine;

namespace ContraLike.Arenas
{
    [DisallowMultipleComponent, AddComponentMenu("Contra-Like/Арены/Арена босса")]
    public sealed class BossArena : ArenaEncounter
    {
        [InspectorName("Префаб босса"), Tooltip("Готовый префаб с компонентом Health. Настройки здоровья, движения и оружия находятся в самом префабе.")]
        public Health bossPrefab;
        [InspectorName("Включать музыку босса")]
        public bool playBossMusic = true;

        private bool startedMusic;
        public Health Boss { get; private set; }
        public override string ConfigurationError => bossPrefab == null ? "Выберите префаб босса." : transform.Find("Boss Spawn") == null ? "Добавьте дочернюю точку Boss Spawn." : null;
        protected override void BeginBattle()
        {
            Boss = Spawn(bossPrefab, transform.Find("Boss Spawn").position);
            if (Boss == null)
            {
                CompleteBattle();
                return;
            }
            if (playBossMusic && MusicManager.Instance != null) { MusicManager.Instance.PlayBossMusic(); startedMusic = true; }
        }
        protected override void RosterChanged()
        {
            if (RemainingEnemies != 0) return;

            ClearSpawnedProjectiles();
            var death = Boss != null ? Boss.GetComponent<EnemyDeath2D>() : null;
            if (death == null) CompleteBattle();
            else StartCoroutine(CompleteAfterAnimation(death));
        }

        private IEnumerator CompleteAfterAnimation(EnemyDeath2D death)
        {
            yield return null;
            while (death != null && !death.DeathAnimationFinished) yield return null;
            CompleteBattle();
        }
        protected override void BattleStopped()
        {
            if (startedMusic && MusicManager.Instance != null) MusicManager.Instance.PlayLevelMusic();
            startedMusic = false; Boss = null;
        }
        private void OnDrawGizmos()
        {
            var spawn = transform.Find("Boss Spawn"); if (spawn == null) return;
            Gizmos.color = new Color(1, 0.3f, 0.75f); Gizmos.DrawWireSphere(spawn.position, 0.45f); Gizmos.DrawLine(spawn.position, spawn.position + Vector3.up);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(spawn.position + Vector3.up * 1.1f, "Появление босса");
#endif
        }
    }
}
