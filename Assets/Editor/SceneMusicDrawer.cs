using System.IO;
using ContraLike.Audio;
using UnityEditor;
using UnityEngine;

namespace ContraLike.Editor
{
    [CustomPropertyDrawer(typeof(SceneMusic))]
    public sealed class SceneMusicDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * (property.isExpanded ? 4 : 1) + (property.isExpanded ? EditorGUIUtility.standardVerticalSpacing * 3 : 0);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var path = property.FindPropertyRelative("scenePath");
            var title = string.IsNullOrEmpty(path.stringValue) ? "Выберите сцену" : Path.GetFileNameWithoutExtension(path.stringValue);
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, title, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var name in new[] { "scenePath", "music", "bossMusic" })
                {
                    row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(row, property.FindPropertyRelative(name));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
    }
}
