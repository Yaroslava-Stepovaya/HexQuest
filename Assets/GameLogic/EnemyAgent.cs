using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private GameManager _gameManager;

    // Ќа MVP можно держать тут константу, позже переедет в Enemy (VisionRange)
    [SerializeField] private int defaultVisionRange = 4;

    // unitId -> visionRange (если пока враги не Enemy, а Unit)
    private readonly Dictionary<int, int> _visionByEnemyId = new();

    private List<Unit> _enemies;
    private HeroUnit _hero;

    public void Init()
    {
        _enemies = _mapManager.EnemiesOnMap;   // логический список врагов
        _hero = _gameManager.Hero;            // HeroUnit : Unit

        _visionByEnemyId.Clear();
        foreach (var e in _enemies)
            _visionByEnemyId[e.Id] = defaultVisionRange;

        // подписка на событие прибыти€ (шаг завершЄн)
        GameEvents.ArrivedAtSector += OnArrivedAtSector;

        // стартова€ проверка: кто видит геро€ Ч тем дать первый шаг
        WakeEnemiesThatSeeHero();
    }

    private void OnDestroy()
    {
        GameEvents.ArrivedAtSector -= OnArrivedAtSector;
    }

    private void OnArrivedAtSector(Unit unit, int sectorId)


    {

        if (_gameManager.GameOver) return;
        if (unit == null) return;

        // √ерой пришЄл в новый сектор -> "разбудить" врагов, которые теперь могут видеть
        if (_hero != null && unit.Id == _hero.Id)
        {
            WakeEnemiesThatSeeHero();
            return;
        }

        // ¬раг пришЄл в новый сектор -> продолжить преследование (следующий шаг)
        if (IsEnemy(unit))
        {
            TryStepTowardsHero(unit);
        }
    }

    private bool IsEnemy(Unit unit)
    {
        // Ќа MVP: враг = тот, кто есть в списке EnemiesOnMap
        return _enemies != null && _enemies.Any(e => e.Id == unit.Id);
    }

    private void WakeEnemiesThatSeeHero()
    {
        if (_enemies == null || _hero == null) return;

        foreach (var e in _enemies)
        {
            if (e == null || !e.IsAlive) continue;
            TryStepTowardsHero(e);
        }
    }

    private void TryStepTowardsHero(Unit enemy)
    {
        if (_hero == null || enemy == null || !enemy.IsAlive) return;

        // правило: если враг сейчас движетс€ Ч не трогаем
        if (!_mapManager.UnitViewsById.TryGetValue(enemy.Id, out var view)) return;
        if (view.IsMoving) return;

        var start = enemy.CurrentSector;
        var goal = _hero.CurrentSector;
        if (start == null || goal == null) return;

        int vision = _visionByEnemyId.TryGetValue(enemy.Id, out var v) ? v : defaultVisionRange;

        // строим путь (BFS), с учЄтом замков/блоков
        if (!_mapManager.Pathfinder.TryFindPath(
                start.Id,
                goal.Id,
                out var pathIds,
                canTraverseEdge: (a, b) => !_mapManager.IsEdgeLocked(a, b) && !_mapManager.IsSectorBlocked(b)))
        {
            return;
        }

        // видимость по дистанции в графе
        int dist = pathIds.Count - 1;
        if (dist > vision) return; // не видит -> стоит

        // следующий шаг = path[1]
        if (pathIds.Count < 2) return;

        var nextSector = _mapManager.GetSectorByID(pathIds[1]);
        if (nextSector == null) return;

        // --- ¬ј∆Ќќ: учЄт слотов (зан€тость €чеек/позиции) ---
        // ќсвобождаем слот в текущем секторе (если был)
        if (enemy.CurrentSector != null)
            _mapManager.ReleaseSlot(enemy.CurrentSector, enemy.Id);

        // –езервируем слот в секторе назначени€ и получаем world-позицию
        Vector3 toPos = _mapManager.ReserveSlot(nextSector, enemy.Id);

        // ƒвигаем ровно на 1 шаг, до точки слота
        view.MoveOneStepTo(nextSector, toPos);
    }
}
