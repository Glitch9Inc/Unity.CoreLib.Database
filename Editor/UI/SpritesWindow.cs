using Glitch9.ExtendedEditor;
using Glitch9.UI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Glitch9.Database.Editor
{
    public class SpritesWindow : AddressableDatabaseWindow<SpritesWindow, Sprites, Sprite>
    {
        [MenuItem("Glitch9/Image Database", priority = 21)]
        public static void ShowWindow() => Initialize();
        public static void ShowSelector(Action<int> onSelect)
        {
            _onSelect = onSelect;
            ShowWindow();
        }

        private const float MIN_ICON_SIZE = 32f;
        private const float ICON_MARGINS = 360f;
        private const int ICONS_PER_ROW = 10;
        private const int ICONS_PER_PAGE = 50;
        private const string DEFAULT_SPRITE_PATH = "Assets/_project/Images/Icons/32_system/32_none.png";
        private Sprite _cachedDefaultSprite;

        private float _iconSize = 40f;
        private bool _isAssetSelector = false;
        private bool _saveTrigger = false;
        private GUIStyle _wrappedLabelStyle;
        private Dictionary<int, Dictionary<int, AddressableObject<Sprite>>> tempDatabase;
        private Dictionary<string /* itemId */, int /* iconId */> _itemIconIds;


        protected override void OnInitialize()
        {
            // Do nothing
        }

        private Dictionary<int, Dictionary<int, AddressableObject<Sprite>>> GetSortedTempDatabase()
        {
            Dictionary<int, Dictionary<int, AddressableObject<Sprite>>> temp = new();

            /* reorder database by key */
            List<int> keys = new(Database.Keys);
            keys.Sort();

            int pages = 0;
            int itemCounts = 0;

            string currentLabel = database.GetSelectedLabel();

            foreach (int key in keys)
            {
                if (!Database.ContainsKey(key)) continue;
                if (Database[key] != null && Database[key].ContainsLabel(currentLabel))
                {
                    if (itemCounts >= ICONS_PER_PAGE)
                    {
                        itemCounts = 0;
                        pages++;
                    }

                    if (!temp.ContainsKey(pages)) temp.Add(pages, new Dictionary<int, AddressableObject<Sprite>>());
                    temp[pages].Add(key, Database[key]);
                    itemCounts++;
                }
            }

            NumPages = pages + 1;

            return temp;
        }

        protected async void Reload()
        {
            //TODO: DO something about this
            //Dictionary<int, Game.Item> items = await ItemLoaderV2.LoadAllItemsAsync(true);
            //_itemIconIds = new Dictionary<string, int>();

            //foreach (Game.Item item in items.Values)
            //{
            //    if (item.IconId > 0)
            //    {
            //        _itemIconIds.AddOrUpdate(item.Id, item.IconId);
            //    }
            //}
        }

        protected override void OnGUIMid()
        {
            if (_itemIconIds == null) Reload();

            // Start a horizontal layout group.
            GUILayout.BeginHorizontal();

            // Retrieve sorted temporary database.
            tempDatabase = GetSortedTempDatabase();

            _iconSize = Mathf.Max(MIN_ICON_SIZE, (position.width - ICON_MARGINS) / ICONS_PER_ROW);

            // Check if the selected page has data, if not, display a message.
            if (!tempDatabase.ContainsKey(SelectedPage) || tempDatabase[SelectedPage].Count == 0)
            {
                EditorGUILayout.LabelField("No Data");
                GUILayout.EndHorizontal();
                return;
            }

            // Initialize an enumerator for iterating through the addressable objects.
            Dictionary<int, AddressableObject<Sprite>>.Enumerator enumerator = tempDatabase[SelectedPage].GetEnumerator();
            int itemCount = 0;

            // 임시로 삭제할 항목을 저장할 리스트 생성
            List<int> itemsToRemove = new();


            // Iterate through the addressable objects.
            while (enumerator.MoveNext())
            {
                (int id, AddressableObject<Sprite> addressableObject) = enumerator.Current;

                // Handle wrapping to the next row if needed.
                if (itemCount >= ICONS_PER_ROW)
                {
                    itemCount = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                // Start a vertical layout group with custom style.
                EditorGUILayout.BeginVertical(EGUI.Box(5, GUIColor.None), GUILayout.MaxWidth(_iconSize));

                // Increment the item count.
                itemCount++;

                // Display item based on edit mode.
                if (!_isEditMode)
                {
                    DisplayNonEditModeItem(id, addressableObject);
                }
                else
                {
                    bool removed = DisplayEditModeItem(id, addressableObject);
                    if (removed) itemsToRemove.Add(id);
                }

                // Show filename and handle asset selection if needed.
                ShowFilename(addressableObject);
                ShowItemIds(id);

                if (_isAssetSelector)
                {
                    if (GUILayout.Button("Select"))
                    {
                        OnSelect(id);
                    }
                }

                // End the vertical layout group.
                EditorGUILayout.EndVertical();
            }


            // Save changes to the database.
            foreach (KeyValuePair<int, AddressableObject<Sprite>> item in tempDatabase[SelectedPage])
            {
                database.SetValue(item.Key, item.Value);
            }

            foreach (int id in itemsToRemove)
            {
                tempDatabase[SelectedPage].Remove(id);
                // 다른 관련 작업 수행, 예를 들어 database에서도 삭제
                database.RemoveObjectFromScriptableObject(id);
            }

            _saveTrigger = false;

            // End the horizontal layout group.
            GUILayout.EndHorizontal();
        }

        private Sprite GetSprite(AddressableObject<Sprite> addressableObject)
        {
            if (addressableObject != null)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(addressableObject.AssetGUID);
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }
            return null;
        }

        private void DisplayNonEditModeItem(int id, AddressableObject<Sprite> addressableObject)
        {
            // Display ID.
            EditorGUILayout.LabelField($"Id: {id}", GUILayout.MaxWidth(_iconSize));
            Sprite sprite = GetSprite(addressableObject);
            if (sprite == null)
            {
                if (_cachedDefaultSprite == null)
                {
                    _cachedDefaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DEFAULT_SPRITE_PATH);
                }
                sprite = _cachedDefaultSprite;
                SaveAddressable(id, sprite, addressableObject);
            }
            EGUILayout.TextureField(sprite, new Vector2(_iconSize, _iconSize));
        }

        /// <summary>
        /// 삭제 버튼을 누르면 true를 반환합니다.
        /// </summary>

        private bool DisplayEditModeItem(int id, AddressableObject<Sprite> addressableObject)
        {
            // Start a horizontal layout group for editing.
            EditorGUILayout.BeginHorizontal();

            // Display ID.
            EditorGUILayout.LabelField($"Id: {id}", GUILayout.MaxWidth(_iconSize - 20f));
            GUILayout.FlexibleSpace();

            bool removed = false;
            //Display delete button and handle deletion.
            if (GUILayout.Button("X", GUILayout.Width(20f)))
            {
                // 삭제 버튼을 누르면 true를 반환합니다.
                removed = true;
            }

            float sizeOffset = 12f;
            float iconSize = _iconSize + sizeOffset;

            // End the horizontal layout group.
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Sprite current = GetSprite(addressableObject);
            Sprite newValue = EditorGUILayout.ObjectField(current, typeof(Sprite), false, GUILayout.Width(iconSize), GUILayout.Height(iconSize)) as Sprite;
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            // Handle saving changes.
            if (_saveTrigger || (newValue != null && newValue != current))
            {
                SaveAddressable(id, newValue, addressableObject);
            }

            return removed;
        }

        private void SaveAddressable(int id, Sprite sprite, AddressableObject<Sprite> addressableObject)
        {
            // Save changes to the addressable asset.
            string path = AssetDatabase.GetAssetPath(sprite);
            string guid = AssetDatabase.AssetPathToGUID(path);
            string filename = Path.GetFileName(path);

            // Logging.
            Debug.LogWarning($"New sprite found. Saving scriptable object...\nPath: {path}");

            // Update addressable object properties.
            addressableObject.Filename = filename;
            addressableObject.Reference = new AssetReference(guid);
            addressableObject.Labels = new string[] { database.GetSelectedLabel() };
            addressableObject.LoadAssetForEditor();

            // Save changes.
            AddressableEditorUtility.SaveAddressable(id, addressableObject, database.AddressableGroup);
            Save();
        }



        private GUIStyle CreateWrappedLabelStyle()
        {
            GUIStyle style = new(EditorStyles.label);
            style.fontSize = 10;
            style.wordWrap = true;
            return style;
        }

        private string ShowFilename(AddressableObject<Sprite> item)
        {
            _wrappedLabelStyle ??= CreateWrappedLabelStyle();
            string filename = item.Filename;

            EditorGUILayout.LabelField($"{filename}", _wrappedLabelStyle, GUILayout.Width(_iconSize), GUILayout.Height(40));
            return filename;
        }

        private void ShowItemIds(int id)
        {
            EditorGUILayout.BeginVertical();

            _wrappedLabelStyle ??= CreateWrappedLabelStyle();

            //EditorGUILayout.LabelField("Items", _wrappedLabelStyle, GUILayout.MaxWidth(_iconSize));

            // Display ItemId if item's iconId is same with id
            if (_itemIconIds.ContainsValue(id))
            {
                // 복수의 아이템이 같은 아이콘을 사용할 수 있으므로, 아이템 아이디를 모두 표시합니다.
                foreach (string itemId in _itemIconIds.GetKeys(id))
                {
                    EditorGUILayout.BeginVertical(EGUI.Box(GUIColor.Green));
                    EditorGUILayout.LabelField($"{itemId}", _wrappedLabelStyle, GUILayout.MaxWidth(_iconSize));
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
        }


        protected override void DrawExtraMenu()
        {
            base.DrawExtraMenu();

            if (GUILayout.Button("Change All Filter Mode", ButtonOptions))
            {
                foreach (KeyValuePair<int, AddressableObject<Sprite>> item in Database)
                {
                    if (item.Value != null)
                    {
                        TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.Value.Value)) as TextureImporter;
                        if (textureImporter != null)
                        {
                            textureImporter.filterMode = FilterMode.Point;
                            textureImporter.SaveAndReimport();
                        }
                    }
                }
            }

            if (GUILayout.Button("Resave All", ButtonOptions))
            {
                _saveTrigger = true;
            }
        }


        protected override void LoadDatabase() { }
        protected override void SaveDatabaseEntry(AddressableObject<Sprite> value) { }
    }
}