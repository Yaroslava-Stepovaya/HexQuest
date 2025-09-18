#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class CellCoordinatesGizmos : MonoBehaviour
{
    public Tilemap tilemap;
    public Color textColor = Color.yellow;  //set color for the text
    public int fontSize = 12;

    void OnDrawGizmos()
    {
        if (tilemap == null) return;    //return nothing if there is no tilemap

        var style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = textColor;
        style.fontSize = fontSize;

        BoundsInt bounds = tilemap.cellBounds;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell)) continue; // skip empty cells

                Vector3 world = tilemap.GetCellCenterWorld(cell);    //get centroid
                Handles.Label(world, cell.ToString(), style);        //draw coordinates on the tile
            }
    }
}
#endif
