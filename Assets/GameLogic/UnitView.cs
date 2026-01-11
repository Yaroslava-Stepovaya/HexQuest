
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

    public bool IsMoving => moveSeq != null && moveSeq.IsActive() && moveSeq.IsPlaying();

    public void MoveOneStepTo(Sector targetSector, Vector3 targetWorldPos)
    {
        if (BoundUnit == null || targetSector == null) return;

        // убить предыдущую анимацию, если была
        moveSeq?.Kill();
        moveSeq = DOTween.Sequence();

        Vector3 from = transform.position;
        Vector3 to = targetWorldPos;

        float dist = Vector3.Distance(from, to);
        float speed = Mathf.Max(0.0001f, BoundUnit.MoveSpeed);
        float dur = Mathf.Clamp(dist / speed, minStepDuration, maxStepDuration);

        Sector stepSector = targetSector;

        moveSeq
            .Append(transform.DOMove(to, dur).SetEase(ease))
            .AppendCallback(() =>
            {
                BoundUnit.CurrentSector = stepSector;
                GameEvents.ArrivedAtSector?.Invoke(BoundUnit, stepSector.Id);
            })
            .OnComplete(() =>
            {
                // без снапа в центр!
                // (можно сделать SnapToAssignedSlot позже, если захочешь)
            });
    }
    public void SnapToCurrentSector()
    {
        if (BoundUnit?.CurrentSector == null) return;
        transform.position = BoundUnit.CurrentSector.CenterWorld;
    }

    /// <summary>
    /// Пошаговое перемещение по списку секторов (центроидам).
    /// Длительность шага = дистанция / BoundUnit.MoveSpeed, зажимаем min/max.
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

            // локальная копия для замыкания
            Sector stepSector = s;

            moveSeq
                .Append(transform.DOMove(to, dur).SetEase(ease))
                .AppendCallback(() =>
                {
                    // обновляем модель
                    BoundUnit.CurrentSector = stepSector;

                    // шлём ивент
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
