using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Fluent <see cref="SerializedObject"/> wrapper so private [SerializeField] fields can be authored by
    /// tooling without widening their access.
    ///
    /// ELI5: the game's data assets keep their values locked away from code (private fields). This is the
    /// little key that lets an editor tool fill in those values anyway, one line at a time.
    ///
    /// Shared by <c>DataAssetGenerator</c> and the content pipeline (`ContentGenerator`).
    /// </summary>
    public sealed class Editable
    {
        private readonly SerializedObject serializedObject;

        public Editable(Object target)
        {
            serializedObject = new SerializedObject(target);
        }

        public Editable Set(string fieldName, float value)
        {
            SerializedProperty property = Find(fieldName);
            if (property != null)
            {
                property.floatValue = value;
            }

            return this;
        }

        /// <summary>Sets an enum field by name ("Tank", "BacklineFirst", ...); ignores unknown names.</summary>
        public Editable SetEnum(string fieldName, System.Enum value)
        {
            SerializedProperty property = Find(fieldName);

            if (property != null && value != null)
            {
                // enumNames are the raw C# names; enumDisplayNames are prettified ("Backline First").
                int index = System.Array.IndexOf(property.enumNames, value.ToString());

                if (index < 0)
                {
                    index = System.Array.IndexOf(property.enumDisplayNames, value.ToString());
                }

                if (index < 0)
                {
                    Debug.LogWarning($"[Editable] '{value}' is not a value of {fieldName} " +
                                     $"({string.Join(", ", property.enumNames)}).");
                }
                else
                {
                    property.enumValueIndex = index;
                }
            }

            return this;
        }

        public Editable Set(string fieldName, int value)
        {
            SerializedProperty property = Find(fieldName);
            if (property != null)
            {
                property.intValue = value;
            }

            return this;
        }

        public Editable Set(string fieldName, double value)
        {
            SerializedProperty property = Find(fieldName);
            if (property != null)
            {
                property.doubleValue = value;
            }

            return this;
        }

        public Editable Set(string fieldName, bool value)
        {
            SerializedProperty property = Find(fieldName);
            if (property != null)
            {
                property.boolValue = value;
            }

            return this;
        }

        /// <summary>
        /// Replaces a serialized array of ints (formation slot unlock stages). The array is resized first, so the
        /// generator can grow or shrink it without leaving stale entries behind.
        /// </summary>
        public Editable SetIntArray(string fieldName, int[] values)
        {
            SerializedProperty property = Find(fieldName);

            if (property == null || !property.isArray)
            {
                return this;
            }

            property.arraySize = values != null ? values.Length : 0;

            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).intValue = values[i];
            }

            return this;
        }

        public Editable Set(string fieldName, string value)
        {
            SerializedProperty property = Find(fieldName);
            if (property != null)
            {
                property.stringValue = value;
            }

            return this;
        }

        public Editable SetColor(string fieldName, Color value)
        {
            SerializedProperty property = Find(fieldName);
            if (property != null)
            {
                property.colorValue = value;
            }

            return this;
        }

        /// <summary>Assigns a generated placeholder sprite by art file name (optional).</summary>
        public Editable SetSprite(string fieldName, string spriteName)
        {
            SerializedProperty property = Find(fieldName);
            if (property == null)
            {
                return this;
            }

            Sprite sprite = null;

            if (!string.IsNullOrEmpty(spriteName))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpriteGenerator.ArtFolder + "/" + spriteName + ".png");
            }

            if (sprite == null && !string.IsNullOrEmpty(spriteName))
            {
                Debug.LogWarning($"[Editable] No art for '{spriteName}'; run Tools > Idle RPG > Generate Placeholder Sprites.");
            }

            property.objectReferenceValue = sprite;
            return this;
        }

        /// <summary>Replaces a List&lt;T&gt; of asset references.</summary>
        public Editable SetObjectList<T>(string fieldName, List<T> values) where T : Object
        {
            SerializedProperty property = Find(fieldName);
            if (property == null)
            {
                return this;
            }

            property.ClearArray();
            int count = values == null ? 0 : values.Count;
            property.arraySize = count;

            for (int i = 0; i < count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            return this;
        }

        /// <summary>Writes the pending changes and marks the asset dirty.</summary>
        public void Apply()
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }

        private SerializedProperty Find(string fieldName)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[Editable] Field '{fieldName}' not found on " +
                               $"{serializedObject.targetObject.GetType().Name} ({serializedObject.targetObject.name}).");
            }

            return property;
        }
    }
}
