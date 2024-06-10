using System.Collections.Generic;
using Glitch9.Database;
using Glitch9.ExtendedEditor;
using UnityEditor;
using UnityEngine;

namespace Glitch9.Database.Editor
{
    [CustomEditor(typeof(ScriptableDatabase))]
    public class ScriptableObjectDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ScriptableDatabase scriptableObject = (ScriptableDatabase)target;
            GUIStyle style = new(GUI.skin.label);

            DrawAddressableGroupSection(scriptableObject, style);
            DrawAddressableLabelsSection(scriptableObject, style);
            DrawDatabaseSection(scriptableObject, style);
        }

        private void DrawAddressableGroupSection(ScriptableDatabase scriptableObject, GUIStyle style)
        {
            if (!string.IsNullOrEmpty(scriptableObject.addressableGroup))
            {
                EGUILayout.VerticalLayout(EGUI.Box(5), () =>
                {
                    EditorGUILayout.LabelField("Addressable Group", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(scriptableObject.addressableGroup, style);
                });
            }
        }

        private void DrawAddressableLabelsSection(ScriptableDatabase scriptableObject, GUIStyle style)
        {
            if (scriptableObject.addressableLabels != null && scriptableObject.addressableLabels.Count > 0)
            {
                EGUILayout.VerticalLayout(EGUI.Box(5), () =>
                {
                    EditorGUILayout.LabelField("Addressable Labels", EditorStyles.boldLabel);
                    foreach (KeyValuePair<string, int> label in scriptableObject.addressableLabels)
                    {
                        DrawLabel(label, scriptableObject, style);
                    }
                });
            }
        }

        private void DrawLabel(KeyValuePair<string, int> label, ScriptableDatabase scriptableObject, GUIStyle style)
        {
            EGUILayout.HorizontalLayout(EGUI.box, () =>
            {
                EditorGUILayout.LabelField(label.Key, style, GUILayout.MaxWidth(120f));
                EditorGUILayout.LabelField(label.Value.ToString(), GUILayout.MaxWidth(120f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete this label?", "Yes", "No"))
                    {
                        scriptableObject.addressableLabels.Remove(label.Key);
                        EditorUtility.SetDirty(scriptableObject);
                    }
                }
            });
        }

        private void DrawDatabaseSection(ScriptableDatabase scriptableObject, GUIStyle style)
        {
            EGUILayout.VerticalLayout(EGUI.Box(5), () =>
            {
                EditorGUILayout.LabelField("Database", EditorStyles.boldLabel);

                if (GUILayout.Button("Sort by ID", GUILayout.Width(100)))
                {
                    scriptableObject.SortDatabaseById();
                    EditorUtility.SetDirty(scriptableObject);
                }

                foreach (KeyValuePair<string, string> obj in scriptableObject.database)
                {
                    DrawDatabaseObject(obj, scriptableObject, style);
                }
            });
        }

        private void DrawDatabaseObject(KeyValuePair<string, string> obj, ScriptableDatabase scriptableObject, GUIStyle style)
        {
            style.wordWrap = true;
            style.fontSize = 10;

            EGUILayout.HorizontalLayout(EGUI.box, () =>
            {
                EditorGUILayout.LabelField(obj.Key.ToString(), style, GUILayout.MaxWidth(60f));
                EditorGUILayout.LabelField(obj.Value.ToString(), style, GUILayout.MaxWidth(840f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete this key?", "Yes", "No"))
                    {
                        scriptableObject.database.Remove(obj.Key);
                        EditorUtility.SetDirty(scriptableObject);
                    }
                }
            });
        }
    }
}