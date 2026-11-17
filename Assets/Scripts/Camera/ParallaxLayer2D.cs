using UnityEngine;

namespace ContraLike.CameraControl
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    [AddComponentMenu("Contra-Like/Камера/Слой параллакса")]
    public sealed class ParallaxLayer2D : MonoBehaviour
    {
        [InspectorName("Следование за камерой"), Tooltip("X — по горизонтали, Y — по вертикали. 0 оставляет слой в мире, 1 перемещает вместе с камерой. Для дальнего фона ставьте большее значение.")]
        public Vector2 cameraFollow = new Vector2(0.55f, 0.7f);

        private Transform view;
        private Vector3 layerOrigin;
        private Vector3 viewOrigin;

        private void Awake()
        {
            var main = Camera.main;
            if (main == null) return;
            view = main.transform;
            viewOrigin = view.position;
            layerOrigin = transform.position;
        }

        private void LateUpdate()
        {
            if (view == null) return;
            var offset = view.position - viewOrigin;
            transform.position = layerOrigin + new Vector3(
                offset.x * Mathf.Clamp01(cameraFollow.x),
                offset.y * Mathf.Clamp01(cameraFollow.y), 0);
        }
    }
}
