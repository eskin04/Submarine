
// Dalga konfigürasyonu
[System.Serializable]
public struct WaveConfig
{
    public int amplitude; // 1-6
    public int frequency; // 1-6
    public int phase;     // 1-3

    public bool Equals(WaveConfig other) =>
        amplitude == other.amplitude && frequency == other.frequency && phase == other.phase;
}

// Mühendisin ekranında gösterilecek denklem verisi
[System.Serializable]
public struct EquationData
{
    public string displayString; // Örn: "X = S4 + 2"
    public int targetAnswer;     // Bu denklemin doğru cevabı (örn: 7)
}

// Her bir kanalın (CH1, CH2, CH3) tüm verilerini tutan paket
[System.Serializable]
public struct ChannelData
{
    public WaveConfig targetWave;
    public int symbolID;         // UI'da gösterilecek sembolün index'i (0-9)
    public EquationData equation;
}