using UnityEngine;
using System.Collections.Generic;

public static class Magnetic_EquationGenerator
{
    // İşlem Tipleri (SubReverse: X - S yerine S - X durumunu temsil eder)
    public enum MathOp { Add, Sub, SubReverse, Mul, Div }

    // Dışarıya 3 kanalın denklem verisini ve bulunan X, Y, Z rakamlarını döner
    public static void GenerateEquations(
        int s1_id, int s1_val,
        int s2_id, int s2_val,
        int s3_id, int s3_val,
        out EquationData eq1, out EquationData eq2, out EquationData eq3,
        out int varX, out int varY, out int varZ)
    {
        // ==========================================
        // KANAL 1 (CH1) - X Değişkeni
        // ==========================================
        // X değişkeni 0-9 arası seçilir[cite: 105, 106, 113].
        varX = Random.Range(0, 10);

        // CH1 için C'nin tam sayı olmasını sağlayacak rastgele bir işlem seçilir[cite: 113].
        MathOp op1 = GetValidOperationForCH1(varX, s1_val);
        int constC = CalculateResult(varX, s1_val, op1);
        string eq1Str = ApplyFormatting("C", "X", $"S{s1_id}", constC.ToString(), op1);
        eq1 = new EquationData { displayString = eq1Str, targetAnswer = varX };

        // ==========================================
        // KANAL 2 (CH2) - Y Değişkeni
        // ==========================================
        // Y'nin her zaman bir rakam olmasını sağlayacak (X ve S2 üzerinden) güvenli işlem havuzu oluşturulur[cite: 114].
        MathOp op2 = GetValidOperationForDigit(varX, s2_val);
        varY = CalculateResult(varX, s2_val, op2);
        string eq2Str = ApplyFormatting("Y", "X", $"S{s2_id}", varY.ToString(), op2);
        eq2 = new EquationData { displayString = eq2Str, targetAnswer = varY };

        // ==========================================
        // KANAL 3 (CH3) - Z Değişkeni
        // ==========================================
        // CH3 için %50 şansla R değişkeni X veya Y olarak seçilir[cite: 122].
        bool useXForR = Random.value > 0.5f;
        int rVal = useXForR ? varX : varY;
        string rStr = useXForR ? "X" : "Y";

        // Z'nin rakam olmasını sağlayacak güvenli işlem havuzu oluşturulur[cite: 123].
        MathOp op3 = GetValidOperationForDigit(rVal, s3_val);
        varZ = CalculateResult(rVal, s3_val, op3);
        string eq3Str = ApplyFormatting("Z", rStr, $"S{s3_id}", varZ.ToString(), op3);
        eq3 = new EquationData { displayString = eq3Str, targetAnswer = varZ };
    }

    // CH1 için işlem doğrulaması (Sadece C'nin tam sayı çıkması yeterlidir) [cite: 113]
    private static MathOp GetValidOperationForCH1(int x, int s)
    {
        List<MathOp> validOps = new List<MathOp> { MathOp.Add, MathOp.Sub, MathOp.Mul };
        if (s != 0 && x % s == 0) validOps.Add(MathOp.Div);
        return validOps[Random.Range(0, validOps.Count)];
    }

    // PDF 6. Sayfa kurallarına göre sonucun her zaman 0-9 arası çıkmasını garantileyen filtreleme
    private static MathOp GetValidOperationForDigit(int leftOperand, int rightOperand)
    {
        List<MathOp> validOps = new List<MathOp>();

        if (leftOperand + rightOperand <= 9) validOps.Add(MathOp.Add); // Toplama kuralı[cite: 115, 124].
        if (leftOperand - rightOperand >= 0) validOps.Add(MathOp.Sub); // Çıkarma kuralı 1[cite: 116, 125].
        if (rightOperand - leftOperand >= 0) validOps.Add(MathOp.SubReverse); // Çıkarma kuralı 2[cite: 117, 126].
        if (leftOperand * rightOperand <= 9) validOps.Add(MathOp.Mul); // Çarpma kuralı[cite: 118, 127].
        if (rightOperand != 0 && leftOperand % rightOperand == 0) validOps.Add(MathOp.Div); // Bölme kuralı[cite: 119, 128].

        // Havuzdan rastgele geçerli bir işlem seçilir
        return validOps[Random.Range(0, validOps.Count)];
    }

    private static int CalculateResult(int left, int right, MathOp op)
    {
        switch (op)
        {
            case MathOp.Add: return left + right;
            case MathOp.Sub: return left - right;
            case MathOp.SubReverse: return right - left;
            case MathOp.Mul: return left * right;
            case MathOp.Div: return left / right;
            default: return 0;
        }
    }

    // Denklem metinlerini PDF'teki Format 1, Format 2 ve Format 3 kurallarına göre görselleştirir
    private static string ApplyFormatting(string targetVarStr, string var1Str, string var2Str, string evaluatedResult, MathOp op)
    {
        int formatType = Random.Range(1, 4); // 1, 2 veya 3 seçilir[cite: 129].

        string opStr = "";
        switch (op)
        {
            case MathOp.Add: opStr = "+"; break;
            case MathOp.Sub: opStr = "-"; break;
            case MathOp.SubReverse:
                opStr = "-";
                // SubReverse (S - X) durumu için ekrandaki değişkenlerin yerini değiştiriyoruz
                string temp = var1Str; var1Str = var2Str; var2Str = temp;
                break;
            case MathOp.Mul: opStr = "*"; break;
            case MathOp.Div: opStr = "/"; break;
        }

        // Sabit sayı (C) gelmişse string'e yediriyoruz
        if (targetVarStr == "C") targetVarStr = evaluatedResult;

        if (formatType == 1)
        {
            // Format 1: [Sonuç] = [Değişken 1] (İşlem) [Değişken 2] [cite: 130]
            return $"{targetVarStr} = {var1Str} {opStr} {var2Str}";
        }
        else if (formatType == 2)
        {
            // Format 2: [Değişken 1] (İşlem) [Değişken 2] = [Sonuç] [cite: 131]
            return $"{var1Str} {opStr} {var2Str} = {targetVarStr}";
        }
        else
        {
            // Format 3 (Ters İşlem Gösterimi): Toplama çıkarma gibi, çarpma bölme gibi gösterilir[cite: 131, 132].
            if (op == MathOp.Add) return $"{var1Str} = {targetVarStr} - {var2Str}"; // Gerçek: Y = X + S -> Gösterilen: X = Y - S
            if (op == MathOp.Sub) return $"{var1Str} = {targetVarStr} + {var2Str}"; // Gerçek: Y = X - S -> Gösterilen: X = Y + S
            if (op == MathOp.SubReverse) return $"{var1Str} = {var2Str} - {targetVarStr}"; // Gerçek: Y = S - X -> Gösterilen: X = S - Y
            if (op == MathOp.Mul) return $"{var1Str} = {targetVarStr} / {var2Str}";
            if (op == MathOp.Div) return $"{var1Str} = {targetVarStr} * {var2Str}";

            return $"{targetVarStr} = {var1Str} {opStr} {var2Str}"; // Fallback
        }
    }
}