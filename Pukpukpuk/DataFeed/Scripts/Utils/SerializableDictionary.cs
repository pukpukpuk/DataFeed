using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Utils
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new();
        [SerializeField] private List<TValue> values = new();

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // load dictionary from lists 
        public void OnAfterDeserialize()
        {
            Clear();

            if (keys.Count != values.Count)
                throw new Exception(
                    $"there are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable. Types: {typeof(TKey)} {typeof(TValue)}");

            for (var i = 0; i < keys.Count; i++)
                Add(keys[i], values[i]);
        }

        public KeyValuePair<TKey, TValue> Get(int index)
        {
            return new KeyValuePair<TKey, TValue>(keys[index], values[index]);
        }
    }
}