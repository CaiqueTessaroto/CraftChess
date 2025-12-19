using System;
using System.Collections.Generic;
using UnityEngine;

public class PieceComponent : MonoBehaviour
{
    public Player Player;
    public string Name;
    public int Power = 0;
    public string Squad;
    public Vector2Int Position;
    public bool HasMoved;
    public bool InitialMoved;
    public bool IsPromoted;
    public bool IsKing;

    public List<Vector2Int> PossibleMoves = new List<Vector2Int>();

    public void Initialize(string squad, string name, int power, Player player, Vector2Int position, bool isKing)
    {
        this.Squad = squad;
        this.Name = name;
        this.Player = player;
        this.Power = power;
        this.Position = position;
        this.HasMoved = false;
        this.InitialMoved = false;
        this.IsPromoted = false;
        this.IsKing = isKing;
        this.PossibleMoves = new List<Vector2Int>();
    }

    public void SetGridPosition(int x, int y)
    {
        Position = new Vector2Int(x, y);
    }

    public GameObject GetGameObject()
    {
        return this.gameObject;
    }
}
