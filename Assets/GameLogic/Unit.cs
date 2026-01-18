using System.Collections.Generic;

public class Unit
{
    // === Public Properties ===
    public int Id { get; }                 // уникальный ID (только чтение)
    public string Name { get; set; }       // имя
    public Sector CurrentSector { get; set; } // сектор, где юнит сейчас
    public float MoveSpeed { get; set; }   // скорость движения
    public bool IsAlive { get; set; } = true; // жив ли юнит

    

    

    // === Private Fields ===
    private bool isMoving;   // внутренний флаг движения
    private int hitPoints;   // здоровье
    public int HP => hitPoints;

    // === Constructor ===
    public Unit(int id, string name, Sector startSector, int startHP, float moveSpeed = 1f)
    {
        Id = id;
        Name = name;
        CurrentSector = startSector;
        hitPoints = startHP;
        MoveSpeed = moveSpeed;
    }

    // === Public Methods ===
    public void MoveTo(Sector target)
    {
        // TODO: смена сектора (логика движения)
    }

    public void StopMove()
    {
        // TODO: остановка движения
    }

    public void TakeDamage(int amount)
    {
        // TODO: уменьшение HP
        hitPoints -= amount;
    }

    public void Die()
    {
        // TODO: смерть
    }

    // === Protected / Virtual Methods (для наследников) ===
    //protected virtual void OnArrived(Sector sector)
    //{
    // TODO: событие по прибытии
    //}

    
    
}
