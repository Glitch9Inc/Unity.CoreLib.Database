using System.Collections.Generic;
using UnityEngine;

namespace Glitch9.Database
{
    public abstract class DatabaseBase<TSelf, TKey, TValue>
        where TSelf : DatabaseBase<TSelf, TKey, TValue>
        where TKey : struct
        where TValue : class
    {
        protected static Dictionary<TKey, TValue> InternalDatabase;
        public static string Name => typeof(TSelf).Name;
        public static IReadOnlyDictionary<TKey, TValue> DB => InternalDatabase;

        /// <summary>
        /// 에디터에서 Application.isPlaying이 false일때 인스턴스를 불러오기 위해 사용
        /// </summary>
        public static Dictionary<TKey, TValue> GetDatabase()
        {
            if (InternalDatabase == null)
            {
                Debug.LogError($"{Name} is not initialized");
                return null;
            }
            return InternalDatabase;
        }

        public static bool ContainsKey(TKey key)
        {
            if (InternalDatabase == null)
            {
                Debug.LogError($"{Name} is not initialized");
                return false;
            }
            return InternalDatabase.ContainsKey(key);
        }

        public static TValue Get(TKey key, TValue defaultValue = null)
        {
            if (InternalDatabase == null)
            {
                Debug.LogError($"{Name} is not initialized");
                return defaultValue;
            }
            return InternalDatabase.GetValueOrDefault(key, defaultValue);
        }

        public static TKey? GetKey(TValue value)
        {
            if (InternalDatabase == null)
            {
                Debug.LogError($"{Name} is not initialized");
                return null;
            }
            foreach (KeyValuePair<TKey, TValue> pair in InternalDatabase)
            {
                if (EqualityComparer<TValue>.Default.Equals(pair.Value, value))
                {
                    return pair.Key;
                }
            }
            Debug.LogError($"{Name} doesn't have value: {value}");
            return null;
        }
    }
}
