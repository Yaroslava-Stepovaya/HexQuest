using UnityEngine;

[CreateAssetMenu(fileName = "KeyDefinition", menuName = "HexQuest/Key Definition")]
public class KeyDefinition: ScriptableObject
{
    [Header("Main settings")]
    public KeyType KeyType;
    public string DisplayName;

    [Header("View settings")]
    public Color Color = Color.white;
}
