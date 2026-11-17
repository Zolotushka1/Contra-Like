using ContraLike.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace ContraLike.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Text))]
    [AddComponentMenu("Contra-Like/Монеты/Счётчик монет")]
    public sealed class CoinCounterView : MonoBehaviour
    {
        private Text label;
        private LevelProgress progress;

        private void Awake() => label = GetComponent<Text>();

        private void OnEnable()
        {
            progress = LevelProgress.Current;
            progress.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => progress.Changed -= Refresh;

        private void Refresh() => label.text = "МОНЕТЫ: " + progress.Coins;
    }
}
