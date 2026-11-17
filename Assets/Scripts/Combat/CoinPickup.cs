using ContraLike.Campaign;
using ContraLike.Player;
using UnityEngine;
using UnityEngine.Events;

namespace ContraLike.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(CircleCollider2D))]
    [AddComponentMenu("Contra-Like/Монеты/Монета на уровне")]
    public sealed class CoinPickup : MonoBehaviour
    {
        [Min(1), InspectorName("Количество монет"), Tooltip("На сколько увеличится баланс при подборе.")]
        public int amount = 1;
        [InspectorName("Звук подбора")]
        public AudioClip pickupSound;
        [InspectorName("При подборе")]
        public UnityEvent onCollected = new UnityEvent();

        private bool collected;
        private string attemptKey;

        private void Reset() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
            attemptKey = "coin:" + LevelRun.ObjectKey(transform);
        }

        private void Start()
        {
            if (LevelRun.WasConsumed(gameObject.scene.path, attemptKey)) gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActiveAndEnabled || collected) return;
            var player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;

            collected = true;
            if (LevelRun.RecordReward(gameObject.scene.path, attemptKey, Mathf.Max(1, amount)))
            {
                if (pickupSound != null) player.GetComponent<AudioSource>().PlayOneShot(pickupSound);
                onCollected.Invoke();
            }
            Destroy(gameObject);
        }
    }
}
