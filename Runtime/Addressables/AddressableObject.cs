using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Glitch9.Database
{
    public class AddressableObject<TValue> where TValue : UnityEngine.Object
    {
        public int Id { get; set; }
        public TValue Value { get; set; }
        public string Filename { get; set; }
        public AssetReference Reference { get; set; }
        public string AssetGUID => Reference?.AssetGUID;
        public string[] Labels { get; set; }

        public AddressableObject(AssetReference reference, string[] labels = null)
        {
            Reference = reference;
            Labels = labels ?? Array.Empty<string>();
        }

        public AddressableObject() { }

        public bool ContainsLabel(string label)
        {
            return Labels?.Contains(label) ?? false;
        }

        public void LoadAssetAsync(System.Action<TValue> onComplete = null, System.Action onFail = null)
        {
            AsyncOperationHandle<TValue> handle = Addressables.LoadAssetAsync<TValue>(Reference);
            handle.Completed += (result) =>
            {
                if (result.Status == AsyncOperationStatus.Succeeded)
                {
                    Value = result.Result;
                    onComplete?.Invoke(Value);
                }
                else
                {
                    onFail?.Invoke();
                }
            };
        }

#if UNITY_EDITOR
        public TValue LoadAssetForEditor()
        {
            if (Reference == null) return null;
            if (string.IsNullOrEmpty(AssetGUID)) return null;
            try
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(AssetGUID);
                Debug.Log($"Loading asset at path: {assetPath}");
                return AssetDatabase.LoadAssetAtPath<TValue>(assetPath);
            }
            catch
            {
                Debug.LogWarning($"Failed to load asset at path: {AssetGUID}");
                return null;
            }
        }
#endif
        public string Serialize()
        {
            return $"{Filename}|{AssetGUID}|{string.Join(",", Labels)}";
        }

        public static AddressableObject<TValue> Deserialize(string serialized)
        {
            if (string.IsNullOrEmpty(serialized)) return null;

            string[] parts = serialized.Split('|');
            if (parts.Length != 3) return null;

            string filename = parts[0];
            string guid = parts[1];
            string[] labels = parts[2].Split(',');
            return new AddressableObject<TValue>(new AssetReference(guid), labels) { Filename = filename };
        }
    }
}