// Assets/Scripts/Editor/SectorMapBaker.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class SectorMapBaker : MonoBehaviour
{
    [Header("Input")]
    public Tilemap sourceTilemap;         // single Tilemap with color
    
    public bool flatTop = true;           

    public bool evenOffset = true;        //odd or even tiles  

    [Header("Output")]
    public MapAsset targetAsset;          // asset with final result

    [ContextMenu("Bake → MapAsset")]      //create context menu to launch the script from Unity environment
    public void Bake()
    {
        if (sourceTilemap == null || targetAsset == null)
        {
            Debug.LogError("Assign sourceTilemap and targetAsset.");      //check if sector is filled
            return;
        }

        //lists for sectors, filled tilemaps and cells in sector's ID storage
        var sectors = new List<SectorData>();
        var visited = new HashSet<Vector3Int>();
        var cellToSector = new Dictionary<Vector3Int, int>();

        BoundsInt bounds = sourceTilemap.cellBounds;      //tilemap bounds

        // 1) Flood-fill based on tilemaps of the same color
        for (int y = bounds.yMin; y < bounds.yMax; y++)
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                var p = new Vector3Int(x, y, 0);
                var t = sourceTilemap.GetTile(p);
                if (t == null || visited.Contains(p)) continue;

                int sid = sectors.Count;         
                var sector = new SectorData { id = sid, displayColor = sourceTilemap.GetColor(p) };        //new sector
                FloodFillByTile(p, t, sector, visited, cellToSector);       //fill tilemaps of the same sector
                sector.centerWorld = ComputeCenterWorld(sector.cells);      //sector's centre
                sectors.Add(sector);
            }

        // 2) Neighbours (find sectors that have bounds with given sector)
        var neighborSets = new List<HashSet<int>>(sectors.Count);
        for (int i = 0; i < sectors.Count; i++) neighborSets.Add(new HashSet<int>());

        foreach (var kv in cellToSector)
        {
            foreach (var n in Neigh(kv.Key))     //for each neighbour
            {
                if (!cellToSector.TryGetValue(n, out int other)) continue;     
                if (other != kv.Value)        //if tilemap belongs to another sector    
                {
                    neighborSets[kv.Value].Add(other);      
                    neighborSets[other].Add(kv.Value);      //add them as a pair of neighbours
                }
            }
        }

        // 3) fill edges with basic values
        for (int i = 0; i < sectors.Count; i++)
        {
            sectors[i].edges.Clear();              //clear previous values 
            foreach (var nb in neighborSets[i])
                sectors[i].edges.Add(new EdgeData { toSectorId = nb, weight = 1f });
        }

        // 4) save to MapAsset
        targetAsset.sectors = sectors;
        EditorUtility.SetDirty(targetAsset);
        AssetDatabase.SaveAssets();

        Debug.Log($"Bake OK: sectors = {sectors.Count}");
        // local functions //
        Vector3 ComputeCenterWorld(List<Vector3Int> cells)
        {
            //calculates centroids of the sector
            if (cells.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var c in cells) sum += sourceTilemap.GetCellCenterWorld(c);
            return sum / cells.Count;
        }

        //fills tiles with the same color and adds it to the same sector
        void FloodFillByTile(Vector3Int start, TileBase tile, SectorData sector,
                             HashSet<Vector3Int> vis, Dictionary<Vector3Int, int> index)
        {
            var q = new Queue<Vector3Int>();
            q.Enqueue(start); vis.Add(start);   //adds tile to the queue

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                index[p] = sector.id;
                sector.cells.Add(p);    //adds tile to the list

                var neighs = Neigh(p);
                foreach (var n in neighs)    //for each neighbour of the current tile
                {
                    if (!vis.Contains(n) && sourceTilemap.GetTile(n) == tile)    //checks if this tile is already added
                    {
                        vis.Add(n);
                        q.Enqueue(n);
                    }
                }
            }
        }

        IEnumerable<Vector3Int> Neigh(Vector3Int p)
        {
            if (flatTop)

                //returns neighbour tiles based on their type (flatTop) and offset (evenOffset)
            {
                bool even = ((p.x & 1) == 0) == evenOffset;
                var ev = new[] { new Vector3Int(+1, 0, 0), new(-1, 0, 0), new(0, +1, 0), new(0, -1, 0), new(+1, +1, 0), new(+1, -1, 0) };
                var od = new[] { new Vector3Int(+1, 0, 0), new(-1, 0, 0), new(0, +1, 0), new(0, -1, 0), new(-1, +1, 0), new(-1, -1, 0) };
                var offs = even ? ev : od;
                for (int i = 0; i < offs.Length; i++) yield return p + offs[i];
            }
            else
            {
                bool even = ((p.y & 1) == 0) == evenOffset;
                var ev = new[] { new Vector3Int(+1, 0, 0), new(-1, 0, 0), new(+1, -1, 0), new(0, -1, 0), new(+1, +1, 0), new(0, +1, 0) };
                var od = new[] { new Vector3Int(+1, 0, 0), new(-1, 0, 0), new(0, -1, 0), new(-1, -1, 0), new(0, +1, 0), new(-1, +1, 0) };
                var offs = even ? ev : od;
                for (int i = 0; i < offs.Length; i++) yield return p + offs[i];
            }
        }
    }
}
#endif
