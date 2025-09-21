using System.Collections.Generic;
using System.Linq.Expressions;
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
        var view = Instantiate(heroViewPrefab, startSector.CenterWorld, Quaternion.identity);
        view.name = "HeroView";
        view.Bind(hero, snapToSector: true);
        unitViews.Add(view);

        List<Sector> path = new List<Sector>();

        foreach (var sec in mapManager.Sectors)
        {
            if (sec.Id == 0)
                continue;
            path.Add(mapManager.GetSectorByID(sec.Id));
        }
        view.GetComponent<UnitView>().MoveAlongPath(path);
    }
}
