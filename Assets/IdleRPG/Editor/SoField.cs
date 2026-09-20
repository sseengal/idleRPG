using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Read-only counterpart to <see cref="Editable"/>: pulls private [SerializeField] values out of an asset
    /// so tooling can export them to a spec file.
    ///
    /// ELI5: `Editable` is the pen, this is the pencil - it lets a tool read the locked-away values.
    /// </summary>
    public static class SoField
    {
        public static float Float(Object target, string fieldName, float fallback = 0f)
        {
            SerializedProperty property = Find(target, fieldName);
            return property == null ? fallback : property.floatValue;
        }

        public static double Double(Object target, string fieldName, double fallback = 0d)
        {
            SerializedProperty property = Find(target, fieldName);
            return property == null ? fallback : property.doubleValue;
        }

        public static int Int(Object target, string fieldName, int fallback = 0)
        {
            SerializedProperty property = Find(target, fieldName);
            return property == null ? fallback : property.intValue;
        }

        public static bool Bool(Object target, string fieldName, bool fallback = false)
        {
            SerializedProperty property = Find(target, fieldName);
            return property == null ? fallback : property.boolValue;
        }

        public static string Text(Object target, string fieldName, string fallback = "")
        {
            SerializedProperty property = Find(target, fieldName);
            return property == null || string.IsNullOrEmpty(property.stringValue) ? fallback : property.stringValue;
        }

        public static Color Color(Object target, string fieldName, Color fallback)
        {
            SerializedProperty property = Find(target, fieldName);
            return property == null ? fallback : property.colorValue;
        }

        public static Object Reference(Object target, string fieldName)
        {
            SerializedProperty property = Find(target, fieldName);
            return property == null ? null : property.objectReferenceValue;
        }

        /// <summary>Reads an array/list length (0 when the field is missing or not a list).</summary>
        public static int Count(Object target, string fieldName)
        {
            SerializedProperty property = Find(target, fieldName);
            if (property == null || !property.isArray)
            {
                return 0;
            }

            return property.arraySize;
        }

        /// <summary>Reads one element of a list field.</summary>
        public static Object ElementAt(Object target, string fieldName, int index)
        {
            SerializedProperty property = Find(target, fieldName);
            if (property == null || !property.isArray || index < 0 || index >= property.arraySize)
            {
                return null;
            }

            return property.GetArrayElementAtIndex(index).objectReferenceValue;
        }

        /// <summary>Name of a sprite asset ("enemy_slime" for "…/enemy_slime.png"); empty when unset.</summary>
        public static string AssetName(Object reference)
        {
            return reference == null ? "" : reference.name;
        }

        private static SerializedProperty Find(Object target, string fieldName)
        {
            if (target == null)
            {
                return null;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            return serializedObject.FindProperty(fieldName);
        }
    }
}
