using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class MapRuntimeSaver
{
    /// <summary>
    /// �������������� ���������� mapAsset �������� �������-���������.
    /// </summary>
    public static void SaveRuntimeToAsset(MapAsset mapAsset, IReadOnlyList<Sector> runtimeSectors)
    {
        if (mapAsset == null || runtimeSectors == null)
        {
            Debug.LogError("SaveRuntimeToAsset: mapAsset/runtimeSectors is null.");
            return;
        }

        mapAsset.sectors.Clear();

        foreach (var s in runtimeSectors)
        {
            // �������� SectorData
            var sd = new SectorData
            {
                id = s.Id,
                centerWorld = s.CenterWorld,
                cells = s.Cells?.ToList() ?? new List<Vector3Int>(),
                edges = new List<EdgeData>()
            };

            // �������� ���� (�� ������ _edges �������)
            foreach (var neighborId in s.Neighbors)
            {
                if (s.TryGetEdge(neighborId, out var e))
                {
                    sd.edges.Add(new EdgeData
                    {
                        toSectorId = e.To,
                        weight = e.Weight,
                        locked = e.Locked,
                        requiredKeyId = e.RequiredKeyId
                    });
                }
            }

            mapAsset.sectors.Add(sd);
        }

        // ��������� asset � ���������
#if UNITY_EDITOR
        EditorUtility.SetDirty(mapAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"Map saved: {mapAsset.name}. Sectors: {mapAsset.sectors.Count}");
#else
        Debug.Log("MapRuntimeSaver: ScriptableObject updated in memory (runtime). Persisting to disk works only in Editor.");
#endif
    }
}
