using System.IO;
using System.Linq;
using ContraLike.Campaign;
using UnityEditor;
using UnityEngine;

namespace ContraLike.Editor
{
    [CustomPropertyDrawer(typeof(SceneDropdownAttribute))]
    public sealed class SceneDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var paths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            var labels = new string[paths.Length + 1];
            labels[0] = "— Выберите сцену —";
            var selected = 0;
            for (var i = 0; i < paths.Length; i++)
            {
                var fileName = Path.GetFileNameWithoutExtension(paths[i]);
                labels[i + 1] = paths.Count(path => Path.GetFileNameWithoutExtension(path) == fileName) > 1 ? paths[i] : fileName;
                if (paths[i] == property.stringValue) selected = i + 1;
            }
            EditorGUI.BeginChangeCheck();
            var choice = EditorGUI.Popup(position, label.text, selected, labels);
            if (EditorGUI.EndChangeCheck()) property.stringValue = choice == 0 ? "" : paths[choice - 1];
            EditorGUI.EndProperty();
        }
    }
}
