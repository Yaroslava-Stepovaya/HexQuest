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
    public Tilemap sourceTilemap;         // ваш Hex Tilemap с раскраской
    
    public bool flatTop = true;

    public bool evenOffset = true;

    [Header("Output")]
    public MapAsset targetAsset;          // сюда запишем результат

    [ContextMenu("Bake → MapAsset")]
    public void Bake()
    {
        if (sourceTilemap == null || targetAsset == null)
        {
            Debug.LogError("Assign sourceTilemap and targetAsset.");
            return;
        }

        var sectors = new List<SectorData>();
        var visited = new HashSet<Vector3Int>();
        var cellToSector = new Dictionary<Vector3Int, int>();

        BoundsInt bounds = sourceTilemap.cellBounds;

        // 1) Flood-fill по одинаковому TileBase
        for (int y = bounds.yMin; y < bounds.yMax; y++)
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                var p = new Vector3Int(x, y, 0);
                var t = sourceTilemap.GetTile(p);
                if (t == null || visited.Contains(p)) continue;

                int sid = sectors.Count;
                var sector = new SectorData { id = sid, displayColor = sourceTilemap.GetColor(p) };
                FloodFillByTile(p, t, sector, visited, cellToSector);
                // центр
                sector.centerWorld = ComputeCenterWorld(sector.cells);
                sectors.Add(sector);
            }

        // 2) Соседство (грани между разными секторами)
        var neighborSets = new List<HashSet<int>>(sectors.Count);
        for (int i = 0; i < sectors.Count; i++) neighborSets.Add(new HashSet<int>());

        foreach (var kv in cellToSector)
        {
            foreach (var n in Neigh(kv.Key))
            {
                if (!cellToSector.TryGetValue(n, out int other)) continue;
                if (other != kv.Value)
                {
                    neighborSets[kv.Value].Add(other);
                    neighborSets[other].Add(kv.Value);
                }
            }
        }

        // 3) Заполняем edges базовыми значениями
        for (int i = 0; i < sectors.Count; i++)
        {
            sectors[i].edges.Clear();
            foreach (var nb in neighborSets[i])
                sectors[i].edges.Add(new EdgeData { toSectorId = nb, weight = 1f });
        }

        // 4) Сохраняем в MapAsset
        targetAsset.sectors = sectors;
        EditorUtility.SetDirty(targetAsset);
        AssetDatabase.SaveAssets();

        Debug.Log($"Bake OK: sectors = {sectors.Count}");
        // --- локальные функции --- //
        Vector3 ComputeCenterWorld(List<Vector3Int> cells)
        {
            if (cells.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var c in cells) sum += sourceTilemap.GetCellCenterWorld(c);
            return sum / cells.Count;
        }

        void FloodFillByTile(Vector3Int start, TileBase tile, SectorData sector,
                             HashSet<Vector3Int> vis, Dictionary<Vector3Int, int> index)
        {
            var q = new Queue<Vector3Int>();
            q.Enqueue(start); vis.Add(start);

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                index[p] = sector.id;
                sector.cells.Add(p);

                var neighs = Neigh(p);
                foreach (var n in neighs)
                {
                    if (!vis.Contains(n) && sourceTilemap.GetTile(n) == tile)
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
