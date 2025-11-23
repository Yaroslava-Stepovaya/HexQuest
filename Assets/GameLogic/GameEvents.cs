using System;
using UnityEngine;

public static class GameEvents
{
    public static Action RebuildEdges;

    public static Action<Unit, int> ArrivedAtSector;

    public static Action<MapKeyData> SwitchKey;
}
