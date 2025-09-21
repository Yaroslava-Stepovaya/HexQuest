using UnityEngine;

public class HeroUnit : Unit
{
    public HeroUnit(int id, string name, Sector startSector, int startHP, float moveSpeed = 1) : base(id, name, startSector, startHP, moveSpeed)
    {
    }

    
}
