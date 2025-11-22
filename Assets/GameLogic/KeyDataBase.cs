using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "KeyDatabase", menuName = "HexQuest/Key Database")]
public class KeyDatabase : ScriptableObject
{
    public KeyDefinition[] keys;

    private Dictionary<KeyType, KeyDefinition> _byType;

    public void Init()
    {
        if (_byType != null) return;

        _byType = new Dictionary<KeyType, KeyDefinition>();
        foreach (var def in keys)
        {
            if (def != null && !_byType.ContainsKey(def.KeyType))
                _byType.Add(def.KeyType, def);
        }
    }

    public KeyDefinition Get(KeyType type)
    {
        Init();
        return _byType.TryGetValue(type, out var def) ? def : null;
    }
}