using ContraLike.Enemies;
using UnityEditor;

namespace ContraLike.Editor
{
    [CustomEditor(typeof(EnemyAttack2D)), CanEditMultipleObjects]
    public sealed class EnemyAttackEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var mode = serializedObject.FindProperty("attackType");
            if (mode.hasMultipleDifferentValues) DrawPropertiesExcluding(serializedObject);
            else if (mode.enumValueIndex == (int)EnemyAttackType.Contact)
                DrawPropertiesExcluding(serializedObject, "attackRange", "weapon");
            else DrawPropertiesExcluding(serializedObject, "contactDamage", "contactInterval");
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(EnemyMovement2D)), CanEditMultipleObjects]
    public sealed class EnemyMovementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var movement = (EnemyMovement2D)target;
            if (serializedObject.FindProperty("movementType").hasMultipleDifferentValues)
                DrawPropertiesExcluding(serializedObject);
            else if (movement.movementType == EnemyMovementType.Ground)
                DrawPropertiesExcluding(serializedObject, "flightHeight");
            else
            {
                var attack = movement.GetComponent<EnemyAttack2D>();
                if (attack != null && attack.attackType == EnemyAttackType.Contact)
                    DrawPropertiesExcluding(serializedObject, "patrolWhenIdle", "flightHeight");
                else DrawPropertiesExcluding(serializedObject, "patrolWhenIdle");
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
