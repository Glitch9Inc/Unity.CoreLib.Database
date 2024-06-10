using Glitch9.ExtendedEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Glitch9.UI;
using UnityEditor;
using UnityEngine;

namespace Glitch9.Database.Editor
{
    public abstract class AddressableDatabaseWindow<TWindow, TDatabase, TValue> : DatabaseEditorWindow<TWindow, int, AddressableObject<TValue>>
        where TWindow : EditorWindow
        where TDatabase : AddressableDatabase<TDatabase, TValue>
        where TValue : UnityEngine.Object
    {
        protected string label => _label = _label ?? (labels.ElementAtOrDefault(database.SelectedLabelIndex) ?? "Error");
        private string _label;
        protected List<string> labels => _labels = _labels ?? database.GetDisplayingLabels();
        private List<string> _labels;

        protected static Action<int> _onSelect;
        protected bool _isEditMode;
        protected int NumPages = 1;
        protected int SelectedPage = 0;

        private Lazy<CashedAddressableDatabase<TWindow, TDatabase, TValue>> _lazyDatabase = new();
        protected CashedAddressableDatabase<TWindow, TDatabase, TValue> database => _lazyDatabase.Value;

        private Vector2 _extraMenuScrollPosition;
        protected override IReadOnlyDictionary<int, AddressableObject<TValue>> Database => database.Database;

        protected override void DrawBottomToolBar()
        {
            string[] selection = Enumerable.Range(1, NumPages).Select(i => i.ToString()).ToArray();
            int newIndex = GUILayout.Toolbar(SelectedPage, selection, EditorStyles.toolbarButton);
            if (newIndex != SelectedPage)
            {
                SelectedPage = newIndex;
            }
        }

        protected override void DrawWindowToolBar()
        {
            if (labels.Any())
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                string[] displayingLabelListArray = labels.ToArray();
                int newIndex = GUILayout.Toolbar(database.SelectedLabelIndex, displayingLabelListArray, EditorStyles.toolbarButton);
                if (newIndex != database.SelectedLabelIndex)
                {
                    database.SelectedLabelIndex = newIndex;
                    SelectedPage = 0;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        protected override void OnGUITop()
        {
            EditorGUILayout.BeginHorizontal();

            if (!database.AddressableLabels.Any())
            {
                EditorGUILayout.LabelField("!!심각한 에러!! 레이블 없음!!");
            }
            else
            {
                int startingId = database.AddressableLabels.TryGetValue(label, out int id) && label != "Error" ? id : -1;
                EditorGUILayout.LabelField($"{typeof(TDatabase)} ({label} / StartingId: {startingId} / Count:{Database.Count})", EditorStyles.boldLabel, GUILayout.MaxWidth(1000f));
            }

            DrawToolbarButtons();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbarButtons()
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Resave Group"))
            {
                SaveAddressableGroup();
            }

            if (GUILayout.Button(EditorIcons.AddFile))
            {
                database.AddNewEntry();
            }

            if (GUILayout.Button(EditorIcons.CheckFile))
            {
                Save();
            }

            _isEditMode = EGUILayout.Toggle(new GUIContent(EditorIcons.Edit), _isEditMode);
        }


        protected void OnSelect(int id)
        {
            _onSelect?.Invoke(id);
            Close();
        }

        protected virtual void Save() => database.SaveReferencesToSO();

        private void DrawAddressableLabels(AddressableLabelScope type)
        {
            if (!database.AddressableLabels.Any()) return;

            EGUILayout.VerticalLayout(EGUI.box, () =>
            {
                EditorGUILayout.LabelField("Addressable Labels");
                EGUILayout.HorizontalLayout(() =>
                {
                    EGUILayout.VerticalLayout(() =>
                    {
                        foreach (string key in database.AddressableLabels.Keys)
                        {
                            database.SetLabel(key, EGUILayout.CheckBox(key, database.GetLabel(key, type)), type);
                        }
                    });
                });
            });
        }

        private void UpdateStartingIndex()
        {
            if (!database.AddressableLabels.Any()) return;

            EGUILayout.VerticalLayout(EGUI.box, () =>
            {
                EditorGUILayout.LabelField("Starting Index");
                EditorGUI.indentLevel = 1;
                EGUILayout.VerticalLayout(() =>
                {
                    bool updated = false;

                    foreach (string key in database.AddressableLabels.Keys.Where(key => database.GetLabel(key, AddressableLabelScope.Display)))
                    {
                        int index = EditorGUILayout.IntField(key, database.AddressableLabels[key]);
                        if (index != database.AddressableLabels[key])
                        {
                            database.SetAddressableLabelIndex(key, index);
                            database.SetLabelStartingIndex(key, index);
                            updated = true;
                        }
                    }

                    if (updated)
                    {
                        // StartingIndex가 작은 순서대로 정렬
                        database.ReorderLablesByStartingIndex();
                    }
                });
                EditorGUI.indentLevel = 0;
            });
        }


        protected override void DrawExtraMenu()
        {
            // start scroll view
            _extraMenuScrollPosition = EditorGUILayout.BeginScrollView(_extraMenuScrollPosition);
            EditorGUILayout.BeginVertical(EGUI.Box(10, GUIColor.None));
            EditorGUILayout.LabelField("Labels used in this database", EditorStyles.boldLabel);
            DrawAddressableLabels(AddressableLabelScope.Display);
            UpdateStartingIndex();
            EditorGUILayout.Space(10f);

            EditorGUILayout.LabelField("Import Assets", EditorStyles.boldLabel);
            string[] groups = AddressableEditorUtility.GetGroupList();
            if (groups != null || groups.Length != 0)
            {
                int index;
                if (string.IsNullOrEmpty(database.AddressableGroup)) index = 0;
                else index = Array.IndexOf(groups, database.AddressableGroup);
                database.AddressableGroup = groups[EditorGUILayout.Popup("Addressable Group", index, groups)];
            }
            DrawAddressableLabels(AddressableLabelScope.Import);
            EditorGUILayout.BeginHorizontal(EGUI.box);
            if (GUILayout.Button("Import Addressables", MiniButtonOptions))
            {
                Initialize();
                string[] selectedLabels = database.GetLabels(AddressableLabelScope.Import);
                foreach (KeyValuePair<int, AddressableObject<TValue>> item in Database) item.Value.Labels = selectedLabels;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);

            EditorGUILayout.LabelField("Label management", EditorStyles.boldLabel);
            DrawAddressableLabels(AddressableLabelScope.Management);

            EditorGUILayout.BeginHorizontal(EGUI.box);
            if (GUILayout.Button("Add Label", MiniButtonOptions))
            {
                database.AddLabel();
            }
            if (GUILayout.Button("Remove Selected Labels", MiniButtonOptions))
            {
                database.RemoveSelectedLabels();
            }
            if (GUILayout.Button("Reapply All Labels", MiniButtonOptions))
            {
                database.ReapplyAllLabels();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset Addressable Names", MiniButtonOptions))
            {
                database.ResetAddressableNames();
            }

            if (GUILayout.Button("Fix Scriptable Object GUID", MiniButtonOptions))
            {
                database.FixScriptableObjectGUID();
            }

            if (GUILayout.Button("Rename All Addressables to ID", MiniButtonOptions))
            {
                database.SetAllAddressableNamesToObjectId();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void SaveAddressableGroup()
        {
            Dictionary<int, AddressableObject<TValue>> copyDict = new(Database);
            AddressableEditorUtility.SaveAddressables(copyDict, database.AddressableGroup);
        }
    }
}