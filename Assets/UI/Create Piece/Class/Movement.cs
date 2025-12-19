using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class MovementConfigData
{
    public PieceInfo piece;
    public Movement straight;
    public Movement diagonal;
    public PersonalizedMove custom;
    public Special special;
    public Promotion promotion;
}

[System.Serializable]
public class PieceInfo
{
    public string Name = "";
    public string Squad = "";
    public int Power = 0;
    public string Art = "";
    public string FolderSprite = "";
    public bool NativeSprite = false;
}

[System.Serializable]
public class Movement
{

    [Header("Configuração de ativação:")]
    public bool Active = false;

    [Header("Configuração de Direção:")]
    public bool All = true;
    public bool Front = false;
    public bool Back = false;
    public bool Right = false;
    public bool Left = false;

    [Header("Configuração de Range:")]
    public int Range = 7;

    [Header("Configurações de Movimento:")]
    public bool Move = true;
    public bool Capture = false;
    public bool Jump = false;
}

[System.Serializable]
public class PersonalizedMove
{
    [Header("Configuração de ativação:")]
    public bool Active = false;

    [Header("Configurações de Movimento:")]
    public bool Move = true;
    public bool Capture = false;
    public bool Jump = false;

    [Header("Lista de Movimentos:")]
    public List<MoveData> Moves;

}

[System.Serializable]
public class Special
{
    [Header("Configuração de ativação:")]
    public bool Active = false;

    [Header("Configuração Adicionais:")]
    public bool Move = true;
    public bool Capture = false;
    public bool Jump = false;
    public bool Castling = false;

    [Header("Lista de Movimentos:")]
    public List<MoveData> Moves;

    [Header("Configuração de Peças:")]
    public List<string> Pieces = new List<string>();
}

[System.Serializable]
public class Promotion
{
    public bool Active = false;

    [Header("Configuração de Peças:")]
    public List<string> Pieces = new List<string>();
}