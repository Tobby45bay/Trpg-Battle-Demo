using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Game.Systems.Trigger;

[CustomPropertyDrawer(typeof(ActionData), true)]
public class ActionDataDrawer : PropertyDrawer
{
    private static Dictionary<string, Type> _cachedTypes;

    private static Dictionary<string, Type> CachedTypes
    {
        get
        {
            if (_cachedTypes == null)
            {
                _cachedTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsSubclassOf(typeof(ActionData)) && !t.IsAbstract)
                    .ToDictionary(t => t.Name, t => t);
            }
            return _cachedTypes;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight;
        var iterator = property.Copy();
        var end = iterator.GetEndProperty();
        iterator.NextVisible(true);
        while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, end))
        {
            height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
        }
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var typeNames = CachedTypes.Keys.ToList();
        string currentType = property.managedReferenceFullTypename?.Split(' ').Last();
        int currentIndex = currentType != null ? typeNames.IndexOf(currentType) : -1;

        Rect dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        int newIndex = EditorGUI.Popup(dropdownRect, "Action Type", currentIndex, typeNames.ToArray());

        if (newIndex != currentIndex)
        {
            Type newType = CachedTypes[typeNames[newIndex]];
            property.managedReferenceValue = Activator.CreateInstance(newType);
        }

        if (property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            var propCopy = property.Copy();
            var end = propCopy.GetEndProperty();
            propCopy.NextVisible(true);

            float y = dropdownRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            while (propCopy.NextVisible(false) && !SerializedProperty.EqualContents(propCopy, end))
            {
                Rect fieldRect = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(propCopy, true));
                EditorGUI.PropertyField(fieldRect, propCopy, true);
                y += fieldRect.height + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
