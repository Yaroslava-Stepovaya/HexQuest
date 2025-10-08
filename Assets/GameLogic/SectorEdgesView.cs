using System.Collections.Generic;    // Коллекции: List, Dictionary
using System.Linq;                   // Для OrderBy, ToList
using TMPro;
using UnityEditor.Build;
using UnityEngine;

[DisallowMultipleComponent]          // Защита — не даёт повесить компонент несколько раз на один объект
public class SectorEdgesView : MonoBehaviour
{
    // === ССЫЛКИ НА ДАННЫЕ ===
    [Header("Data")]
    [SerializeField] private MapManager mapManager; // ссылка на объект, где хранится список всех секторов

    // === НАСТРОЙКИ ВНЕШНЕГО ВИДА ===
    [Header("Appearance")]
    [SerializeField] private Material lineMaterial;  // материал для линий (если null, создаём дефолтный)
    [SerializeField] private Color edgeColor = new Color(0.2f, 1f, 0.6f, 0.9f);     // обычный цвет ребра
    [SerializeField] private Color lockedColor = new Color(1f, 0.35f, 0.35f, 0.95f); // цвет заблокированного ребра
    [SerializeField, Range(0.01f, 1f)] private float edgeWidth = 0.2f;              // толщина линии в мировых единицах
    [SerializeField, Range(6, 48)] private int segments = 16;                       // количество сегментов для кривой Безье
    [SerializeField] private GameObject centroidPrefab;
    [SerializeField] private float centroidScale = 1.2f;

    [Header("Labels")]
    [SerializeField] private Color TextSectorIDColor = new Color(1.0f, 1.0f, 0.6f, 0.5f);
    [SerializeField] private float TextSectorIDSize = 7;
    [SerializeField] private int TextSectorIDNumberInLayer = 0;
    [SerializeField] private float labelRadius = 0.6f;       // смещение от центроида в мире
    [SerializeField] private float labelAngleDeg = 25f;      // угол смещения (чтобы не перекрывало дороги по оси)


    // === НАСТРОЙКИ ФОРМЫ КРИВОЙ ===
    [Header("Curve")]
    [SerializeField, Tooltip("Изгиб от середины, ~доля длины ребра")]
    private float bendFactor = 0.25f;   // коэффициент изгиба (насколько сильно линия отклоняется от прямой)
    [SerializeField, Tooltip("Однократный шум на контрольной точке (в долях длины ребра)")]
    private float noiseFactor = 0.15f;  // коэффициент случайного смещения контрольной точки
    [SerializeField] private int seed = 1337; // зерно случайного генератора (чтобы рисунок был стабильным)

    // === СОРТИРОВКА ОТНОСИТЕЛЬНО ДРУГИХ СПРАЙТОВ ===
    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default"; // имя sorting layer
    [SerializeField] private int sortingOrder = 20;               // порядок внутри слоя (чем больше, тем выше)

    // список всех созданных линий, чтобы потом их удалить при перестройке
    private readonly List<LineRenderer> _lines = new();

    // Unity вызывает Start() один раз при старте
    private void Start()
    {
        Rebuild(); // сразу строим линии при запуске
    }

    // Перестроить линии заново (например, после обновления карты)
    public void Rebuild()
    {
        ClearLines(); // удалить старые
        BuildLines(); // создать новые
    }

    // Удаляет все LineRenderer'ы, созданные ранее
    private void ClearLines()
    {
        foreach (var lr in _lines)
            if (lr) Destroy(lr.gameObject); // уничтожаем объекты с линиями
        _lines.Clear(); // очищаем список
    }

