using System;
using System.Collections.Generic;
using ContraLike.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ContraLike.Editor
{
    public static class LevelMapAuthoring
    {
        public const string NodePrefabPath = "Assets/Prefabs/UI/LevelNode.prefab";

        public static LevelNode AddLevel(LevelManager manager, LevelNode predecessor = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodePrefabPath);
            if (prefab == null) throw new InvalidOperationException("Не найден префаб кнопки уровня: " + NodePrefabPath);
            var count = manager.GetComponentsInChildren<LevelNode>(true).Length;
            var nodeObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, manager.transform);
            Undo.RegisterCreatedObjectUndo(nodeObject, "Добавить уровень");
            nodeObject.name = "Level " + (count + 1) + " Cube";
            var node = nodeObject.GetComponent<LevelNode>();
            node.title = "Уровень " + (count + 1);
            node.scenePath = "";
            node.availableFromStart = count == 0;
            node.nextLevels = new List<LevelNode>();
            node.Rect.anchoredPosition = new Vector2((count % 3 - 1) * 440, 10 - count / 3 * 290);
            if (predecessor != null)
            {
                Undo.RecordObject(predecessor, "Связать уровни");
                predecessor.nextLevels ??= new List<LevelNode>();
                predecessor.nextLevels.Add(node);
                EditorUtility.SetDirty(predecessor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(predecessor);
            }
            EditorUtility.SetDirty(node);
            PrefabUtility.RecordPrefabInstancePropertyModifications(node);
            manager.RefreshMap();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Selection.activeGameObject = nodeObject;
            return node;
        }
    }
}
