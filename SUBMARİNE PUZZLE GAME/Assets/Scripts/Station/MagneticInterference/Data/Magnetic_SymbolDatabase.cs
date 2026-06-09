using UnityEngine;

[CreateAssetMenu(fileName = "MagneticSymbolDatabase", menuName = "Magnetic/Symbol Database")]
public class Magnetic_SymbolDatabase : ScriptableObject
{
    [Tooltip("0'dan 9'a kadar olan sembol spritelarını buraya sırasıyla ekleyin.")]
    public Sprite[] symbolSprites = new Sprite[10];

    public Sprite GetSymbol(int id)
    {
        if (id >= 0 && id < symbolSprites.Length)
        {
            return symbolSprites[id];
        }
        return null;
    }
}