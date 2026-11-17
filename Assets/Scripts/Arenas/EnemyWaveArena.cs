using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ContraLike.Combat;
using ContraLike.Enemies;
using UnityEngine;
using UnityEngine.Events;

namespace ContraLike.Arenas
{
    [Serializable]
    public sealed class WaveEnemy
    {
        [InspectorName("Префаб врага")] public Health prefab;
        [Min(1), InspectorName("Количество")] public int count = 3;
    }
    [Serializable]
    public sealed class EnemyWave
    {
        [InspectorName("Название")] public string title = "Волна";
        [Min(0), InspectorName("Пауза перед волной, сек")] public float delay = 1;
        [Min(0), InspectorName("Пауза между врагами, сек")] public float spawnInterval = 0.6f;
        [InspectorName("Состав волны")] public List<WaveEnemy> enemies = new List<WaveEnemy>();
    }

    [DisallowMultipleComponent, AddComponentMenu("Contra-Like/Арены/Арена с волнами врагов")]
    public sealed class EnemyWaveArena : ArenaEncounter
    {
        [InspectorName("Волны"), Tooltip("Следующая волна начинается после гибели всех врагов предыдущей. Точки появления берутся из дочерних Arena Spawn Point.")]
        public List<EnemyWave> waves = new List<EnemyWave>();
        [Header("События")]
        [InspectorName("Волна началась")] public UnityEvent onWaveStarted = new UnityEvent();
        [InspectorName("Волна пройдена")] public UnityEvent onWaveCleared = new UnityEvent();

        private ArenaSpawnPoint[] points;
        private int groundIndex, airIndex;
        public int CurrentWave { get; private set; }
        public override string ConfigurationError
        {
            get
            {
                if (waves == null || waves.Count == 0) return "Добавьте хотя бы одну волну.";
                var available = GetComponentsInChildren<ArenaSpawnPoint>(true).Where(PointEnabled).ToArray();
                foreach (var wave in waves)
                {
                    if (wave == null || wave.enemies == null || wave.enemies.Count == 0) return "Укажите состав каждой волны.";
                    foreach (var group in wave.enemies)
                    {
                        if (group == null || group.prefab == null || group.count < 1) return "Для каждой группы выберите префаб и количество врагов.";
                        if (!available.Any(p => p.movementType == MovementOf(group.prefab))) return "Нет точки появления для одного из типов движения.";
                    }
                }
                return null;
            }
        }
        protected override void BeginBattle()
        {
            points = GetComponentsInChildren<ArenaSpawnPoint>(true).Where(PointEnabled).ToArray(); groundIndex = airIndex = 0; CurrentWave = 0;
            StartCoroutine(SpawnWaves());
        }
        private IEnumerator SpawnWaves()
        {
            foreach (var wave in waves)
            {
                if (wave.delay > 0) yield return new WaitForSeconds(wave.delay);
                if (!IsRunning) yield break;
                CurrentWave++; onWaveStarted.Invoke();
                if (!IsRunning) yield break;
                foreach (var group in wave.enemies) for (var i = 0; i < group.count; i++)
                {
                    if (!IsRunning) yield break;
                    var type = MovementOf(group.prefab); var matching = points.Where(p => PointEnabled(p) && p.movementType == type).ToArray();
                    if (matching.Length == 0) { Zone.ResetArena(); yield break; }
                    var index = type == EnemyMovementType.Flying ? airIndex++ : groundIndex++;
                    var enemy = Spawn(group.prefab, matching[index % matching.Length].transform.position);
                    if (enemy != null && wave.spawnInterval > 0) yield return new WaitForSeconds(wave.spawnInterval);
                }
                while (IsRunning && RemainingEnemies > 0) yield return null;
                if (!IsRunning) yield break;
                onWaveCleared.Invoke();
                if (!IsRunning) yield break;
            }
            CompleteBattle();
        }
        private static EnemyMovementType MovementOf(Health prefab)
        {
            var movement = prefab.GetComponent<EnemyMovement2D>(); return movement != null ? movement.movementType : EnemyMovementType.Ground;
        }
        private static bool PointEnabled(ArenaSpawnPoint point)
        {
            if (point == null || !point.enabled) return false;
            for (var parent = point.transform; parent != null; parent = parent.parent) if (!parent.gameObject.activeSelf) return false;
            return true;
        }
    }
}
