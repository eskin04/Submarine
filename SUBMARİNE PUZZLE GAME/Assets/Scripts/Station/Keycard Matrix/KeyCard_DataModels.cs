using System;

// Temel Özellikler
public enum CardColor { Red, White, Green, Blue }
public enum CardType { Chip, PhosphorX, Scratched, DuctTaped }
public enum CardDetail { BrokenEdge, PostIt, HoleInMiddle, Rusty }

public enum PropertyCategory { Color, Type, Detail }
public enum RelativeDirection { Left, Right }

public enum ConditionTemplateType
{
    SpecificSocketTrait,     // [Socket_No] [MustBe/Mustn'tBe] [Trait]
    GlobalTraitPresence,     // Sistemde [MustBe/Mustn'tBe] [Trait]
    RelativeDirectionTrait,  // [Left/Right] [MustBe/Mustn'tBe] [Trait]
    ForbiddenSockets,        // [Socket_X] veya [Socket_Y]'de olmamalıdır
    RelativeSharedCategory,  // [Left/Right] aynı [Category]'i paylaşmalıdır
    ExactGlobalTraitCount    // Sistemde tam olarak [Count] tane [Trait]
}

// Ağda senkronize edilecek Koşul Verisi
[Serializable]
public struct ConditionData
{
    public ConditionTemplateType TemplateType;
    public bool IsPositive;

    public int TargetSocket1;
    public int TargetSocket2;
    public RelativeDirection Direction;
    public PropertyCategory TargetCategory;

    public CardColor TargetColor;
    public CardType TargetType;
    public CardDetail TargetDetail;

    public int TargetCount;
}

[Serializable]
public struct CardData
{
    public int CardID;
    public CardColor Color;
    public CardType Type;
    public CardDetail Detail;

    public ConditionData Condition;
}