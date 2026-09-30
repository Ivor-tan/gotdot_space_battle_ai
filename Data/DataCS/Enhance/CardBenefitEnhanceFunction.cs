using Godot;

/// <summary>Adapts a design card to the existing level-up benefit pipeline.</summary>
public partial class CardBenefitEnhanceFunction : BaseEnhanceFunction
{
    public CardBenefitData Card { get; private set; }

    public void Configure(CardBenefitData card)
    {
        Card = card;
        Name = card.DisplayName;
        Description = card.EffectDescription;
        Icon = CardBenefitIconCatalog.GetCardIcon(card.CategoryId, card.CardId);
        FunctionType = EnhanceFunctionType.AddictionFunction;
        CanRepeat = true;
        AppliesToAllShips = !card.RequiresTargetSelection;
    }

    public override void ApplyEffect()
    {
        if (Card == null || !IsInstanceValid(Card))
        {
            LogUtil.Warning("Card benefit application failed because its card data is unavailable.");
            return;
        }

        if (!CardBenefitRuntime.TryApply(Card, null, out string reason))
        {
            LogUtil.Warning($"Card {Card.CardId} was not applied: {reason}");
        }
    }
}
