using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MapManager mapManager;   // ����� ���� �� �����
    [SerializeField] private UnitView heroViewPrefab; // ������ UnitView (� ����� SpriteRenderer)

    [Header("State")]
    public List<Unit> units = new();                 // ���������� �����
    public List<UnitView> unitViews = new();         // �� �������

    private HeroUnit hero;
    private UnitView heroView;

    private void Start()
    {
        // 1) ��������
        if (mapManager == null || mapManager.Sectors == null || mapManager.Sectors.Count == 0)
        {
            Debug.LogError("GameManager: MapManager �� �������� ��� ��� ��������.");
            return;
        }
        if (heroViewPrefab == null)
        {
            Debug.LogError("GameManager: �� �������� prefab UnitView.");
            return;
        }

        // 2) ���� ������ 0

        Sector startSector = mapManager.GetSectorByID(0);

        if (startSector == null)
        {
            Debug.LogError("GameManager: ��� ������� � id=0.");
            return;
        }
        //var startSector = mapManager.Sectors[0];

        // 3) ������ ����������� �����
        hero = new HeroUnit(
            id: 1,
            name: "Hero",
            startSector: startSector,
            startHP: 10,
            moveSpeed: 4f
        );
        units.Add(hero);

        // 4) ������� ������
        heroView = Instantiate(heroViewPrefab, startSector.CenterWorld, Quaternion.identity);
        heroView.name = "HeroView";
        heroView.Bind(hero, snapToSector: true);
        unitViews.Add(heroView);

//        List<Sector> path = new List<Sector>();

//        foreach (var sec in mapManager.Sectors)
//        {
//            if (sec.Id == 0)
//                continue;
//            path.Add(mapManager.GetSectorByID(sec.Id));
//        }
//        view.GetComponent<UnitView>().MoveAlongPath(path);
    }

    public void HandleSectorClick(int targetSectorId)
    {
        if (hero == null) return;
        int start = hero.CurrentSector.Id;
        int goal = targetSectorId;

        var pf = mapManager.Pathfinder;

        bool ok = pf.TryFindPath(
          start, goal, out var path,
          canEnterSector: id => !mapManager.IsSectorBlocked(id),
          canTraverseEdge: (a, b) => !mapManager.IsEdgeLocked(a, b)
      );

        if (ok) {

            List<Sector> sectorPath = new List<Sector>();

            foreach (int i in path)
                sectorPath.Add(mapManager.GetSectorByID(i));

            heroView.GetComponent<UnitView>().MoveAlongPath(sectorPath);

        }
    }

    
}
