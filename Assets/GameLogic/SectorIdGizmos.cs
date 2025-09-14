// Assets/Scripts/Editor/SectorIdGizmos.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class SectorIdGizmos : MonoBehaviour
{
    public MapManager map;              // перетащи MapManager из сцены
    public bool showInEditor = true;
    public Color textColor = Color.white;

    void OnDrawGizmos()
    {
        if (!showInEditor || map == null || map.Sectors == null) return;

        var style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = textColor;

        foreach (var s in map.Sectors)
        {
            Handles.Label(s.CenterWorld, s.Id.ToString(), style);
        }
    }
}
#endif
