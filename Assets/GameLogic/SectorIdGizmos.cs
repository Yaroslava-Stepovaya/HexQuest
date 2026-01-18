// Assets/Scripts/Editor/SectorIdGizmos.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class SectorIdGizmos : MonoBehaviour
{
    public MapManager map;              // drag MapManager from the scene
    public bool showInEditor = true;    
    public Color textColor = Color.white;

    void OnDrawGizmos()
    {
        if (!showInEditor || map == null || map.Sectors == null) return;   //return nothing if there's no sectors

        var style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = textColor;

        foreach (var s in map.Sectors)    //for each sector
        {
            if (map.mapAsset.WinSectorId != s.Id)
                Handles.Label(s.CenterWorld, s.Id.ToString(), style);   //numerate each sector
            else
                Handles.Label(s.CenterWorld, "⚑", style);
        }
    }
}
#endif
