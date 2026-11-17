using System.Collections.Generic;
using ContraLike.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace ContraLike.UI
{
    [ExecuteAlways, AddComponentMenu("")]
    public sealed class LevelConnectionsGraphic : MaskableGraphic
    {
        private readonly Vector3[] corners = new Vector3[4];
        private readonly HashSet<LevelNode> seenTargets = new HashSet<LevelNode>();

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var manager = GetComponentInParent<LevelManager>();
            if (manager == null) return;
            foreach (var source in manager.Levels)
            {
                if (source == null || source.nextLevels == null) continue;
                seenTargets.Clear();
                foreach (var target in source.nextLevels)
                {
                    if (target == null || target == source || target.Manager != manager || !target.isActiveAndEnabled || !seenTargets.Add(target)) continue;
                    DrawConnection(helper, manager, source.Rect, target.Rect);
                }
            }
        }

        private void DrawConnection(VertexHelper helper, LevelManager manager, RectTransform from, RectTransform to)
        {
            var start = (Vector2)rectTransform.InverseTransformPoint(from.TransformPoint(from.rect.center));
            var end = (Vector2)rectTransform.InverseTransformPoint(to.TransformPoint(to.rect.center));
            var direction = end - start;
            var fullLength = direction.magnitude;
            if (fullLength < 0.01f) return;
            direction /= fullLength;
            var startMargin = EdgeDistance(from, direction) + 12;
            var endMargin = EdgeDistance(to, direction) + 12;
            if (fullLength <= startMargin + endMargin) return;
            start += direction * startMargin;
            var length = fullLength - startMargin - endMargin;
            var normal = new Vector2(-direction.y, direction.x) * Mathf.Max(1, manager.lineThickness) * 0.5f;
            var dash = Mathf.Max(2, manager.dashLength);
            var gap = Mathf.Max(1, manager.gapLength);
            for (float offset = 0; offset < length && helper.currentVertCount < 64000; offset += dash + gap)
            {
                var a = start + direction * offset;
                var b = start + direction * Mathf.Min(length, offset + dash);
                var first = helper.currentVertCount;
                helper.AddVert(a - normal, manager.connectionColor, Vector2.zero);
                helper.AddVert(a + normal, manager.connectionColor, Vector2.zero);
                helper.AddVert(b + normal, manager.connectionColor, Vector2.zero);
                helper.AddVert(b - normal, manager.connectionColor, Vector2.zero);
                helper.AddTriangle(first, first + 1, first + 2);
                helper.AddTriangle(first, first + 2, first + 3);
            }
        }

        private float EdgeDistance(RectTransform node, Vector2 direction)
        {
            node.GetWorldCorners(corners);
            var min = (Vector2)rectTransform.InverseTransformPoint(corners[0]);
            var max = min;
            for (var i = 1; i < corners.Length; i++)
            {
                var point = (Vector2)rectTransform.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            var half = (max - min) * 0.5f;
            var horizontal = Mathf.Abs(direction.x) < 0.0001f ? float.PositiveInfinity : half.x / Mathf.Abs(direction.x);
            var vertical = Mathf.Abs(direction.y) < 0.0001f ? float.PositiveInfinity : half.y / Mathf.Abs(direction.y);
            return Mathf.Min(horizontal, vertical);
        }
    }
}
