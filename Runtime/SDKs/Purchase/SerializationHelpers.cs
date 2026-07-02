using System;
using UnityEngine;

namespace Serialization
{
    /// <summary>
    /// Helper class for serializing arrays to JSON
    /// Unity's JsonUtility doesn't serialize arrays directly, so we wrap them
    /// </summary>
    [Serializable]
    public class SerializableArray<T>
    {
        public T[] items;
        
        public SerializableArray()
        {
            items = new T[0];
        }
        
        public SerializableArray(T[] items)
        {
            this.items = items ?? new T[0];
        }
    }
    
    /// <summary>
    /// Helper class for serializing dictionaries to JSON
    /// Unity's JsonUtility doesn't serialize dictionaries directly
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [SerializeField] private TKey[] keys;
        [SerializeField] private TValue[] values;
        
        public SerializableDictionary()
        {
            keys = new TKey[0];
            values = new TValue[0];
        }
        
        public SerializableDictionary(System.Collections.Generic.Dictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                keys = new TKey[0];
                values = new TValue[0];
                return;
            }
            
            var keyList = new System.Collections.Generic.List<TKey>();
            var valueList = new System.Collections.Generic.List<TValue>();
            
            foreach (var kvp in dictionary)
            {
                keyList.Add(kvp.Key);
                valueList.Add(kvp.Value);
            }
            
            keys = keyList.ToArray();
            values = valueList.ToArray();
        }
        
        public System.Collections.Generic.Dictionary<TKey, TValue> ToDictionary()
        {
            var dictionary = new System.Collections.Generic.Dictionary<TKey, TValue>();
            
            if (keys != null && values != null)
            {
                var minLength = Mathf.Min(keys.Length, values.Length);
                for (int i = 0; i < minLength; i++)
                {
                    dictionary[keys[i]] = values[i];
                }
            }
            
            return dictionary;
        }
    }
}