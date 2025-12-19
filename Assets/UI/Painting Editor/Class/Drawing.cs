using System.Collections.Generic;

[System.Serializable]
public class Drawing
{
    public List<PixelData> list;
    public int scale;
}

[System.Serializable]
public class PixelData
{
    public int x;
    public int y;
    public float r;
    public float g;
    public float b;
    public float a;
}

[System.Serializable]
public class Palette
{
    public List<ColorData> palette = new List<ColorData>();
}

[System.Serializable]
public class ColorData
{
    public float r;
    public float g;
    public float b;
    public float a;
}
