using System.Collections.Generic;
using ContraLike.Campaign;
using ContraLike.Combat;
using ContraLike.Enemies;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ContraLike.Arenas
{
    [DisallowMultipleComponent, RequireComponent(typeof(ArenaZone))]
    public abstract class ArenaEncounter : MonoBehaviour
    {
        private readonly HashSet<Health> active = new HashSet<Health>();
        private readonly HashSet<Health> spawned = new HashSet<Health>();
        private readonly HashSet<int> ownerIds = new HashSet<int>();
        private GameObject actors;
        private int nextSpawn;
        protected ArenaZone Zone { get; private set; }
        public bool IsRunning { get; private set; }
        public int RemainingEnemies => active.Count;
        public abstract string ConfigurationError { get; }

        internal void BeginEncounter(ArenaZone zone)
        {
            EndEncounter(true);
            Zone = zone; IsRunning = true;
            nextSpawn = 0;
            actors = new GameObject(name + " — Enemies"); SceneManager.MoveGameObjectToScene(actors, gameObject.scene);
            BeginBattle();
        }
        protected abstract void BeginBattle();
        protected virtual void BattleStopped() { }
        protected virtual void RosterChanged() { }

        protected Health Spawn(Health prefab, Vector3 position)
        {
            var path = gameObject.scene.path;
            var key = Zone.AttemptKey + "/enemy/" + nextSpawn++;
            if (LevelRun.WasConsumed(path, key)) return null;

            var actor = Instantiate(prefab, position, Quaternion.identity, actors.transform);
            if (actor.TryGetComponent<EnemyCoinReward>(out var reward) && reward.isActiveAndEnabled)
                reward.SetSpawnKey(key);
            else if (LevelRun.Tracks(path))
                actor.Died += _ => LevelRun.RecordReward(path, key, 0);
            actor.name = prefab.name; spawned.Add(actor); active.Add(actor); ownerIds.Add(actor.transform.GetInstanceID()); actor.Died += OnDefeated;
            return actor;
        }
        private void OnDefeated(Health actor)
        {
            if (!IsRunning || !active.Remove(actor)) return;
            actor.Died -= OnDefeated; RosterChanged();
        }
        private void Update()
        {
            if (IsRunning && active.RemoveWhere(actor => actor == null || actor.IsDead) > 0) RosterChanged();
            if (!IsRunning && actors != null && actors.transform.childCount == 0) { Destroy(actors); actors = null; }
        }
        protected void CompleteBattle() { if (IsRunning) Zone.CompleteEncounter(); }

        protected void ClearSpawnedProjectiles()
        {
            foreach (var shot in FindObjectsByType<Projectile2D>(FindObjectsSortMode.None))
            {
                if (!ownerIds.Contains(shot.OwnerInstanceId)) continue;
                shot.gameObject.SetActive(false);
                Destroy(shot.gameObject);
            }
        }

        internal void EndEncounter(bool cleanup)
        {
            IsRunning = false; StopAllCoroutines();
            ClearSpawnedProjectiles();
            foreach (var actor in spawned) if (actor != null) actor.Died -= OnDefeated;
            active.Clear(); spawned.Clear(); ownerIds.Clear();
            if (actors != null)
            {
                if (cleanup) { actors.SetActive(false); Destroy(actors); actors = null; }
            }
            BattleStopped();
        }
        protected virtual void OnDisable()
        {
            if (IsRunning && Zone != null) Zone.ResetArena();
            EndEncounter(true);
        }
    }
}
