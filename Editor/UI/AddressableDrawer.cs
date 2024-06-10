using Glitch9.ExtendedEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Glitch9.Database.Editor
{
    [CustomPropertyDrawer(typeof(AddressableAttribute))]
    public class AddressableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AddressableAttribute attribute = (AddressableAttribute)base.attribute;
            string group = attribute.AssetName;
            string value = property.stringValue;
            GUILayout.Space(-20);
            List<string> uiSoundList = AddressableEditorUtility.GetAllAddressableNames(group); //예시: SFX_UI 
            string newValue = EGUILayout.StringListDropdown(value, uiSoundList, label);
            if (newValue != value)
            {
                property.stringValue = newValue;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
