using System.Collections.Generic;
using UnityEngine;

public abstract class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();
    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public void OnAfterDeserialize()
    {
        Clear();

        for (int i = 0; i < keys.Count && i < values.Count; i++)
        {
            this[keys[i]] = values[i];
        }
    }

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
}
