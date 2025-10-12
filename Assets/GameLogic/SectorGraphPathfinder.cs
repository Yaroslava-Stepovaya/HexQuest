using System;
using System.Collections.Generic;

public sealed class SectorGraphPathfinder
{
    private readonly Dictionary<int, Sector> _byId;
    private readonly bool _deterministicOrder;

    /// <param name="sectors">Глобальный список секторов из MapManager.Sectors</param>
    /// <param name="deterministicOrder">
    /// Если true — соседи будут обходиться в отсортированном порядке (стабильные пути при равнозначных альтернативах).
    /// </param>
    public SectorGraphPathfinder(IReadOnlyList<Sector> sectors, bool deterministicOrder = true)
    {
        if (sectors == null) throw new ArgumentNullException(nameof(sectors));

        _byId = new Dictionary<int, Sector>(sectors.Count);
        for (int i = 0; i < sectors.Count; i++)
        {
            var s = sectors[i];
            _byId[s.Id] = s;
        }
        _deterministicOrder = deterministicOrder;
    }

    /// <summary>
    /// Находит кратчайший путь по числу переходов (BFS). Возвращает true при успехе.
    /// </summary>
    public bool TryFindPath(
        int startId,
        int goalId,
        out List<int> path,
        Func<int, bool> canEnterSector = null,
        Func<int, int, bool> canTraverseEdge = null)
    {
        path = null;

        if (startId == goalId)
        {
            path = new List<int> { startId };
            return true;
        }

        if (!_byId.ContainsKey(startId) || !_byId.ContainsKey(goalId))
            return false;

        canEnterSector ??= static _ => true;
        canTraverseEdge ??= static (_, _) => true;

        var visited = new HashSet<int> { startId };
        var parent = new Dictionary<int, int>(64);
        var queue = new Queue<int>(64);

        queue.Enqueue(startId);

        // Временный буфер для детерминированного обхода
        List<int> nbBuf = _deterministicOrder ? new List<int>(8) : null;

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            var curSector = _byId[cur];

            // Получаем соседей
            if (_deterministicOrder)
            {
                nbBuf.Clear();
                foreach (var nb in curSector.Neighbors) nbBuf.Add(nb);
                nbBuf.Sort(); // сортировка по Id — дёшево и даёт стабильность
                for (int i = 0; i < nbBuf.Count; i++)
                {
                    int nb = nbBuf[i];
                    if (!ProcessNeighbor(cur, nb)) continue;
                    if (nb == goalId) { path = Reconstruct(parent, startId, goalId); return true; }
                    queue.Enqueue(nb);
                }
            }
            else
            {
                foreach (var nb in curSector.Neighbors)
                {
                    if (!ProcessNeighbor(cur, nb)) continue;
                    if (nb == goalId) { path = Reconstruct(parent, startId, goalId); return true; }
                    queue.Enqueue(nb);
                }
            }
        }

        return false;

        // Локальная функция — применяет все фильтры и помечает посещение
        bool ProcessNeighbor(int from, int nb)
        {
            if (visited.Contains(nb)) return false;
            if (!_byId.ContainsKey(nb)) return false;
            if (!canTraverseEdge(from, nb)) return false;
            if (!canEnterSector(nb)) return false;

            visited.Add(nb);
            parent[nb] = from;
            return true;
        }
    }

    /// <summary>
    /// Изокрона: все достижимые сектора за ≤ maxSteps. Возвращает dist (шаги) и parent для последующего восстановления маршрутов.
    /// </summary>
    public (Dictionary<int, int> dist, Dictionary<int, int> parent) BuildIsochrone(
        int startId,
        int maxSteps,
        Func<int, bool> canEnterSector = null,
        Func<int, int, bool> canTraverseEdge = null)
    {
        canEnterSector ??= static _ => true;
        canTraverseEdge ??= static (_, _) => true;

        var dist = new Dictionary<int, int>(128);
        var parent = new Dictionary<int, int>(128);
        var queue = new Queue<int>(128);

        if (!_byId.ContainsKey(startId))
            return (dist, parent);

        dist[startId] = 0;
        queue.Enqueue(startId);

        List<int> nbBuf = _deterministicOrder ? new List<int>(8) : null;

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            int d = dist[cur];
            if (d == maxSteps) continue;

            var curSector = _byId[cur];

            if (_deterministicOrder)
            {
                nbBuf.Clear();
                foreach (var nb in curSector.Neighbors) nbBuf.Add(nb);
                nbBuf.Sort();

                for (int i = 0; i < nbBuf.Count; i++)
                {
                    int nb = nbBuf[i];
                    if (dist.ContainsKey(nb)) continue;
                    if (!_byId.ContainsKey(nb)) continue;
                    if (!canTraverseEdge(cur, nb)) continue;
                    if (!canEnterSector(nb)) continue;

                    dist[nb] = d + 1;
                    parent[nb] = cur;
                    queue.Enqueue(nb);
                }
            }
            else
            {
                foreach (var nb in curSector.Neighbors)
                {
                    if (dist.ContainsKey(nb)) continue;
                    if (!_byId.ContainsKey(nb)) continue;
                    if (!canTraverseEdge(cur, nb)) continue;
                    if (!canEnterSector(nb)) continue;

                    dist[nb] = d + 1;
                    parent[nb] = cur;
                    queue.Enqueue(nb);
                }
            }
        }

        return (dist, parent);
    }

    private static List<int> Reconstruct(Dictionary<int, int> parent, int start, int goal)
    {
        var path = new List<int>();
        int cur = goal;
        while (cur != start)
        {
            path.Add(cur);
            cur = parent[cur];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }
}
