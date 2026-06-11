using UnityEngine;
using System.Collections.Generic;

public static class Magnetic_EquationGenerator
{
    public enum MathOp { Add, Sub, SubReverse, Mul, Div }

    public static void GenerateEquations(
        int s1_id, int s1_val,
        int s2_id, int s2_val,
        int s3_id, int s3_val,
        out EquationData eq1, out EquationData eq2, out EquationData eq3,
        out int varX, out int varY, out int varZ)
    {
        int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // ==========================================
            // KANAL 1 (CH1) - X Değişkeni
            // ==========================================
            int tempX = Random.Range(0, 10);

            // ==========================================
            // KANAL 2 (CH2) - Y Değişkeni
            // ==========================================
            List<MathOp> validOpsY = GetValidOperationsForDigit(tempX, s2_val);
            validOpsY.RemoveAll(op => CalculateResult(tempX, s2_val, op) == tempX);

            if (validOpsY.Count == 0) continue;

            MathOp op2 = validOpsY[Random.Range(0, validOpsY.Count)];
            int tempY = CalculateResult(tempX, s2_val, op2);

            // ==========================================
            // KANAL 3 (CH3) - Z Değişkeni
            // ==========================================
            bool useXForR = Random.value > 0.5f;
            int rVal = useXForR ? tempX : tempY;
            string rStr = useXForR ? "X" : "Y";

            List<MathOp> validOpsZ = GetValidOperationsForDigit(rVal, s3_val);
            validOpsZ.RemoveAll(op => CalculateResult(rVal, s3_val, op) == tempX || CalculateResult(rVal, s3_val, op) == tempY);

            if (validOpsZ.Count == 0) continue;

            MathOp op3 = validOpsZ[Random.Range(0, validOpsZ.Count)];
            int tempZ = CalculateResult(rVal, s3_val, op3);

            // ==========================================
            // BAŞARILI DURUM: Değerleri ata ve çık
            // ==========================================
            varX = tempX;
            varY = tempY;
            varZ = tempZ;

            MathOp op1 = GetValidOperationForCH1(varX, s1_val);
            int constC = CalculateResult(varX, s1_val, op1);

            eq1 = new EquationData
            {
                displayString = ApplyFormatting("C", "X", $"S{s1_id}", s1_val, constC.ToString(), op1),
                targetAnswer = varX
            };

            eq2 = new EquationData
            {
                displayString = ApplyFormatting("Y", "X", $"S{s2_id}", s2_val, varY.ToString(), op2),
                targetAnswer = varY
            };

            eq3 = new EquationData
            {
                displayString = ApplyFormatting("Z", rStr, $"S{s3_id}", s3_val, varZ.ToString(), op3),
                targetAnswer = varZ
            };

            return;
        }

