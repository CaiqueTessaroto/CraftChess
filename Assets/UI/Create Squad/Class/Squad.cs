using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Squad
{
    public string Name;
    public int Power;
    public bool NativeSquad = false;
    public King King;
    public List<SquadPieceData> Pieces = new List<SquadPieceData>();
    public List<UnitPieceData> Units = new List<UnitPieceData>();
}

[System.Serializable]
public class King
{
    public string Name = "";
    public Vector2Int Position;
}

[System.Serializable]
public class SquadPieceData
{
    public string NameInSquad;
    public string Name;
    public string Squad;
    public string Sprite;
    public bool NativePiece = false;

}

[System.Serializable]
public class UnitPieceData
{
    public string Name;
    public int Power;
    public Vector2Int Position;

    public UnitPieceData(string name, int power, Vector2Int pos)
    {
        this.Name = name;
        this.Power = power;
        this.Position = pos;
    }
}