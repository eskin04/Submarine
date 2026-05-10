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
                }
            }

            if (validCount == 1)
            {
                isPuzzlePerfect = true;
                Debug.Log($"[Keycard İstasyonu] Mükemmel bulmaca {attempts}. denemede oluşturuldu. (İşaretçi Zinciri Başarılı)");
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
            allCards[i].Condition = GenerateDiverseCondition(targetSolution, allCards[i], i, targetSocketToDescribe, usedTemplates);
        }

        for (int i = 4; i < 6; i++)
        {
            int fakeMyIndex = Random.Range(0, 4);
            int fakeTargetIndex = Random.Range(0, 4);
            allCards[i].Condition = GenerateDiverseCondition(allCards, allCards[i], fakeMyIndex, fakeTargetIndex, new List<ConditionTemplateType>());

            if (Random.value > 0.5f) allCards[i].Condition.IsPositive = !allCards[i].Condition.IsPositive;

            if (Random.value > 0.7f)
            {
                allCards[i].Condition.TemplateType = ConditionTemplateType.ForbiddenSockets;
                allCards[i].Condition.IsPositive = false;
                allCards[i].Condition.TargetSocket1 = Random.Range(0, 4);
                allCards[i].Condition.TargetSocket2 = (allCards[i].Condition.TargetSocket1 + 1) % 4;
            }
        }
    }

    private ConditionData GenerateDiverseCondition(CardData[] solutionSequence, CardData myCard, int mySocketIndex, int targetSocketIndex, List<ConditionTemplateType> usedTemplates)
    {
        ConditionData cond = new ConditionData();
        cond.IsPositive = true;
        CardData targetCard = solutionSequence[targetSocketIndex];

        List<ConditionTemplateType> available = new List<ConditionTemplateType>();

        available.Add(ConditionTemplateType.SpecificSocketTrait);
        available.Add(ConditionTemplateType.ExactGlobalTraitCount);
        available.Add(ConditionTemplateType.GlobalTraitPresence);

        if (Mathf.Abs(mySocketIndex - targetSocketIndex) == 1)
        {
            available.Add(ConditionTemplateType.RelativeDirectionTrait);

            if (myCard.Color == targetCard.Color || myCard.Type == targetCard.Type || myCard.Detail == targetCard.Detail)
                available.Add(ConditionTemplateType.RelativeSharedCategory);
        }

        List<ConditionTemplateType> filtered = available.Where(t => !usedTemplates.Contains(t)).ToList();

        if (filtered.Count == 0) filtered = available;

        ConditionTemplateType selection = filtered[Random.Range(0, filtered.Count)];
        usedTemplates.Add(selection);

        cond.TemplateType = selection;
        cond.TargetCategory = (PropertyCategory)Random.Range(0, 3);

        bool makeNegative = (Random.value > 0.7f) &&
                            selection != ConditionTemplateType.RelativeSharedCategory &&
                            selection != ConditionTemplateType.ExactGlobalTraitCount;

        switch (selection)
        {
            case ConditionTemplateType.SpecificSocketTrait:
                cond.TargetSocket1 = targetSocketIndex;
                if (makeNegative) SetNegativeTrait(ref cond, targetCard);
                else SetConditionTraits(ref cond, targetCard);
                break;

            case ConditionTemplateType.RelativeDirectionTrait:
                cond.Direction = (targetSocketIndex > mySocketIndex) ? RelativeDirection.Right : RelativeDirection.Left;
                if (makeNegative) SetNegativeTrait(ref cond, targetCard);
                else SetConditionTraits(ref cond, targetCard);
                break;

            case ConditionTemplateType.RelativeSharedCategory:
                cond.Direction = (targetSocketIndex > mySocketIndex) ? RelativeDirection.Right : RelativeDirection.Left;
                List<PropertyCategory> shared = new List<PropertyCategory>();
                if (myCard.Color == targetCard.Color) shared.Add(PropertyCategory.Color);
                if (myCard.Type == targetCard.Type) shared.Add(PropertyCategory.Type);
                if (myCard.Detail == targetCard.Detail) shared.Add(PropertyCategory.Detail);
                cond.TargetCategory = shared[Random.Range(0, shared.Count)];
                break;

            case ConditionTemplateType.ExactGlobalTraitCount:
                SetConditionTraits(ref cond, targetCard);
                cond.TargetCount = solutionSequence.Count(c => CheckTrait(c, cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail));
                break;

            case ConditionTemplateType.GlobalTraitPresence:
                if (makeNegative) SetNegativeTrait(ref cond, targetCard);
                else SetConditionTraits(ref cond, targetCard);
                break;
        }

        return cond;
    }

    private void SetNegativeTrait(ref ConditionData cond, CardData targetCard)
    {
        cond.IsPositive = false;

        cond.TargetColor = (CardColor)((((int)targetCard.Color) + Random.Range(1, 4)) % 4);
        cond.TargetType = (CardType)((((int)targetCard.Type) + Random.Range(1, 4)) % 4);
        cond.TargetDetail = (CardDetail)((((int)targetCard.Detail) + Random.Range(1, 4)) % 4);
    }

    private void SetConditionTraits(ref ConditionData cond, CardData sourceCard)
    {
        cond.TargetColor = sourceCard.Color;
        cond.TargetType = sourceCard.Type;
        cond.TargetDetail = sourceCard.Detail;
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