using ContraLike.Arenas;
using UnityEditor;
using UnityEngine;

namespace ContraLike.Editor
{
    [CustomPropertyDrawer(typeof(EnemyWave))]
    public sealed class EnemyWaveDrawer : PropertyDrawer
    {
        private static readonly string[] Fields = { "title", "delay", "spawnInterval", "enemies" };
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded) foreach (var name in Fields) height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true) + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var title = property.FindPropertyRelative("title").stringValue;
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, string.IsNullOrEmpty(title) ? "Волна" : title, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var name in Fields)
                {
                    row.y += row.height + EditorGUIUtility.standardVerticalSpacing;
                    var child = property.FindPropertyRelative(name); row.height = EditorGUI.GetPropertyHeight(child, true); EditorGUI.PropertyField(row, child, true);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
    }
}
