
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    [SerializeField] private GameObject unitMediaObject;
    [SerializeField] private float minStepDuration = 0.15f;
    [SerializeField] private float maxStepDuration = 1.5f;
    [SerializeField] private Ease ease = Ease.Linear;

    public Unit BoundUnit { get; private set; }

    private Sequence moveSeq;

    public void Bind(Unit unit, bool snapToSector = true)
    {
        BoundUnit = unit;

        if (snapToSector && unit?.CurrentSector != null)
            SnapToCurrentSector();
    }

    public void SnapToCurrentSector()
    {
        if (BoundUnit?.CurrentSector == null) return;
        transform.position = BoundUnit.CurrentSector.CenterWorld;
    }

    /// <summary>
    /// ѕошаговое перемещение по списку секторов (центроидам).
    /// ƒлительность шага = дистанци€ / BoundUnit.MoveSpeed, зажимаем min/max.
    /// </summary>
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

            // локальна€ копи€ дл€ замыкани€
            Sector stepSector = s;

            moveSeq
                .Append(transform.DOMove(to, dur).SetEase(ease))
                .AppendCallback(() =>
                {
                    // обновл€ем модель
                    BoundUnit.CurrentSector = stepSector;

                    // шлЄм ивент
                    GameEvents.ArrivedAtSector?.Invoke(BoundUnit, stepSector.Id);
                });

            from = to;
        }

        moveSeq.OnComplete(() =>
        {
            // подстраховочный снап, если нужно
            SnapToCurrentSector();
        });
    }
}
