using ContraLike.Combat;
using UnityEditor;

namespace ContraLike.Editor
{
    [InitializeOnLoad]
    internal sealed class WeaponAssetIds : AssetPostprocessor
    {
        static WeaponAssetIds() => EditorApplication.delayCall += RefreshExistingWeapons;

        private static void RefreshExistingWeapons()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:WeaponSettings"))
                UpdateId(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported) UpdateId(path);
            foreach (var path in moved) UpdateId(path);
        }

        private static void UpdateId(string path)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(WeaponSettings)) return;
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponSettings>(path);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (weapon == null || string.IsNullOrEmpty(guid) || weapon.UnlockId == guid) return;
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("unlockId").stringValue = guid;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorApplication.delayCall += () =>
            {
                if (weapon != null) AssetDatabase.SaveAssetIfDirty(weapon);
            };
        }
    }
}
