using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Glitch9.Database.Editor
{
    public static class AddressableEditorUtility
    {
        public static List<string> GetAllAddressableNames(string addressableGroup)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup(addressableGroup);
            if (group == null)
            {
                Debug.LogError($"Cannot find the group: {addressableGroup}");
                return null;
            }

            List<string> names = new();
            foreach (AddressableAssetEntry entry in group.entries)
            {
                names.Add(entry.address);
            }

            return names;
        }

        public static TValue LoadInEditor<TValue>(string groupName, string guid)
        where TValue : Object
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                Debug.LogError($"AddressableAssetSettings could not be loaded. isUpdating: {EditorApplication.isUpdating}, isCompiling: {EditorApplication.isCompiling}");
                return null;
            }
            try
            {
                List<AddressableAssetEntry> allEntries = new(settings.FindGroup(groupName).entries);
                AddressableAssetEntry foundEntry = allEntries.FirstOrDefault(e => e.address == guid);
                return foundEntry != null
                           ? AssetDatabase.LoadAssetAtPath<TValue>(foundEntry.AssetPath)
                           : null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load {typeof(TValue).Name} from {guid} in {groupName} group. {e.Message}");
                return null;
            }
        }

        public static Dictionary<int, AssetReference> GetAssetReferences(string[] labels, string group = null)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            List<AddressableAssetEntry> allEntries = new(group == null ? settings.groups.SelectMany(g => g.entries) : settings.FindGroup(group).entries);
            Dictionary<int, AssetReference> newDict = new();

            for (int i = 0; i < allEntries.Count; i++)
            {
                if (labels == null || labels.Length == 0)
                {
                    newDict.Add(i, new AssetReference(allEntries[i].address));
                }
                else
                {
                    /* add to newDict if all labels are present */
                    if (labels.All(l => allEntries[i].labels.Contains(l)))
                    {
                        newDict.Add(i, new AssetReference(allEntries[i].address));
                    }
                }
            }
            return newDict;
        }

        public static Dictionary<int, AssetReference> GetAssetReferences(string label, string group = null)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            List<AddressableAssetEntry> allEntries = new(group == null ? settings.groups.SelectMany(g => g.entries) : settings.FindGroup(group).entries);
            Dictionary<int, AssetReference> newDict = new();

            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].labels.Contains(label))
                {
                    newDict.Add(i, new AssetReference(allEntries[i].address));
                }
            }
            return newDict;
        }


        public static string GetLabel(int label)
        {
            List<string> labelList = AddressableAssetSettingsDefaultObject.Settings.GetLabels();
            string labelName = labelList[label];
            return labelName;
        }

        public static void SaveAddressables<TValue>(Dictionary<int, AddressableObject<TValue>> dict, string group) where TValue : UnityEngine.Object
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup addressableGroup = string.IsNullOrWhiteSpace(group) ? settings.DefaultGroup : settings.FindGroup(group);
            Debug.Log($"Saving {dict.Count} addressables to {addressableGroup.Name}");

            foreach (AddressableObject<TValue> item in dict.Values)
            {
                SaveAddressableWithGUID(settings, addressableGroup, item);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, dict.Values, true);
        }

        private static void SaveAddressableWithGUID<TValue>(AddressableAssetSettings settings, AddressableAssetGroup addressableGroup, AddressableObject<TValue> item) where TValue : UnityEngine.Object
        {
            /*get guid of Tvalue */
            if (item == null || string.IsNullOrEmpty(item.AssetGUID)) return;

            string path = AssetDatabase.GUIDToAssetPath(item.AssetGUID);
            string guid = AssetDatabase.AssetPathToGUID(path);

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, addressableGroup, false, false);
            if (entry == null)
            {
                Debug.LogError($"Failed to create or move entry for {item.Filename} to {addressableGroup.Name}");
                return;
            }
            entry.address = path;

            List<string> labelList = settings.GetLabels();
            for (int i = 0; i < labelList.Count; i++)
            {
                if (item.Labels.Contains(labelList[i]))
                {
                    entry.SetLabel(labelList[i], true, true);
                }
                else
                {
                    entry.SetLabel(labelList[i], false, true);
                }
            }

            Debug.Log($"Saved {item.Filename} to {entry.address} with labels {string.Join(", ", item.Labels)}");
        }

        public static void SaveAddressable<TValue>(int id, AddressableObject<TValue> value, string group) where TValue : UnityEngine.Object
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup addressableGroup = string.IsNullOrWhiteSpace(group) ? settings.DefaultGroup : settings.FindGroup(group);

            SaveAddressableWithGUID(settings, addressableGroup, value);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, value, true);
        }

        public static string[] GetGroupList()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            string[] groupList = new string[settings.groups.Count];
            for (int i = 0; i < settings.groups.Count; i++)
            {
                groupList[i] = settings.groups[i].Name;
            }
            return groupList;
        }

        public static void ResetAddressableNames(string group)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup engryGroup = group == null ? settings.DefaultGroup : settings.FindGroup(group);
            List<AddressableAssetEntry> allEntries = new(engryGroup.entries);
            foreach (AddressableAssetEntry item in allEntries)
            {
                item.address = item.AssetPath;
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, allEntries, true);
        }

        public static Dictionary<string, string> FixScriptableObjectGUID(string group, Dictionary<string, string> database)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup engryGroup = group == null ? settings.DefaultGroup : settings.FindGroup(group);
            List<AddressableAssetEntry> allEntries = new(engryGroup.entries);
            Dictionary<string, string> newDatabase = new();

            foreach (KeyValuePair<string, string> entry in database)
            {
                string entryGUID = entry.Value.Split(",")[0];
                string entryLabel = entry.Value.Split(",")[1];
                string entryFilename = Path.GetFileName(entryGUID);
                Debug.Log("Entry Filename : " + entryFilename);

                foreach (AddressableAssetEntry item in allEntries)
                {
                    string itemGUID = item.AssetPath;
                    string itemFilename = Path.GetFileName(item.AssetPath);
                    if (entryFilename == itemFilename)
                    {
                        string newEntry = itemGUID + "," + entryLabel;
                        newDatabase.Add(entry.Key, newEntry);
                        break;
                    }
                }
            }

            return newDatabase;
        }
    }
}