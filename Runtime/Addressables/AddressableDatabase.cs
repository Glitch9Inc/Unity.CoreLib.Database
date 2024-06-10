using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
// ReSharper disable StaticMemberInGenericType

namespace Glitch9.Database
{
    public abstract class AddressableDatabase<TSelf, TValue> : DatabaseBase<TSelf, int, AddressableObject<TValue>>
        where TSelf : AddressableDatabase<TSelf, TValue>
        where TValue : UnityEngine.Object
    {
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public static string AddressableGroup;
        public static Dictionary<string, int> AddressableLabels; // labels, starting index 


        /// <summary>
        /// 에디터에서 Application.isPlaying이 false일때 인스턴스를 불러오기 위해 사용
        /// </summary>
        public static async UniTask<Dictionary<int, AddressableObject<TValue>>> GetDatabaseAsync()
        {
            await InitializeAsync();
            return InternalDatabase;
        }

        public static async UniTask InitializeAsync(Action<bool> onSuccess = null)
        {
            Debug.Log($"Initializing {typeof(TSelf).Name}");

            try
            {
                await _semaphore.WaitAsync();

                if (Application.isPlaying)
                {
                    await LoadAsync();
                }
                else
                {
                    LoadAssetsEditor();
                }

                onSuccess?.Invoke(true);
            }
            catch (Exception e)
            {
                GNLog.Exception(e);
                onSuccess?.Invoke(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static async UniTask LoadAsync()
        {
            string className = typeof(TSelf).Name;
            InternalDatabase = LoadReferencesFromScriptableObject();
            if (InternalDatabase.IsNullOrEmpty())
            {
                GNLog.Error($"Failed to load {className} references!");
                return;
            }

            List<AssetReference> refsList = new();
            foreach (KeyValuePair<int, AddressableObject<TValue>> item in InternalDatabase)
            {
                if (item.Value == null || item.Value.Reference == null) continue;
                refsList.Add(item.Value.Reference);
            }

            if (refsList.Count == 0)
            {
                GNLog.Error("Addressable references count is 0.");
                return;
            }

            try
            {
                long totalSize = await Addressables.GetDownloadSizeAsync(refsList).ToUniTask();
                Debug.Log($"Loading <color=blue>{className}</color> from <color=blue>{refsList.Count}</color> references. Total size: <color=blue>{totalSize} bytes</color>");
            }
            catch (Exception e)
            {
                GNLog.Critical($"Failed to get download size: {e.Message}");
            }


            List<UniTask> loadTasks = new();
            foreach (KeyValuePair<int, AddressableObject<TValue>> item in InternalDatabase)
            {
                if (item.Value == null || item.Value.Reference == null) continue;

                try
                {
                    UniTaskCompletionSource tcs = new();
                    item.Value.Reference.LoadAssetAsync<TValue>().Completed += handle =>
                    {
                        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                        {
                            item.Value.Value = handle.Result;
                            tcs.TrySetResult();
                        }
                        else
                        {
                            tcs.TrySetException(new Exception("Failed to load asset"));
                        }
                    };

                    loadTasks.Add(tcs.Task);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    GNLog.Error($"Failed to load {item.Value.Reference.editorAsset.name} asset: {e.Message}");
#else
                    GNLog.Error($"Failed to load {item.Value.Reference.Asset.name} asset: {e.Message}");
#endif
                }
            }

            await UniTask.WhenAll(loadTasks);//.ConfigureAwait(false);
            GNLog.Info($"Loaded <color=blue>{className}</color> from <color=blue>{refsList.Count}</color> references.");
        }

        private static void LoadAssetsEditor()
        {
#if UNITY_EDITOR
            InternalDatabase = LoadReferencesFromScriptableObject();
            if (InternalDatabase == null)
            {
                Debug.LogError("Failed to load references!");
                return;
            }

            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (KeyValuePair<int, AddressableObject<TValue>> item in InternalDatabase)
            {
                if (item.Value == null || string.IsNullOrEmpty(item.Value.AssetGUID)) continue;
                string assetPath = AssetDatabase.GUIDToAssetPath(item.Value.AssetGUID);
                if (string.IsNullOrEmpty(assetPath)) continue;

                TValue asset = AssetDatabase.LoadAssetAtPath<TValue>(assetPath);
                if (asset != null)
                {
                    item.Value.Value = asset;
                }
            }
#endif
        }

        private static Dictionary<int, AddressableObject<TValue>> LoadReferencesFromScriptableObject()
        {
            string className = typeof(TSelf).Name;

            if (string.IsNullOrEmpty(className))
            {
                Debug.LogError("Class name not set!");
                return null;
            }

            Dictionary<int, string> scriptableObject = ScriptableDatabase.GetIntDict<TSelf>();
            if (scriptableObject == null)
            {
                Debug.LogError($"Failed to load {className} references!");
                return null;
            }

            Dictionary<int, AddressableObject<TValue>> newDict = new();
            foreach (KeyValuePair<int, string> item in scriptableObject)
            {
                AddressableObject<TValue> addressableObject = AddressableObject<TValue>.Deserialize(item.Value);
                newDict.Add(item.Key, addressableObject);
            }

            return newDict;
        }

        public static TValue Get(int id, TValue defaultValue = null)
        {
            if (InternalDatabase.IsNullOrEmpty())
            {
                Debug.LogError("Database is empty!");
                return defaultValue;
            }

            if (id < 0) return defaultValue;
            return InternalDatabase.TryGetValue(id, out AddressableObject<TValue> obj) ? obj.Value : defaultValue;
        }

        public static TValue Get(int id, int defaultValueId)
        {
            if (InternalDatabase.IsNullOrEmpty())
            {
                Debug.LogError("Database is empty!");
                return Get(defaultValueId);
            }

            if (id < 0) return Get(defaultValueId);
            return InternalDatabase.TryGetValue(id, out AddressableObject<TValue> obj) ? obj.Value : Get(defaultValueId);
        }

        public static int GetKey(TValue value)
        {
            if (InternalDatabase.IsNullOrEmpty())
            {
                Debug.LogError("Database is empty!");
                return -1;
            }

            foreach (KeyValuePair<int, AddressableObject<TValue>> item in InternalDatabase)
            {
                if (item.Value.Value == value)
                {
                    return item.Key;
                }
            }

            Debug.LogError("Value not found in the database!");
            return -1;
        }
    }
}