        // Kilitlenme durumunda yedek algoritma
        GenerateEquationsUnsafe(s1_id, s1_val, s2_id, s2_val, s3_id, s3_val, out eq1, out eq2, out eq3, out varX, out varY, out varZ);
    }

    private static void GenerateEquationsUnsafe(
        int s1_id, int s1_val, int s2_id, int s2_val, int s3_id, int s3_val,
        out EquationData eq1, out EquationData eq2, out EquationData eq3,
        out int varX, out int varY, out int varZ)
    {
        varX = Random.Range(0, 10);

        MathOp op1 = GetValidOperationForCH1(varX, s1_val);
        int constC = CalculateResult(varX, s1_val, op1);
        eq1 = new EquationData { displayString = ApplyFormatting("C", "X", $"S{s1_id}", s1_val, constC.ToString(), op1), targetAnswer = varX };

        List<MathOp> opsY = GetValidOperationsForDigit(varX, s2_val);
        MathOp op2 = opsY[Random.Range(0, opsY.Count)];
        varY = CalculateResult(varX, s2_val, op2);
        eq2 = new EquationData { displayString = ApplyFormatting("Y", "X", $"S{s2_id}", s2_val, varY.ToString(), op2), targetAnswer = varY };

        bool useXForR = Random.value > 0.5f;
        int rVal = useXForR ? varX : varY;
        string rStr = useXForR ? "X" : "Y";

        List<MathOp> opsZ = GetValidOperationsForDigit(rVal, s3_val);
        MathOp op3 = opsZ[Random.Range(0, opsZ.Count)];
        varZ = CalculateResult(rVal, s3_val, op3);
        eq3 = new EquationData { displayString = ApplyFormatting("Z", rStr, $"S{s3_id}", s3_val, varZ.ToString(), op3), targetAnswer = varZ };
    }

    private static MathOp GetValidOperationForCH1(int x, int s)
    {
        List<MathOp> validOps = new List<MathOp> { MathOp.Add, MathOp.Sub };

        // KORUMA 1: Eğer sembol değeri 0 ise, çarpma işlemine ASLA izin verilmez.
        // Bu sayede "0 = X * 0" gibi X'in bilinemeyeceği çıkmaz sokaklar engellenir.
        if (s != 0) validOps.Add(MathOp.Mul);

        // Bölme işlemi için zaten s != 0 korumamız vardı.
        if (s != 0 && x % s == 0) validOps.Add(MathOp.Div);

        return validOps[Random.Range(0, validOps.Count)];
    }

    private static List<MathOp> GetValidOperationsForDigit(int leftOperand, int rightOperand)
    {
        List<MathOp> validOps = new List<MathOp>();

        if (leftOperand + rightOperand <= 9) validOps.Add(MathOp.Add);
        if (leftOperand - rightOperand >= 0) validOps.Add(MathOp.Sub);
        if (rightOperand - leftOperand >= 0) validOps.Add(MathOp.SubReverse);

        // KORUMA 2: Diğer kanallarda da Sembol 0 ise çarpma işlemine izin verilmez.
        // Çünkü "Y = X * 0" durumunda Y direkt 0 çıkar ve X'in değerini bulmaya gerek kalmaz (Oyun kırılır).
        if (rightOperand != 0 && leftOperand * rightOperand <= 9) validOps.Add(MathOp.Mul);

        if (rightOperand != 0 && leftOperand % rightOperand == 0) validOps.Add(MathOp.Div);

        return validOps;
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

    private static string ApplyFormatting(string targetVarStr, string var1Str, string var2Str, int var2Val, string evaluatedResult, MathOp op)
    {
        if (targetVarStr == "C") targetVarStr = evaluatedResult;

        List<int> availableFormats = new List<int> { 1, 2, 3 };

        // Ekstra Güvenlik Ağı (Artık teorik olarak buraya S=0 olan bir Mul gelemeyecek ama yine de tutuyoruz)
        if (op == MathOp.Mul && var2Val == 0) availableFormats.Remove(3);

        int formatType = availableFormats[Random.Range(0, availableFormats.Count)];

        string opStr = "";
        switch (op)
        {
            case MathOp.Add: opStr = "+"; break;
            case MathOp.Sub: opStr = "-"; break;
            case MathOp.SubReverse:
                opStr = "-";
                string temp = var1Str; var1Str = var2Str; var2Str = temp;
                break;
            case MathOp.Mul: opStr = "*"; break;
            case MathOp.Div: opStr = "/"; break;
        }

        if (formatType == 1) return $"{targetVarStr} = {var1Str} {opStr} {var2Str}";
        else if (formatType == 2) return $"{var1Str} {opStr} {var2Str} = {targetVarStr}";
        else
        {
            if (op == MathOp.Add) return $"{var1Str} = {targetVarStr} - {var2Str}";
            if (op == MathOp.Sub) return $"{var1Str} = {targetVarStr} + {var2Str}";
            if (op == MathOp.SubReverse) return $"{var1Str} = {var2Str} - {targetVarStr}";
            if (op == MathOp.Mul) return $"{var1Str} = {targetVarStr} / {var2Str}";
            if (op == MathOp.Div) return $"{var1Str} = {targetVarStr} * {var2Str}";

            return $"{targetVarStr} = {var1Str} {opStr} {var2Str}";
        }
    }
}