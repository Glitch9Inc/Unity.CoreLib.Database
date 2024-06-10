using Glitch9.ExtendedEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Glitch9.Database.Editor
{
    public enum AddressableLabelScope
    {
        Display,
        Import,
        Management
    }
    /// <summary>
    /// 스크립터블 오브젝트 + 어드레서블을 이용한 데이터베이스를 캐싱하는 클래스
    /// </summary>
    public class CashedAddressableDatabase<TWindow, TDatabase, TValue>
        where TWindow : EditorWindow
        where TDatabase : AddressableDatabase<TDatabase, TValue>
        where TValue : UnityEngine.Object
    {
        // Fields
        public int SelectedLabelIndex;
        public string AddressableGroup;
        private Dictionary<string, int> _addressableLabels;
        private Dictionary<string, int> addressableLabels
        {
            get
            {
                _addressableLabels ??= LoadLabelsFromSO();
                return _addressableLabels;
            }
            set => _addressableLabels = value;
        }

        private Dictionary<int, AddressableObject<TValue>> _database;

        // Properties
        public IReadOnlyDictionary<string, int> AddressableLabels => addressableLabels;
        public IReadOnlyDictionary<int, AddressableObject<TValue>> Database => _database;

        public CashedAddressableDatabase()
        {
            _database = LoadReferencesFromSO();

            if (_database == null)
            {
                Debug.LogError("_database is null");
                return;
            }

            foreach ((int key, AddressableObject<TValue> value) in _database)
            {
                if (value == null)
                {
                    Debug.LogError($"Null value found in database: {key}");
                    continue;
                }
                value.LoadAssetForEditor();
            }
        }

        public void Save(Dictionary<TValue, string> values)
        {
            ScriptableDatabase resource = Resources.Load<ScriptableDatabase>(nameof(TDatabase));

            if (resource == null)
            {
                Debug.LogError($"Failed to load ScriptableObject of type {nameof(TDatabase)}");
                return;
            }

            UpdateScriptableObject(resource, values);
            CompleteSave(resource);
        }

        private void UpdateScriptableObject(ScriptableDatabase resource, IReadOnlyDictionary<TValue, string> values)
        {
            // Update existing entries and add new ones
            foreach ((TValue key, string value) in values)
            {
                string keyStr = key.ToString();
                if (resource.database.ContainsKey(keyStr))
                    resource.database[keyStr] = value;
                else
                    resource.database.Add(keyStr, value);
            }

            // Remove entries that no longer exist
            List<string> keysToRemove = resource.database.Keys
                .Where(k => !values.ContainsKey((TValue)Convert.ChangeType(k, typeof(TValue))))
                .ToList();

            foreach (string keyToRemove in keysToRemove) resource.database.Remove(keyToRemove);
        }

        public void RemoveObjectFromScriptableObject(int id)
        {
            ScriptableDatabase resource = Resources.Load<ScriptableDatabase>(typeof(TDatabase).Name);

            if (resource != null && resource.database.ContainsKey(id.ToString()))
            {
                resource.database.Remove(id.ToString());
                SaveChanges(resource);
            }
        }

        private void CompleteSave(ScriptableDatabase resource)
        {
            resource.addressableGroup = AddressableGroup;
            foreach ((string key, int value) in addressableLabels)
                if (!resource.addressableLabels.ContainsKey(key))
                    resource.addressableLabels.Add(key, value);

            SaveChanges(resource);
        }

        private void SaveChanges(ScriptableDatabase resource)
        {
            EditorUtility.SetDirty(resource);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Saved changes to scriptable object.");
        }

        public Dictionary<string, int> LoadLabelsFromSO()
        {
            string className = typeof(TDatabase).Name;

            if (className == null || className == "")
            {
                Debug.LogError("Class name not set!");
                return null;
            }

            Dictionary<string, int> newDict = ScriptableDatabase.GetLabels<TDatabase>();
            if (newDict == null)
            {
                Debug.LogError($"Failed to load {className} labels from json! The file may not exist.");
                return null;
            }

            Debug.Log($"Loaded {newDict.Count} {className} labels from json.");
            return newDict;
        }

        public Dictionary<int, AddressableObject<TValue>> LoadReferencesFromSO()
        {
            string className = typeof(TDatabase).Name;

            if (className == null || className == "")
            {
                Debug.LogError("Class name not set!");
                return null;
            }

            Dictionary<int, string> jsonDict = ScriptableDatabase.GetIntDict<TDatabase>();
            if (jsonDict == null)
            {
                Debug.LogError($"Failed to load {className} references from json! The file may not exist.");
                return null;
            }

            Dictionary<int, AddressableObject<TValue>> newDict = new();
            foreach (KeyValuePair<int, string> item in jsonDict)
            {
                AddressableObject<TValue> addressableObject = AddressableObject<TValue>.Deserialize(item.Value);
                newDict.Add(item.Key, addressableObject);
            }
            Debug.Log($"Loaded {newDict.Count} {className} references from json.");

            AddressableGroup = ScriptableDatabase.GetAddressableGroup<TDatabase>();
            Dictionary<string, int> temp = ScriptableDatabase.GetAddressableLabels<TDatabase>();

            if (temp != null)
            {
                foreach (KeyValuePair<string, int> item in temp)
                {
                    SetLabelStartingIndex(item.Key, item.Value);
                    SetLabel(item.Key, true, AddressableLabelScope.Display);
                }
            }
            else
            {
                Debug.LogError("Failed to load _addressableLabels");
            }

            return newDict;
        }

        public Dictionary<int, TValue> GetParsedDatabase()
        {
            _database ??= LoadReferencesFromSO();
            return _database.ToDictionary(entry => entry.Key, entry => entry.Value.LoadAssetForEditor());
        }

        public void SaveReferencesToSO()
        {
            string className = typeof(TDatabase).Name;
            if (className == null || className == "")
            {
                Debug.LogError("Class name not set!");
                return;
            }

            ScriptableDatabase res = Resources.Load("Database/" + className) as ScriptableDatabase;

            if (res == null)
            {
                Debug.LogError($"Failed to load {className} references from json! The file may not exist.");
                return;
            }

            if (res.database == null)
            {
                Debug.LogError($"Failed to load {className} references from json! The file may not exist.");
                return;
            }

            foreach (KeyValuePair<int, AddressableObject<TValue>> item in _database)
            {
                if (item.Value == null) continue;

                if (res.database.ContainsKey(item.Key.ToString()))
                {
                    res.database[item.Key.ToString()] = item.Value.Serialize();
                }
                else
                {
                    res.database.Add(item.Key.ToString(), item.Value.Serialize());
                }
            }

            /* remove that are not contained */
            List<string> toRemove = new();
            foreach (KeyValuePair<string, string> pair in res.database)
            {
                if (!_database.ContainsKey(int.Parse(pair.Key)))
                {
                    toRemove.Add(pair.Key.ToString());
                }
            }

            foreach (string keyToRemove in toRemove)
            {
                res.database.Remove(keyToRemove);
            }

            CompleteSave(res);
        }

        public List<string> GetDisplayingLabels()
        {
            List<string> labels = new();
            if (addressableLabels == null) return labels;
            foreach (KeyValuePair<string, int> item in addressableLabels)
            {
                if (GetLabel(item.Key, AddressableLabelScope.Display))
                {
                    labels.Add(item.Key);
                }
            }
            return labels;
        }

        public void ReapplyAllLabels()
        {
            foreach (KeyValuePair<int, AddressableObject<TValue>> item in _database)
            {
                string[] selectedLabels = GetLabels(AddressableLabelScope.Display);
                int[] minIndexes = new int[selectedLabels.Length];

                for (int i = 0; i < selectedLabels.Length; i++)
                {
                    minIndexes[i] = GetLabelStartingIndex(selectedLabels[i]);
                }

                for (int i = 0; i < minIndexes.Length; i++)
                {
                    if (i + 1 == minIndexes.Length)
                    {
                        item.Value.Labels = new string[] { selectedLabels[i] };
                        break;
                    }
                    else
                    {
                        if (item.Key < minIndexes[i + 1])
                        {
                            item.Value.Labels = new string[] { selectedLabels[i] };
                            break;
                        }
                    }
                }
            }
        }

        public void AddNewEntry()
        {
            int id = GetNextId();
            if (id == -1) return;
            AddressableObject<TValue> newObj = new();
            newObj.Labels = new List<string>() { GetSelectedLabel() }.ToArray();
            _database.Add(id, newObj);
        }

        private int GetNextId()
        {
            List<string> labels = GetDisplayingLabels();
            int labelIndex = labels.IndexOf(GetSelectedLabel());

            if (labelIndex == -1)
            {
                Debug.LogWarning("Label not found");
                return -1;
            }

            int startingIndex = AddressableLabels[labels[labelIndex]];
            int nextId = startingIndex;
            while (Database.ContainsKey(nextId))
            {
                nextId++;
            }
            return nextId;
        }

        public void SetValue(int id, AddressableObject<TValue> value)
        {
            if (Database.ContainsKey(id))
            {
                _database[id] = value;
            }
            else
            {
                _database.Add(id, value);
            }
        }

        public int GetLabelStartingIndex(string label)
            => EditorPrefs.GetInt(typeof(TWindow).Name + "_label_" + label + "_startingIndex");
        public void SetLabelStartingIndex(string label, int value)
            => EditorPrefs.SetInt(typeof(TWindow).Name + "_label_" + label + "_startingIndex", value);
        public bool GetLabel(string label, AddressableLabelScope type)
            => EditorPrefs.GetBool(typeof(TWindow).Name + "_label_" + label + "_" + type);
        public void SetLabel(string label, bool value, AddressableLabelScope type)
            => EditorPrefs.SetBool(typeof(TWindow).Name + "_label_" + label + "_" + type, value);

        public void ReorderLablesByStartingIndex()
        {
            List<string> labels = GetDisplayingLabels();
            labels.Sort((a, b) => GetLabelStartingIndex(a).CompareTo(GetLabelStartingIndex(b)));
            for (int i = 0; i < labels.Count; i++)
            {
                SetLabelStartingIndex(labels[i], i);
            }
        }

        public string GetSelectedLabel()
        {
            List<string> labels = GetDisplayingLabels();
            if (labels.Count == 0) return null;
            if (SelectedLabelIndex >= labels.Count) SelectedLabelIndex = 0;
            return labels[SelectedLabelIndex];
        }

        public void SetSelectedLabel(string label)
        {
            List<string> labels = GetDisplayingLabels();
            if (labels.Count == 0) return;
            SelectedLabelIndex = label == null ? 0 : labels.IndexOf(label);
        }

        public string[] GetLabels(AddressableLabelScope type)
        {
            List<string> labels = new();
            foreach (KeyValuePair<string, int> item in addressableLabels)
            {
                if (GetLabel(item.Key, type)) labels.Add(item.Key);
            }
            return labels.ToArray();
        }

        public void ImportAddressables()
        {
            string[] selectedLabels = GetLabels(AddressableLabelScope.Import);
            foreach (string label in selectedLabels)
            {
                Debug.LogError($"Importing {label}...");

                Dictionary<int, UnityEngine.AddressableAssets.AssetReference> refDict = AddressableEditorUtility.GetAssetReferences(label, AddressableGroup);
                foreach ((int key, UnityEngine.AddressableAssets.AssetReference value) in refDict)
                {
                    bool isDuplicate = _database.Values.Any(v => v?.Reference == value);
                    if (isDuplicate)
                    {
                        Debug.Log($"Skipping {value} because it is already in database.");
                        continue;
                    }

                    AddressableObject<TValue> addressableObject = new()
                    {
                        Reference = value,
                        Labels = new[] { label }
                    };

                    int minIndex = GetLabelStartingIndex(label);
                    while (_database.ContainsKey(minIndex)) minIndex++;
                    _database.Add(minIndex, addressableObject);

                    Debug.Log($"Importing {value} id:{minIndex} to database.");
                }
            }

            Debug.Log($"Import Complete (Count: {_database.Count})");
        }

        //Helper Methods
        public void ResetAddressableNames()
        {
            AddressableEditorUtility.ResetAddressableNames(AddressableGroup);
        }

        public void FixScriptableObjectGUID()
        {
            Dictionary<string, string> dict = ScriptableDatabase.Get<TDatabase>();
            Dictionary<string, string> fixedDict = AddressableEditorUtility.FixScriptableObjectGUID(AddressableGroup, dict);
            ScriptableDatabase.Set<TDatabase>(fixedDict);
        }

        public void SetAllAddressableNamesToObjectId()
        {
            if (EditorUtility.DisplayDialog("Warning", "Set all to id?", "Ok"))
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null)
                {
                    AddressableAssetGroup group = settings.FindGroup(AddressableGroup);
                    if (group == null)
                    {
                        Debug.LogError($"Addressable group '{AddressableGroup}' not found.");
                        return;
                    }

                    foreach (KeyValuePair<int, AddressableObject<TValue>> data in _database)
                    {
                        int key = data.Key;
                        string path = data.Value.AssetGUID;
                        AddressableAssetEntry entry = GetEntry(group, path);

                        if (entry != null)
                        {
                            entry.address = key.ToString();
                        }
                        else
                        {
                            Debug.LogError($"Addressable with address '{path}' not found.");
                        }
                    }

                    // Save changes.
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private AddressableAssetEntry GetEntry(AddressableAssetGroup group, string guid)
        {
            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry.address == guid) return entry;
            }
            return null;
        }

        public void AddLabel()
        {
            EditorInputField.Show("Add Label", "Enter Label Name", (string label) =>
            {
                if (addressableLabels.ContainsKey(label))
                {
                    EditorUtility.DisplayDialog("Error", "Label already exists", "Ok");
                }
                else
                {
                    addressableLabels.Add(label, (addressableLabels.Count - 1) * 1000);
                    AddressableAssetSettingsDefaultObject.Settings.AddLabel(label);
                }
            });
        }

        public void SetAddressableLabelIndex(string key, int index)
        {
            if (!addressableLabels.ContainsKey(key))
            {
                Debug.LogError($"Failed to set addressable label index: {key} does not exist");
                return;
            }

            addressableLabels[key] = index;
        }

        public void RemoveSelectedLabels()
        {
            bool ok = EditorUtility.DisplayDialog("Warning", "Remove selected labels?", "Ok");
            if (ok)
            {
                foreach (KeyValuePair<string, int> item in addressableLabels)
                {
                    if (GetLabel(item.Key, AddressableLabelScope.Import))
                    {
                        AddressableAssetSettingsDefaultObject.Settings.RemoveLabel(item.Key);
                        addressableLabels.Remove(item.Key);
                    }
                }
            }
        }

    }
}
