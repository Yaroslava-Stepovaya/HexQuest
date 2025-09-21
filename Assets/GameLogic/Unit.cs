public class Unit
{
    // === Public Properties ===
    public int Id { get; }                 // ���������� ID (������ ������)
    public string Name { get; set; }       // ���
    public Sector CurrentSector { get; set; } // ������, ��� ���� ������
    public float MoveSpeed { get; set; }   // �������� ��������
    public bool IsAlive { get; private set; } = true; // ��� �� ����

    // === Private Fields ===
    private bool isMoving;   // ���������� ���� ��������
    private int hitPoints;   // ��������

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
        // TODO: ����� ������� (������ ��������)
    }

    public void StopMove()
    {
        // TODO: ��������� ��������
    }

    public void TakeDamage(int amount)
    {
        // TODO: ���������� HP
    }

    public void Die()
    {
        // TODO: ������
    }

    // === Protected / Virtual Methods (��� �����������) ===
    //protected virtual void OnArrived(Sector sector)
    //{
        // TODO: ������� �� ��������
    //}
}
