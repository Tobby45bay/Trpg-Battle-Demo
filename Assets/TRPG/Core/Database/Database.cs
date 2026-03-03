using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TRPG.Core.Database
{
    public interface IDataEntry
    {
        int ID { get; }
    }

    public interface IDataDatabase<T> where T : IDataEntry
    {
        T Get(int id);
        bool TryGet(int id, out T entry);
        IReadOnlyDictionary<int, T> GetAll();
    }

    public class GameDatabase<T> : IDataDatabase<T> where T : IDataEntry
    {
        private readonly Dictionary<int, T> entries;

        public GameDatabase(IEnumerable<T> source)
        {
            entries = new Dictionary<int, T>();

            foreach (var entry in source)
            {
                if (entries.ContainsKey(entry.ID))
                {
                    Debug.LogError(
                        $"Duplicate ID {entry.ID} in database for type {typeof(T).Name}"
                    );
                    continue;
                }

                entries.Add(entry.ID, entry);
            }
        }

        public T Get(int id)
            => entries[id];

        public bool TryGet(int id, out T entry)
            => entries.TryGetValue(id, out entry);

        public IReadOnlyDictionary<int, T> GetAll()
            => entries;
    }

    public interface IDataAuthor<T> where T : IDataEntry
    {
        T Build();
    }
}