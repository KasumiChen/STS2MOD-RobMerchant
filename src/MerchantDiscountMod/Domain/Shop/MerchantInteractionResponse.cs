using MerchantDiscountMod.Combat;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Domain.Shop;

public sealed class MerchantInteractionResponse
{
    private MerchantInteractionResponse(
        MerchantInteractionOutcome outcome,
        string? dialogueLine,
        DiscountPromptRequest? prompt,
        MerchantBattleRequest? battleRequest,
        string? ignoredReason)
    {
        Outcome = outcome;
        DialogueLine = dialogueLine;
        Prompt = prompt;
        BattleRequest = battleRequest;
        IgnoredReason = ignoredReason;
    }

    public MerchantInteractionOutcome Outcome { get; }

    public string? DialogueLine { get; }

    public DiscountPromptRequest? Prompt { get; }

    public MerchantBattleRequest? BattleRequest { get; }

    public string? IgnoredReason { get; }

    public static MerchantInteractionResponse None { get; } =
        new(MerchantInteractionOutcome.None, null, null, null, null);

    public static MerchantInteractionResponse ShowDialogue(string line) =>
        new(MerchantInteractionOutcome.RefusalDialogue, line, null, null, null);

    public static MerchantInteractionResponse ShowPrompt(DiscountPromptRequest prompt) =>
        new(MerchantInteractionOutcome.DiscountPrompt, null, prompt, null, null);

    public static MerchantInteractionResponse StartCombat(MerchantBattleRequest battleRequest) =>
        new(MerchantInteractionOutcome.BattleRequested, null, null, battleRequest, null);

    public static MerchantInteractionResponse Ignored(
        MerchantInteractionOutcome outcome,
        string reason) =>
        new(outcome, null, null, null, reason);
}
