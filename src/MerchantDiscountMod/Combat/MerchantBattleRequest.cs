namespace MerchantDiscountMod.Combat;

public sealed class MerchantBattleRequest
{
    public required string EncounterId { get; init; }

    public required string DisplayName { get; init; }

    public int PlaceholderDamagePerTurn { get; init; }

    public int RefusalClicksBeforeChallenge { get; init; }

    public bool CurrentShopPricesBecomeZeroOnVictory { get; init; }

    public bool FutureShopsBecomeEmptyOnVictory { get; init; }

    public string VictoryConsequenceSummary { get; init; } =
        "Current shop prices become free and future shops lose merchant stock.";

    public string DefeatConsequenceSummary { get; init; } =
        "Defeat is handled by the game's normal combat-loss flow.";

    public static MerchantBattleRequest Placeholder() =>
        new()
        {
            EncounterId = "merchant_discount_placeholder",
            DisplayName = "Merchant",
            PlaceholderDamagePerTurn = 10,
            RefusalClicksBeforeChallenge = 5,
            CurrentShopPricesBecomeZeroOnVictory = true,
            FutureShopsBecomeEmptyOnVictory = true
        };
}
