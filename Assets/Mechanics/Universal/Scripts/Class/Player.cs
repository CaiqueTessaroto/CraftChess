using UnityEngine;

// Classe para representar um jogador
[System.Serializable]
public class Player
{
    public string name;
    public int id;
    public Color color; // Cor do jogador

    public Player(string name, int id, Color color)
    {
        this.name = name;
        this.id = id;
        this.color = color;
    }


}