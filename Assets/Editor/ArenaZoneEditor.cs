using ContraLike.Arenas;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ContraLike.Editor
{
    [CustomEditor(typeof(ArenaZone))]
    public sealed class ArenaZoneEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle bounds = new BoxBoundsHandle();
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var zone = (ArenaZone)target; var encounter = zone.GetComponent<ArenaEncounter>();
            EditorGUILayout.HelpBox("Позиция объекта — центр камеры. Жёлтая рамка — зона боя, голубая — кадр камеры, зелёный Box Collider — вход. Размер зоны можно менять ручками в Scene.", MessageType.Info);
            if (encounter == null) EditorGUILayout.HelpBox("Добавьте Boss Arena или Enemy Wave Arena.", MessageType.Warning);
            else if (!string.IsNullOrEmpty(encounter.ConfigurationError)) EditorGUILayout.HelpBox(encounter.ConfigurationError, MessageType.Warning);
        }
        private void OnSceneGUI()
        {
            var zone = (ArenaZone)target;
            using (new Handles.DrawingScope(new Color(1, 0.72f, 0.28f), zone.transform.localToWorldMatrix))
            {
                bounds.center = Vector3.zero; bounds.size = new Vector3(zone.arenaSize.x, zone.arenaSize.y, 0);
                bounds.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
                EditorGUI.BeginChangeCheck(); bounds.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Изменить размер арены"); zone.arenaSize = Vector2.Max(Vector2.one, bounds.size);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(zone); EditorUtility.SetDirty(zone);
                }
            }
        }
    }
}
