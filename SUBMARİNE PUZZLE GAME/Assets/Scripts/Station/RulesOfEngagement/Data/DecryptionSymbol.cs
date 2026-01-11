using UnityEngine;

[System.Serializable]
public struct DecryptionSymbol
{
    public enum Shape { Square, Circle, Diamond, Triangle }
    public enum Color { Red, Green, Blue }

    public Shape shape;
    public Color color;
    public Sprite icon;

    public override bool Equals(object obj)
    {
        if (!(obj is DecryptionSymbol)) return false;
        var other = (DecryptionSymbol)obj;
        return shape == other.shape && color == other.color;
    }

    public override int GetHashCode()
    {
        return (shape, color).GetHashCode();
    }
}