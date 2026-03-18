using UnityEngine;

[System.Serializable]
public struct DecryptionSymbol
{
    [Tooltip("Benzer sembolleri gruplamak için. Bağımsızlar için 0 kullanın.")]
    public int groupID;
    public Sprite icon;

    public override bool Equals(object obj)
    {
        if (!(obj is DecryptionSymbol)) return false;
        var other = (DecryptionSymbol)obj;

        return icon == other.icon;
    }

    public override int GetHashCode()
    {
        return icon != null ? icon.GetHashCode() : 0;
    }
}