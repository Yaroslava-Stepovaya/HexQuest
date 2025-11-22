using UnityEngine;

public class KeyView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public MapKeyData Data { get; private set; }
    public KeyDefinition Definition { get; private set; }

    public void Init(MapKeyData data, KeyDefinition def, Vector3 worldPos)
    {
        Data = data;
        Definition = def;

        transform.position = worldPos;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && def != null)
            spriteRenderer.color = def.Color;
    }
}