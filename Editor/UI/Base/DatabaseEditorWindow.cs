using Glitch9.ExtendedEditor;
using Glitch9.UI;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Glitch9.Database.Editor
{
    /// <summary>
    /// Base class for database editor windows
    /// </summary>
    /// <typeparam name="TWindowClass"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public abstract class DatabaseEditorWindow<TWindowClass, TKey, TValue> : ScrollEditorWindow<TWindowClass>
        where TWindowClass : EditorWindow
        where TKey : struct
        where TValue : class
    {
        protected abstract IReadOnlyDictionary<TKey, TValue> Database { get; }
        protected bool IsShowingDataMenu;
        protected bool IsShowingExtraMenu;

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadDatabase();
        }

        protected override void OnGUITop()
        {
            if (WindowName == null)
            {
                EditorGUILayout.HelpBox("Window name is null", global::UnityEditor.MessageType.Error);
                return;
            }

            if (Database == null)
            {
                EditorGUILayout.HelpBox("Database is null", global::UnityEditor.MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField($"{WindowName} (Count: {Database.Count})", EditorStyles.boldLabel, GUILayout.MaxWidth(1000f));
        }

        protected abstract void LoadDatabase();
        protected abstract void SaveDatabaseEntry(TValue value);

        protected virtual void DataManagement() { }

        private void ShowDataManagement()
        {
            EGUILayout.VerticalLayout(EGUI.Box(10, GUIColor.None), () =>
            {
                InternalMenuHeader("Data Management", ref IsShowingDataMenu);
                EGUILayout.HorizontalLayout(DataManagement);
            });
        }

        protected virtual void DrawBottomToolBar() { }

        protected override void OnGUIBottom()
        {
            EGUILayout.VerticalLayout(() =>
            {
                DrawBottomToolBar();

                if (IsShowingDataMenu)
                {
                    ShowDataManagement();
                }
                else if (IsShowingExtraMenu)
                {
                    ShowExtraMenu();
                }

                EGUILayout.HorizontalLayout(() =>
                {
                    if (GUILayout.Button("Data Management", ButtonOptions))
                    {
                        ToggleVisibility(ref IsShowingDataMenu);
                    }

                    if (GUILayout.Button("Extra", ButtonOptions))
                    {
                        ToggleVisibility(ref IsShowingExtraMenu);
                    }
                });
            });
        }

        private void ToggleVisibility(ref bool visibility)
        {
            IsShowingDataMenu = false;
            IsShowingExtraMenu = false;
            visibility = !visibility;
        }

        protected virtual void DrawExtraMenu() { }

        private void ShowExtraMenu()
        {
            EGUILayout.VerticalLayout(EGUI.Box(10, GUIColor.None), () =>
            {
                InternalMenuHeader("Extra Menu", ref IsShowingExtraMenu);
                DrawExtraMenu();
            });
        }
    }
}