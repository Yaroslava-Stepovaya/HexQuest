using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UnitView : MonoBehaviour
{
    [SerializeField]
    private GameObject unitMediaObject;

    public Unit BoundUnit { get; private set; }

    private Sequence moveSeq;

    [SerializeField] private float minStepDuration = 0.15f; // нижний предел длительности шага
    [SerializeField] private float maxStepDuration = 1.5f;  // верхний предел длительности шага
    [SerializeField] private Ease ease = Ease.Linear;

    public void Bind(Unit unit, bool snapToSector = true)
    {
        BoundUnit = unit;
        //if (snapToSector && unit?.CurrentSector != null)
           // SnapToCurrentSector();
    }

    public void SnapToCurrentSector()
    {
        if (BoundUnit?.CurrentSector == null) return;
        transform.position = BoundUnit.CurrentSector.CenterWorld;
    }

    public void MoveAlongPath(IList<Sector> path)
    {
        if (BoundUnit == null || path == null || path.Count == 0) return;

        // убить предыдущую анимацию, если была
        moveSeq?.Kill();
        moveSeq = DOTween.Sequence();

        Vector3 from = transform.position;

        for (int i = 0; i < path.Count; i++)
        {
            var s = path[i];
            if (s == null) continue;

            Vector3 to = s.CenterWorld;
            float dist = Vector3.Distance(from, to);
            float speed = Mathf.Max(0.0001f, BoundUnit.MoveSpeed);
            float dur = Mathf.Clamp(dist / speed, minStepDuration, maxStepDuration);


            BoundUnit.CurrentSector = path[i];//temporary
            moveSeq.Append(transform.DOMove(to, dur).SetEase(ease));
            from = to;
        }

        moveSeq.OnComplete(() =>
        {
            // По завершении пути можно уведомить внешний контроллер,
            // либо тут же снапнуть позицию (на всякий случай)
            SnapToCurrentSector();
        });
    }

}
