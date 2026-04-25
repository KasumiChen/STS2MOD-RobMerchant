using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Run;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Domain.Shop;

public sealed class MerchantInteractionController
{
    private const int PromptClickThreshold = 6;
    private readonly MerchantInteractionState interactionState;
    private readonly MerchantRunState runState;
    private readonly Func<DiscountPromptRequest> discountPromptFactory;

    public MerchantInteractionController(
        MerchantInteractionState interactionState,
        MerchantRunState runState,
        Func<DiscountPromptRequest>? discountPromptFactory = null)
    {
        this.interactionState = interactionState;
        this.runState = runState;
        this.discountPromptFactory = discountPromptFactory ?? DiscountPromptText.English;
    }

    public MerchantInteractionResponse OnMerchantClicked()
    {
        return OnMerchantInteractionAttempt();
    }

    public MerchantInteractionResponse OnMerchantInteractionAttempt()
    {
        return OnPressureAttempt(showRefusalDialogue: true);
    }

    public MerchantInteractionResponse OnUnaffordablePurchaseAttempt()
    {
        return OnPressureAttempt(showRefusalDialogue: false);
    }

    private MerchantInteractionResponse OnPressureAttempt(bool showRefusalDialogue)
    {
        if (runState.MerchantDefeatedThisRun)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredMerchantAlreadyDefeated,
                "The merchant has already been defeated this run.");
        }

        if (interactionState.PromptOpened)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredPromptAlreadyOpen,
                "The discount challenge prompt is already open.");
        }

        if (interactionState.CombatStarted || runState.MerchantChallengePending)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredCombatAlreadyStarted,
                "The merchant challenge is already waiting on combat resolution.");
        }

        var attemptCount = interactionState.RegisterPressureAttempt();

        if (attemptCount < PromptClickThreshold)
        {
            return showRefusalDialogue
                ? MerchantInteractionResponse.ShowDialogue(
                    MerchantRefusalCatalog.GetLineForClick(attemptCount))
                : MerchantInteractionResponse.None;
        }

        interactionState.OpenPrompt();
        return MerchantInteractionResponse.ShowPrompt(discountPromptFactory());
    }

    public MerchantInteractionResponse OnDiscountConfirmed()
    {
        if (runState.MerchantDefeatedThisRun)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredMerchantAlreadyDefeated,
                "The discount reward has already been claimed this run.");
        }

        if (interactionState.CombatStarted || runState.MerchantChallengePending)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredCombatAlreadyStarted,
                "The merchant challenge has already been sent to combat.");
        }

        if (!interactionState.PromptOpened)
        {
            return MerchantInteractionResponse.None;
        }

        runState.MarkChallengePending();
        interactionState.MarkCombatStarted();
        return MerchantInteractionResponse.StartCombat(MerchantBattleRequest.Placeholder());
    }

    public void OnDiscountCanceled()
    {
        interactionState.ClosePrompt();
    }
}
