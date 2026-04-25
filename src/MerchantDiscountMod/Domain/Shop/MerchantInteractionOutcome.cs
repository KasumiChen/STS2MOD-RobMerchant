namespace MerchantDiscountMod.Domain.Shop;

public enum MerchantInteractionOutcome
{
    None = 0,
    RefusalDialogue = 1,
    DiscountPrompt = 2,
    BattleRequested = 3,
    IgnoredMerchantUnavailable = 4,
    IgnoredPromptAlreadyOpen = 5,
    IgnoredCombatAlreadyStarted = 6,
    IgnoredMerchantAlreadyDefeated = 7
}
