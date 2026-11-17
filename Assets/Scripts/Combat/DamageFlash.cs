using UnityEngine;

namespace ContraLike.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(Health))]
    [AddComponentMenu("Contra-Like/Эффекты/Окрашивание при уроне")]
    public sealed class DamageFlash : MonoBehaviour
    {
        [Min(0), InspectorName("Длительность вспышки"), Tooltip("Сколько секунд спрайт остаётся окрашенным после полученного урона.")]
        public float duration = 0.18f;
        [ColorUsage(false), InspectorName("Цвет при уроне")]
        public Color damageColor = Color.red;

        private static readonly int FlashAmount = Shader.PropertyToID("_DamageFlash");
        private static readonly int FlashColor = Shader.PropertyToID("_DamageColor");
        private Health health;
        private SpriteRenderer sprite;
        private MaterialPropertyBlock properties;
        private Color originalColor;
        private bool useShader;
        private bool flashing;
        private float clearAt;

        private void OnEnable()
        {
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                originalColor = sprite.color;
                useShader = sprite.sharedMaterial != null && sprite.sharedMaterial.HasProperty(FlashAmount);
                properties ??= new MaterialPropertyBlock();
            }
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
        private void OnDisable()
        {
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; }
            Clear();
        }
        private void OnDamaged(Health _)
        {
            if (sprite == null || duration <= 0) return;
            flashing = true; clearAt = Time.time + duration;
            if (useShader)
            {
                sprite.GetPropertyBlock(properties);
                properties.SetFloat(FlashAmount, 1); properties.SetColor(FlashColor, damageColor);
                sprite.SetPropertyBlock(properties);
            }
            else sprite.color = new Color(damageColor.r, damageColor.g, damageColor.b, originalColor.a);
        }
        private void OnDied(Health _) => Clear();
        private void Update() { if (flashing && Time.time >= clearAt) Clear(); }
        private void Clear()
        {
            flashing = false;
            if (sprite == null) return;
            if (useShader)
            {
                sprite.GetPropertyBlock(properties); properties.SetFloat(FlashAmount, 0); sprite.SetPropertyBlock(properties);
            }
            else sprite.color = originalColor;
        }
    }
}
