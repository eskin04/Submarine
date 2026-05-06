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

            for (int i = 0; i < 4; i++)
            {
                targetSolution[i] = finalCards[i];
            }

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
                Debug.Log($"[Keycard İstasyonu] Mükemmel bulmaca {attempts}. denemede oluşturuldu.");
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
        for (int i = 0; i < 4; i++)
        {
            allCards[i].Condition = GenerateValidConditionFor(targetSolution, targetSolution[i], i);
        }

        for (int i = 4; i < 6; i++)
        {
            int fakeIndex = Random.Range(0, 4);
            allCards[i].Condition = GenerateValidConditionFor(allCards, allCards[i], fakeIndex);

            if (Random.value > 0.5f) allCards[i].Condition.IsPositive = !allCards[i].Condition.IsPositive;
        }
    }

    private ConditionData GenerateValidConditionFor(CardData[] solutionSequence, CardData card, int mySocketIndex)
    {
        ConditionData cond = new ConditionData();
        cond.IsPositive = true;

        ConditionTemplateType randomTemplate = (ConditionTemplateType)Random.Range(0, 6);

        switch (randomTemplate)
        {
            case ConditionTemplateType.SpecificSocketTrait:
                cond.TemplateType = ConditionTemplateType.SpecificSocketTrait;
                cond.TargetSocket1 = Random.Range(0, 4);
                cond.TargetCategory = (PropertyCategory)Random.Range(0, 3);

                CardData targetCard1 = solutionSequence[cond.TargetSocket1];
                SetConditionTraits(ref cond, targetCard1);
                break;

            case ConditionTemplateType.GlobalTraitPresence:
                cond.TemplateType = ConditionTemplateType.GlobalTraitPresence;
                CardData targetCard2 = solutionSequence[Random.Range(0, 4)];
                cond.TargetCategory = (PropertyCategory)Random.Range(0, 3);
                SetConditionTraits(ref cond, targetCard2);
                break;

            case ConditionTemplateType.RelativeDirectionTrait:
                cond.TemplateType = ConditionTemplateType.RelativeDirectionTrait;
                if (mySocketIndex == 0) cond.Direction = RelativeDirection.Right;
                else if (mySocketIndex == 3) cond.Direction = RelativeDirection.Left;
                else cond.Direction = (RelativeDirection)Random.Range(0, 2);

                int neighborIndex = cond.Direction == RelativeDirection.Left ? mySocketIndex - 1 : mySocketIndex + 1;
                cond.TargetCategory = (PropertyCategory)Random.Range(0, 3);
                SetConditionTraits(ref cond, solutionSequence[neighborIndex]);
                break;

            case ConditionTemplateType.ForbiddenSockets:
                cond.TemplateType = ConditionTemplateType.ForbiddenSockets;
                cond.IsPositive = false;
                List<int> availableSockets = new List<int> { 0, 1, 2, 3 };
                availableSockets.Remove(mySocketIndex);

                cond.TargetSocket1 = availableSockets[Random.Range(0, availableSockets.Count)];
                availableSockets.Remove(cond.TargetSocket1);
                cond.TargetSocket2 = availableSockets[Random.Range(0, availableSockets.Count)];
                break;

            case ConditionTemplateType.RelativeSharedCategory:
                if (mySocketIndex == 0) cond.Direction = RelativeDirection.Right;
                else if (mySocketIndex == 3) cond.Direction = RelativeDirection.Left;
                else cond.Direction = (RelativeDirection)Random.Range(0, 2);

                int neighborIdx = cond.Direction == RelativeDirection.Left ? mySocketIndex - 1 : mySocketIndex + 1;
                CardData neighbor = solutionSequence[neighborIdx];

                List<PropertyCategory> sharedCategories = new List<PropertyCategory>();
                if (card.Color == neighbor.Color) sharedCategories.Add(PropertyCategory.Color);
                if (card.Type == neighbor.Type) sharedCategories.Add(PropertyCategory.Type);
                if (card.Detail == neighbor.Detail) sharedCategories.Add(PropertyCategory.Detail);

                if (sharedCategories.Count > 0)
                {
                    cond.TemplateType = ConditionTemplateType.RelativeSharedCategory;
                    cond.TargetCategory = sharedCategories[Random.Range(0, sharedCategories.Count)];
                }
                else
                {
                    cond.TemplateType = ConditionTemplateType.SpecificSocketTrait;
                    cond.TargetSocket1 = mySocketIndex;
                    cond.TargetCategory = PropertyCategory.Color;
                    cond.TargetColor = card.Color;
                }
                break;

            case ConditionTemplateType.ExactGlobalTraitCount:
                cond.TemplateType = ConditionTemplateType.ExactGlobalTraitCount;
                CardData randomTarget = solutionSequence[Random.Range(0, 4)];
                cond.TargetCategory = (PropertyCategory)Random.Range(0, 3);
                SetConditionTraits(ref cond, randomTarget);

                int count = solutionSequence.Count(c => CheckTrait(c, cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail));
                cond.TargetCount = count;
                break;
        }

        return cond;
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
            {
                return false;
            }
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
                {
                    conditionMet = CheckTrait(systemCards[cond.TargetSocket1], cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail);
                }
                break;

            case ConditionTemplateType.GlobalTraitPresence:
                conditionMet = systemCards.Any(c => CheckTrait(c, cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail));
                break;

            case ConditionTemplateType.RelativeDirectionTrait:
                int targetIndex = cond.Direction == RelativeDirection.Left ? currentIndex - 1 : currentIndex + 1;
                if (targetIndex >= 0 && targetIndex < systemCards.Length)
                {
                    conditionMet = CheckTrait(systemCards[targetIndex], cond.TargetCategory, cond.TargetColor, cond.TargetType, cond.TargetDetail);
                }
                break;

            case ConditionTemplateType.ForbiddenSockets:
                conditionMet = (currentIndex == cond.TargetSocket1 || currentIndex == cond.TargetSocket2);
                break;

            case ConditionTemplateType.RelativeSharedCategory:
                int relIndex = cond.Direction == RelativeDirection.Left ? currentIndex - 1 : currentIndex + 1;
                if (relIndex >= 0 && relIndex < systemCards.Length)
                {
                    conditionMet = ShareSameTrait(card, systemCards[relIndex], cond.TargetCategory);
                }
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