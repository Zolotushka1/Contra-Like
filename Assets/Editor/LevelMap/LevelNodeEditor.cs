using ContraLike.Campaign;
using UnityEditor;
using UnityEngine;

namespace ContraLike.Editor
{
    [CustomEditor(typeof(LevelNode)), CanEditMultipleObjects]
    public sealed class LevelNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length != 1) return;
            var node = (LevelNode)target;
            var manager = node.Manager;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("Разместите кнопку дочерним объектом Level Manager.", MessageType.Info);
                return;
            }
            if (!string.IsNullOrEmpty(node.scenePath) && !System.Array.Exists(EditorBuildSettings.scenes,
                    scene => scene.enabled && scene.path == node.scenePath))
                EditorGUILayout.HelpBox("Выбранная сцена не включена в список сборки. Добавьте её в Build Profiles.", MessageType.Warning);
            if (node.nextLevels != null)
                foreach (var next in node.nextLevels)
                    if (next != null && (next == node || next.Manager != manager))
                    {
                        EditorGUILayout.HelpBox("Следующим должен быть другой уровень внутри этого же Level Manager.", MessageType.Warning);
                        break;
                    }
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Создать следующий уровень")) LevelMapAuthoring.AddLevel(manager, node);
        }
    }

    [CustomEditor(typeof(LevelManager))]
    public sealed class LevelManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Уровни находятся в дочерних объектах. Связи задаются в списке «Следующие уровни» каждой кнопки. Пунктир обновляется автоматически.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Добавить отдельный уровень")) LevelMapAuthoring.AddLevel((LevelManager)target);
        }
    }
}
