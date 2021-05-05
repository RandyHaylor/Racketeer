using System;

[Serializable]
public struct InspectableKvp<K,T>
{
    public K Key;
    public T Value;
    public InspectableKvp(K key, T value)
    {
        Key = key;
        Value = value;
    }
}
