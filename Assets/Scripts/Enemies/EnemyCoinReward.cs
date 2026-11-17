using ContraLike.Campaign;
using ContraLike.Combat;
using UnityEngine;

namespace ContraLike.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(Health))]
    [AddComponentMenu("Contra-Like/Монеты/Награда за врага")]
    public sealed class EnemyCoinReward : MonoBehaviour
    {
        [Min(0), InspectorName("Монет за победу"), Tooltip("Монеты начисляются при гибели врага. 0 отключает награду.")]
        public int amount = 1;

        private Health health;
        private bool awarded;
        private string attemptKey;

        private void Awake() => attemptKey = "enemy:" + LevelRun.ObjectKey(transform);

        private void Start()
        {
            if (LevelRun.WasConsumed(gameObject.scene.path, attemptKey)) gameObject.SetActive(false);
        }

        internal void SetSpawnKey(string key) => attemptKey = key;

        private void OnEnable()
        {
            health = GetComponent<Health>();
            health.Died += Award;
        }

        private void OnDisable() => health.Died -= Award;

        private void Award(Health defeated)
        {
            if (awarded) return;
            awarded = true;
            LevelRun.RecordReward(gameObject.scene.path, attemptKey, defeated.team == CombatTeam.Enemy ? amount : 0);
        }
    }
}
