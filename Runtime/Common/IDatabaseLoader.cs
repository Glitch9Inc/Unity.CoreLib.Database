using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Glitch9.Database
{
    public interface IDatabaseLoader<TKey, TValue> where TValue : class
    {
        UniTask<Dictionary<TKey, TValue>> LoadDatabaseAsync(string databaseName);
    }
}