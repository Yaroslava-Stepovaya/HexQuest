using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SectorData
{
    public int id;
    public Color displayColor;                 // ����� ������ (��� ���������/���������)
    public List<Vector3Int> cells = new();     // ���������� ��������
    public Vector3 centerWorld;                // ������������ ��������
    public List<EdgeData> edges = new();       // ����� ���� (������ + ����/�����)
}

[Serializable]
public class EdgeData
{
    public int toSectorId;
    public float weight = 1f;                  // ��������� �������� (�� ��������� 1)
    public bool locked = false;                // ������ �������?
    public string requiredKeyId;               // �� ����� (����� �������� ������ �� ������)
}