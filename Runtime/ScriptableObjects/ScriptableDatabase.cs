using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Glitch9.CoreLib.Database
{
    [CreateAssetMenu(fileName = "Scriptable Database", menuName = "Glitch9/Database/Scriptable Database")]
    public class ScriptableDatabase : ScriptableObject
    {
        public string addressableGroup;
        public SerializedDictionary<string, int> addressableLabels; // labels, starting index 
        public SerializedDictionary<string, string> database;
        private const string DATABASE_PATH = "Assets/_project/Resources/Database/";

        public static void Create<TDatabaseClass>()
        {
#if UNITY_EDITOR
            string db = typeof(TDatabaseClass).Name;
            ScriptableDatabase res = Resources.Load(db) as ScriptableDatabase;
            if (res != null) return;
            System.IO.DirectoryInfo di = System.IO.Directory.CreateDirectory(DATABASE_PATH);
            ScriptableDatabase obj = CreateInstance<ScriptableDatabase>();
            string filePath = DATABASE_PATH + db + ".asset";
            Debug.Log("Database Missing, New Created: " + filePath);
            UnityEditor.AssetDatabase.CreateAsset(obj, filePath);
            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }

        public static Dictionary<string, int> GetLabels<TDatabaseClass>()
        {
            string db = typeof(TDatabaseClass).Name;
            ScriptableDatabase res = Resources.Load("Database/" + db) as ScriptableDatabase;

            if (res == null)
            {
                Create<TDatabaseClass>();
                res = Resources.Load("Database/" + db) as ScriptableDatabase;
            }

            return res.addressableLabels;
        }


        public static Dictionary<string, string> Get<TDatabaseClass>()
        {
            string db = typeof(TDatabaseClass).Name;
            ScriptableDatabase res = Resources.Load("Database/" + db) as ScriptableDatabase;

            if (res == null)
            {
                Create<TDatabaseClass>();
                res = Resources.Load("Database/" + db) as ScriptableDatabase;
            }

            Dictionary<string, string> dict = new();
            foreach (KeyValuePair<string, string> item in res.database)
            {
                dict.Add(item.Key, item.Value);
            }

            return dict;
        }

        public static void Set<TDatabaseClass>(Dictionary<string, string> dict)
        {
            string db = typeof(TDatabaseClass).Name;
            ScriptableDatabase res = Resources.Load("Database/" + db) as ScriptableDatabase;

            if (res == null)
            {
                Create<TDatabaseClass>();
                res = Resources.Load("Database/" + db) as ScriptableDatabase;
            }

            SerializedDictionary<string, string> serializedDict = new();

            foreach (KeyValuePair<string, string> item in dict)
            {
                serializedDict.Add(item.Key, item.Value);
            }

            res.database = serializedDict;
        }

        public static Dictionary<int, string> GetIntDict<TDatabaseClass>()
        {
            string db = typeof(TDatabaseClass).Name;
            ScriptableDatabase res = Resources.Load("Database/" + db) as ScriptableDatabase;

            if (res == null)
            {
                Create<TDatabaseClass>();
                res = Resources.Load("Database/" + db) as ScriptableDatabase;
            }

            Dictionary<int, string> dict = new();
            foreach (KeyValuePair<string, string> item in res.database)
            {
                dict.Add(int.Parse(item.Key), item.Value);
            }

            return dict;
        }

        public static string GetAddressableGroup<TDatabaseClass>()
        {
            string db = typeof(TDatabaseClass).Name;
            ScriptableDatabase res = Resources.Load("Database/" + db) as ScriptableDatabase;

            if (res == null)
            {
                Create<TDatabaseClass>();
                res = Resources.Load("Database/" + db) as ScriptableDatabase;
            }

            return res.addressableGroup;
        }

        public static Dictionary<string, int> GetAddressableLabels<TDatabaseClass>()
        {
            string db = typeof(TDatabaseClass).Name;
            ScriptableDatabase res = Resources.Load("Database/" + db) as ScriptableDatabase;

            if (res == null)
            {
                Create<TDatabaseClass>();
                res = Resources.Load("Database/" + db) as ScriptableDatabase;
            }

            return res.addressableLabels;
        }

        public void SortDatabaseById()
        {
            List<KeyValuePair<string, string>> sortedList = database.OrderBy(x => int.Parse(x.Key)).ToList();

            database.Clear();
            foreach (KeyValuePair<string, string> kvp in sortedList)
            {
                database.Add(kvp.Key, kvp.Value);
            }
        }


    }
}