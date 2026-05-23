using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Squad
{
    public string Name;
    public int Power;
    public bool Balanced = true;
    public bool Translate = true;
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
    public int Power;
    public string Sprite;
    public string SpriteSet;
    public bool Translate = false;
    public List<string> PromotionPieces = new List<string>();
    public List<string> CastlingPieces = new List<string>();
}

[System.Serializable]
public class UnitPieceData
{
    public string Name;
    public Vector2Int Position;
    public UnitPieceData(string name, Vector2Int pos)
    {
        this.Name = name;
        this.Position = pos;
    }
}