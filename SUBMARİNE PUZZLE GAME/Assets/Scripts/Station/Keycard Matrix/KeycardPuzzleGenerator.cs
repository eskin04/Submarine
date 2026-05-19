using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KeycardPuzzleGenerator
{
    private List<int[]> allPermutations;

    public KeycardPuzzleGenerator()
    {
        GenerateAllPermutations();
    }

    public CardData[] GeneratePuzzle()
    {
        bool isPuzzlePerfect = false;
        CardData[] finalCards = new CardData[6];
        CardData[] targetSolution = new CardData[4];

        int attempts = 0;
        int maxAttempts = 5000;

        while (!isPuzzlePerfect && attempts < maxAttempts)
        {
            attempts++;
            finalCards = CreateSixUniqueCards();

            for (int i = 0; i < 4; i++) targetSolution[i] = finalCards[i];

            AssignConditions(finalCards, targetSolution);

            if (!EvaluatePermutation(targetSolution))
            {
                continue;
            }

            int validCount = 0;
            foreach (var permutationIndices in allPermutations)
            {
                CardData[] testSequence = new CardData[4];
                testSequence[0] = finalCards[permutationIndices[0]];
                testSequence[1] = finalCards[permutationIndices[1]];
                testSequence[2] = finalCards[permutationIndices[2]];
                testSequence[3] = finalCards[permutationIndices[3]];

                if (EvaluatePermutation(testSequence))
                {
                    validCount++;
                    if (validCount > 1) break;
                }
            }

            if (validCount == 1)
            {
                isPuzzlePerfect = true;
                Debug.Log($"<color=green>[Keycard İstasyonu] MÜKEMMEL bulmaca {attempts}. denemede oluşturuldu!</color>");
            }
        }

        if (attempts >= maxAttempts)
            Debug.LogError("[Keycard İstasyonu] Bulmaca oluşturulamadı, limit aşıldı.");

        return finalCards;
    }

    private void GenerateAllPermutations()
    {
        allPermutations = new List<int[]>();
        for (int a = 0; a < 6; a++)
        {
            for (int b = 0; b < 6; b++)
            {
                if (a == b) continue;
                for (int c = 0; c < 6; c++)
                {
                    if (a == c || b == c) continue;
                    for (int d = 0; d < 6; d++)
                    {
                        if (a == d || b == d || c == d) continue;
                        allPermutations.Add(new int[] { a, b, c, d });
                    }
                }
            }
        }
    }

    private CardData[] CreateSixUniqueCards()
    {
        CardData[] cards = new CardData[6];
        List<string> generatedSignatures = new List<string>();

        for (int i = 0; i < 6; i++)
        {
            CardData newCard = new CardData();
            bool isUnique = false;
            while (!isUnique)
            {
                newCard.CardID = i + 1;
                newCard.Color = (CardColor)Random.Range(0, 4);
                newCard.Type = (CardType)Random.Range(0, 4);
                newCard.Detail = (CardDetail)Random.Range(0, 4);

                string signature = $"{newCard.Color}_{newCard.Type}_{newCard.Detail}";
                if (!generatedSignatures.Contains(signature))
                {
                    generatedSignatures.Add(signature);
                    isUnique = true;
                }
            }
            cards[i] = newCard;
        }
        return cards;
    }

    private void AssignConditions(CardData[] allCards, CardData[] targetSolution)
    {
        int[][] validCycles = new int[][]
        {
            new int[] { 1, 2, 3, 0 },
            new int[] { 1, 3, 0, 2 },
            new int[] { 2, 0, 3, 1 },
            new int[] { 2, 3, 1, 0 },
            new int[] { 3, 0, 1, 2 },
            new int[] { 3, 2, 0, 1 }
        };

        int[] selectedChain = validCycles[Random.Range(0, validCycles.Length)];
        List<ConditionTemplateType> usedTemplates = new List<ConditionTemplateType>();

        for (int i = 0; i < 4; i++)
        {
            int targetSocketToDescribe = selectedChain[i];
            allCards[i].Condition = GenerateTruthfulCondition(targetSolution, allCards[i], i, targetSocketToDescribe, usedTemplates);
        }

        for (int i = 4; i < 6; i++)
        {
            ConditionData fakeCond = new ConditionData();
            fakeCond.TemplateType = (ConditionTemplateType)Random.Range(0, 6);
            fakeCond.IsPositive = Random.value > 0.5f;
            fakeCond.TargetSocket1 = Random.Range(0, 4);
            fakeCond.TargetSocket2 = (fakeCond.TargetSocket1 + 1) % 4;
            fakeCond.TargetCategory = (PropertyCategory)Random.Range(0, 3);
            fakeCond.TargetColor = (CardColor)Random.Range(0, 4);
            fakeCond.TargetType = (CardType)Random.Range(0, 4);
            fakeCond.TargetDetail = (CardDetail)Random.Range(0, 4);
            fakeCond.TargetCount = Random.Range(1, 4);
            fakeCond.Direction = Random.value > 0.5f ? RelativeDirection.Left : RelativeDirection.Right;

            allCards[i].Condition = fakeCond;
        }
    }

    private ConditionData GenerateTruthfulCondition(CardData[] solution, CardData myCard, int myIndex, int targetIndex, List<ConditionTemplateType> usedTemplates)
    {
        ConditionData cond = new ConditionData();
        cond.IsPositive = true;
        CardData targetCard = solution[targetIndex];

        List<ConditionTemplateType> available = new List<ConditionTemplateType> { ConditionTemplateType.SpecificSocketTrait };

        if (Mathf.Abs(myIndex - targetIndex) == 1)
        {
            available.Add(ConditionTemplateType.RelativeDirectionTrait);
            if (myCard.Color == targetCard.Color || myCard.Type == targetCard.Type || myCard.Detail == targetCard.Detail)
                available.Add(ConditionTemplateType.RelativeSharedCategory);
        }

        available.Add(ConditionTemplateType.ExactGlobalTraitCount);
        available.Add(ConditionTemplateType.GlobalTraitPresence);

        List<ConditionTemplateType> filtered = available.Where(t => !usedTemplates.Contains(t)).ToList();
        if (filtered.Count == 0) filtered = available;

        ConditionTemplateType selection = filtered[Random.Range(0, filtered.Count)];
        usedTemplates.Add(selection);
        cond.TemplateType = selection;

        bool wantsNegative = Random.value > 0.7f;

        switch (selection)
        {
            case ConditionTemplateType.SpecificSocketTrait:
                cond.TargetSocket1 = targetIndex;
                if (wantsNegative) SetRandomFalseTrait(ref cond, targetCard);
                else SetRandomTrueTrait(ref cond, targetCard);
                break;

            case ConditionTemplateType.RelativeDirectionTrait:
                cond.Direction = (targetIndex > myIndex) ? RelativeDirection.Right : RelativeDirection.Left;
                if (wantsNegative) SetRandomFalseTrait(ref cond, targetCard);
                else SetRandomTrueTrait(ref cond, targetCard);
                break;

            case ConditionTemplateType.RelativeSharedCategory:
                cond.Direction = (targetIndex > myIndex) ? RelativeDirection.Right : RelativeDirection.Left;
                List<PropertyCategory> shared = new List<PropertyCategory>();
                if (myCard.Color == targetCard.Color) shared.Add(PropertyCategory.Color);
                if (myCard.Type == targetCard.Type) shared.Add(PropertyCategory.Type);
                if (myCard.Detail == targetCard.Detail) shared.Add(PropertyCategory.Detail);
                cond.TargetCategory = shared[Random.Range(0, shared.Count)];
                break;

            case ConditionTemplateType.ExactGlobalTraitCount:
                if (SetTrueTraitDifferentFromMe(ref cond, targetCard, myCard))
                {
                    cond.TargetCount = solution.Count(c => CheckTrait(c, cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail));
                }
                else
                {
                    cond.TemplateType = ConditionTemplateType.SpecificSocketTrait;
                    cond.TargetSocket1 = targetIndex;
                    SetRandomTrueTrait(ref cond, targetCard);
                }
                break;

            case ConditionTemplateType.GlobalTraitPresence:
                if (wantsNegative)
                {
                    if (!SetRandomAbsolutelyFalseTrait(ref cond, solution))
                    {
                        cond.IsPositive = true;
                        SetTrueTraitDifferentFromMe(ref cond, targetCard, myCard);
                    }
                }
                else
                {
                    if (!SetTrueTraitDifferentFromMe(ref cond, targetCard, myCard))
                    {
                        cond.TemplateType = ConditionTemplateType.SpecificSocketTrait;
                        cond.TargetSocket1 = targetIndex;
                        SetRandomTrueTrait(ref cond, targetCard);
                    }
                }
                break;
        }

        return cond;
    }

    private void SetRandomTrueTrait(ref ConditionData cond, CardData source)
    {
        cond.TargetCategory = (PropertyCategory)Random.Range(0, 3);
        cond.TargetColor = source.Color;
        cond.TargetType = source.Type;
        cond.TargetDetail = source.Detail;
    }

    private void SetRandomFalseTrait(ref ConditionData cond, CardData source)
    {
        cond.IsPositive = false;
        cond.TargetCategory = (PropertyCategory)Random.Range(0, 3);
        cond.TargetColor = source.Color;
        cond.TargetType = source.Type;
        cond.TargetDetail = source.Detail;

        if (cond.TargetCategory == PropertyCategory.Color)
            cond.TargetColor = (CardColor)(((int)source.Color + Random.Range(1, 4)) % 4);
        else if (cond.TargetCategory == PropertyCategory.Type)
            cond.TargetType = (CardType)(((int)source.Type + Random.Range(1, 4)) % 4);
        else if (cond.TargetCategory == PropertyCategory.Detail)
            cond.TargetDetail = (CardDetail)(((int)source.Detail + Random.Range(1, 4)) % 4);
    }

    private bool SetTrueTraitDifferentFromMe(ref ConditionData cond, CardData targetCard, CardData myCard)
    {
        List<PropertyCategory> diffs = new List<PropertyCategory>();
        if (targetCard.Color != myCard.Color) diffs.Add(PropertyCategory.Color);
        if (targetCard.Type != myCard.Type) diffs.Add(PropertyCategory.Type);
        if (targetCard.Detail != myCard.Detail) diffs.Add(PropertyCategory.Detail);

        if (diffs.Count == 0) return false;

        cond.TargetCategory = diffs[Random.Range(0, diffs.Count)];
        cond.TargetColor = targetCard.Color;
        cond.TargetType = targetCard.Type;
        cond.TargetDetail = targetCard.Detail;
        return true;
    }

    private bool SetRandomAbsolutelyFalseTrait(ref ConditionData cond, CardData[] solution)
    {
        cond.IsPositive = false;
        List<PropertyCategory> cats = new List<PropertyCategory> { PropertyCategory.Color, PropertyCategory.Type, PropertyCategory.Detail };
        cats = cats.OrderBy(x => Random.value).ToList(); // Rastgele karıştır

        foreach (var cat in cats)
        {
            if (cat == PropertyCategory.Color)
            {
                var used = solution.Select(c => c.Color).ToList();
                for (int i = 0; i < 4; i++)
                {
                    if (!used.Contains((CardColor)i))
                    {
                        cond.TargetCategory = cat; cond.TargetColor = (CardColor)i; return true;
                    }
                }
            }
            if (cat == PropertyCategory.Type)
            {
                var used = solution.Select(c => c.Type).ToList();
                for (int i = 0; i < 4; i++)
                {
                    if (!used.Contains((CardType)i))
                    {
                        cond.TargetCategory = cat; cond.TargetType = (CardType)i; return true;
                    }
                }
            }
            if (cat == PropertyCategory.Detail)
            {
                var used = solution.Select(c => c.Detail).ToList();
                for (int i = 0; i < 4; i++)
                {
                    if (!used.Contains((CardDetail)i))
                    {
                        cond.TargetCategory = cat; cond.TargetDetail = (CardDetail)i; return true;
                    }
                }
            }
        }
        return false;
    }

    public bool EvaluatePermutation(CardData[] systemCards)
    {
        for (int i = 0; i < systemCards.Length; i++)
        {
            if (!DoesConditionMatch(systemCards[i], i, systemCards))
                return false;
        }
        return true;
    }

    private bool DoesConditionMatch(CardData card, int currentIndex, CardData[] systemCards)
    {
        ConditionData cond = card.Condition;
        bool conditionMet = false;

        switch (cond.TemplateType)
        {
            case ConditionTemplateType.SpecificSocketTrait:
                if (cond.TargetSocket1 >= 0 && cond.TargetSocket1 < systemCards.Length)
                    conditionMet = CheckTrait(systemCards[cond.TargetSocket1], cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail);
                break;

            case ConditionTemplateType.GlobalTraitPresence:
                conditionMet = systemCards.Any(c => CheckTrait(c, cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail));
                break;

            case ConditionTemplateType.RelativeDirectionTrait:
                int targetIndex = cond.Direction == RelativeDirection.Left ? currentIndex - 1 : currentIndex + 1;
                if (targetIndex >= 0 && targetIndex < systemCards.Length)
                    conditionMet = CheckTrait(systemCards[targetIndex], cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail);
                break;

            case ConditionTemplateType.ForbiddenSockets:
                conditionMet = (currentIndex == cond.TargetSocket1 || currentIndex == cond.TargetSocket2);
                break;

            case ConditionTemplateType.RelativeSharedCategory:
                int relIndex = cond.Direction == RelativeDirection.Left ? currentIndex - 1 : currentIndex + 1;
                if (relIndex >= 0 && relIndex < systemCards.Length)
                    conditionMet = ShareSameTrait(card, systemCards[relIndex], cond.TargetCategory);
                break;

            case ConditionTemplateType.ExactGlobalTraitCount:
                int count = systemCards.Count(c => CheckTrait(c, cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail));
                conditionMet = (count == cond.TargetCount);
                break;
        }

        return cond.IsPositive ? conditionMet : !conditionMet;
    }

    private bool CheckTrait(CardData card, PropertyCategory category, CardColor color, CardType type, CardDetail detail)
    {
        switch (category)
        {
            case PropertyCategory.Color: return card.Color == color;
            case PropertyCategory.Type: return card.Type == type;
            case PropertyCategory.Detail: return card.Detail == detail;
            default: return false;
        }
    }

    private bool ShareSameTrait(CardData c1, CardData c2, PropertyCategory category)
    {
        switch (category)
        {
            case PropertyCategory.Color: return c1.Color == c2.Color;
            case PropertyCategory.Type: return c1.Type == c2.Type;
            case PropertyCategory.Detail: return c1.Detail == c2.Detail;
            default: return false;
        }
    }
}