    // Основная функция: построение линий между секторами
    private void BuildLines()
    {
        // Проверяем наличие данных
        if (mapManager == null || mapManager.Sectors == null || mapManager.Sectors.Count == 0)
        {
            Debug.LogWarning("SectorEdgesView: MapManager/Sectors not set or empty.");
            return;
        }


        //spawn centroids
        SpawnNodes(mapManager.Sectors);


        // Создаём словарь id → Sector (потому что MapManager.Sectors — это список)
        var sectors = mapManager.Sectors.ToList();

        // Если материал не задан — создаём стандартный, который поддерживает цвет
        Material mat = lineMaterial != null
            ? lineMaterial
            : new Material(Shader.Find("Sprites/Default"));

        // Псевдослучайный генератор с фиксированным seed для стабильного рисунка
        var rng = new System.Random(seed);

        // Проходим по всем секторам, сортируя по Id для стабильного порядка
        foreach (var a in sectors.OrderBy(s => s.Id))
        {
            // Для каждого соседа a (по id)
            foreach (var bId in a.Neighbors)
            {
                // чтобы не рисовать дважды одно и то же ребро, рисуем только если a.Id < bId
                if (bId <= a.Id) continue;


                var b = mapManager.GetSectorByID(bId);

                if (b == null)
                    continue;

                // Проверяем параметры ребра: заблокировано ли оно, какой у него вес
                bool locked = false;
                float weight = 1f;
                if (a.TryGetEdge(bId, out var e))
                {
                    locked = e.Locked;
                    weight = Mathf.Max(0.0001f, e.Weight); // защита от деления на 0
                }

                // Берём координаты центроидов в 2D
                Vector2 p0 = (Vector2)a.CenterWorld;
                Vector2 p3 = (Vector2)b.CenterWorld;

                // Разность между ними → направление
                Vector2 d = p3 - p0;
                float len = d.magnitude;
                if (len < 0.0001f) continue; // слишком близко → пропускаем

                // Нормализуем вектор (единичная длина)
                Vector2 dir = d / len;

                // 2D-перпендикуляр (поворот на 90 градусов)
                Vector2 perp = new Vector2(-dir.y, dir.x);

                // Середина линии
                Vector2 mid = (p0 + p3) * 0.5f;

                // Вычисляем изгиб (насколько сильно отогнём линию)
                float bend = bendFactor * len * 0.5f;

                // Контрольная точка Безье — середина + отклонение по перпендикуляру
                Vector2 c = mid + perp * bend;

                // Однократный шум: добавляем немного хаоса в контрольную точку
                float r1 = (float)rng.NextDouble() * 2f - 1f; // [-1..1]
                float r2 = (float)rng.NextDouble() * 2f - 1f; // [-1..1]
                float noiseAmp = noiseFactor * len;
                // Смещаем контрольную точку слегка по перпу и чуть по направлению
                c += perp * (r1 * noiseAmp) + dir * (r2 * noiseAmp * 0.5f);

                // === СОЗДАЁМ ОБЪЕКТ ДЛЯ ЛИНИИ ===
                var go = new GameObject($"Edge_{a.Id}_{bId}");
                go.transform.SetParent(transform, false); // привязываем к этому объекту
                var lr = go.AddComponent<LineRenderer>(); // добавляем LineRenderer
                _lines.Add(lr);                           // сохраняем в список

                // === НАСТРОЙКА ЛИНИИ ===
                lr.useWorldSpace = true;        // мировые координаты (а не локальные)
                lr.alignment = LineAlignment.View; // всегда «повёрнут» к камере
                lr.numCapVertices = 12;         // скругляем концы линии
                lr.numCornerVertices = 6;       // сглаживаем углы
                lr.material = mat;              // общий материал

                // Цвет линии (по состоянию locked / normal)
                var col = locked ? lockedColor : edgeColor;
                lr.startColor = lr.endColor = col;

                // Толщина (немного зависит от веса)
                lr.widthMultiplier = 1f;
                lr.startWidth = lr.endWidth = edgeWidth * Mathf.Clamp01(0.7f + 0.3f / weight);

                // Слой отрисовки
                lr.sortingLayerName = sortingLayerName;
                lr.sortingOrder = sortingOrder;

                // === ЗАПОЛНЯЕМ ТОЧКИ КРИВОЙ ===
                lr.positionCount = segments + 1; // сколько точек будет на линии

                // Проходим t от 0 до 1 и вычисляем точки по формуле квадратичной Безье
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;              // доля пути [0..1]
                    Vector2 p = BezierQuadratic(p0, c, p3, t);  // вычисляем точку на кривой
                    lr.SetPosition(i, new Vector3(p.x, p.y, 0f)); // записываем в LineRenderer
                }
            }
        }
    }

    // === ВСПОМОГАТЕЛЬНАЯ ФУНКЦИЯ ===
    // Квадратичная кривая Безье: p(t) = (1−t)^2 * p0 + 2(1−t)t * c + t^2 * p3
    private static Vector2 BezierQuadratic(Vector2 p0, Vector2 c, Vector2 p3, float t)
    {
        float u = 1f - t;
        return (u * u) * p0 + 2f * u * t * c + (t * t) * p3;
    }

    private void SpawnNodes(IEnumerable<Sector> sectors)
    {
        foreach (var s in sectors)
        {
            var node = Instantiate(centroidPrefab, s.CenterWorld, Quaternion.identity, transform);
            node.transform.localScale = Vector3.one * centroidScale;

            CreateLabel(node,s.Id);

        }
    }
    private TextMeshPro CreateLabel(GameObject centroid, int id)
    {
        var go = new GameObject("SectorID");
        go.transform.SetParent(centroid.transform, false);

        // --- позиция: слегка в стороне от точки центроида ---
        float ang = labelAngleDeg * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * labelRadius;
        go.transform.localPosition = offset; // <- ключевая строка

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = TextSectorIDSize;
        tmp.color = TextSectorIDColor;
        tmp.text = id.ToString();

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = TextSectorIDNumberInLayer;

        // Материалом можно добавить обводку, чтобы текст читался на любой подложке:
        // var mat = tmp.fontMaterial;
        // mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
        // mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

        return tmp;
    }

    //private TextMeshPro CreateLabel(GameObject centroid, int id)
    //{
    //    var go = new GameObject("SectorID");
    //    go.transform.SetParent(centroid.transform, false);

    //    var tmp = go.AddComponent<TextMeshPro>();
    //    //tmp.font = _font;
    //    tmp.alignment = TextAlignmentOptions.Center;
    //    //tmp.enableWordWrapping = false;
    //    tmp.fontSize = TextSectorIDSize;
    //    tmp.color = TextSectorIDColor;
    //    tmp.text = id.ToString();

    //    // Рендер-приоритет
    //    var mr = go.GetComponent<MeshRenderer>();
    //    if (mr != null)
    //    {
    //        //mr.sortingLayerName = _sortingLayerName;
    //        mr.sortingOrder = TextSectorIDNumberInLayer;
    //    }

    //    return tmp;
    //}

}

