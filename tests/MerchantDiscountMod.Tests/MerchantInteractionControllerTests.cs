using MerchantDiscountMod.Domain.Run;
using MerchantDiscountMod.Domain.Shop;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Tests;

public sealed class MerchantInteractionControllerTests
{
    [Fact]
    public void FirstFivePressureAttemptsReturnFiveDistinctRefusalLines()
    {
        var controller = CreateController(out _, out _);
        var seenLines = new HashSet<string>();

        for (var attempt = 1; attempt <= 5; attempt += 1)
        {
            var response = controller.OnMerchantInteractionAttempt();

            Assert.NotNull(response.DialogueLine);
            seenLines.Add(response.DialogueLine!);
            Assert.Null(response.Prompt);
            Assert.Null(response.BattleRequest);
        }

        Assert.Equal(5, seenLines.Count);
    }

    [Fact]
    public void SixthPressureAttemptOpensDiscountPrompt()
    {
        var controller = CreateController(out var interactionState, out _);

        for (var attempt = 1; attempt <= 5; attempt += 1)
        {
            controller.OnMerchantInteractionAttempt();
        }

        var response = controller.OnMerchantInteractionAttempt();

        Assert.True(interactionState.PromptOpened);
        Assert.Null(response.DialogueLine);
        Assert.NotNull(response.Prompt);
        Assert.Equal(DiscountPromptText.EnglishTitle, response.Prompt!.Title);
        Assert.Null(response.BattleRequest);
    }

    [Fact]
    public void MerchantAndUnaffordablePurchaseAttemptsShareTheSameDiscountPromptCounter()
    {
        var controller = CreateController(out var interactionState, out _);

        for (var attempt = 1; attempt <= 3; attempt += 1)
        {
            controller.OnMerchantInteractionAttempt();
        }

        for (var attempt = 1; attempt <= 2; attempt += 1)
        {
            var response = controller.OnUnaffordablePurchaseAttempt();

            Assert.Equal(MerchantInteractionOutcome.None, response.Outcome);
            Assert.Null(response.DialogueLine);
            Assert.Null(response.Prompt);
        }

        var promptResponse = controller.OnUnaffordablePurchaseAttempt();

        Assert.True(interactionState.PromptOpened);
        Assert.Equal(MerchantInteractionOutcome.DiscountPrompt, promptResponse.Outcome);
        Assert.NotNull(promptResponse.Prompt);
    }

    [Fact]
    public void DiscountPromptTextUsesChineseLocale()
    {
        var prompt = DiscountPromptText.ForLocale("zh_CN");

        Assert.Equal("“请”商人降价", prompt.Title);
        Assert.Equal("动手", prompt.AcceptText);
        Assert.Equal("算了", prompt.DeclineText);
    }

    [Fact]
    public void DiscountPromptTextUsesEnglishFallback()
    {
        var prompt = DiscountPromptText.ForLocale("en_US");

        Assert.Equal("\"Ask\" the Merchant for a Discount", prompt.Title);
        Assert.Equal("Fight", prompt.AcceptText);
        Assert.Equal("Leave", prompt.DeclineText);
    }

    [Fact]
    public void ConfirmingPromptStartsPlaceholderMerchantBattle()
    {
        var controller = CreateController(out var interactionState, out var runState);

        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            controller.OnUnaffordablePurchaseAttempt();
        }

        var response = controller.OnDiscountConfirmed();

        Assert.True(runState.MerchantChallengePending);
        Assert.True(interactionState.CombatStarted);
        Assert.NotNull(response.BattleRequest);
        Assert.Equal("merchant_discount_placeholder", response.BattleRequest!.EncounterId);
        Assert.Equal(10, response.BattleRequest.PlaceholderDamagePerTurn);
    }

    [Fact]
    public void CancelingPromptLeavesChallengeUnstarted()
    {
        var controller = CreateController(out var interactionState, out var runState);

        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            controller.OnUnaffordablePurchaseAttempt();
        }

        controller.OnDiscountCanceled();

        Assert.False(interactionState.PromptOpened);
        Assert.False(interactionState.CombatStarted);
        Assert.False(runState.MerchantChallengePending);
    }

    private static MerchantInteractionController CreateController(
        out MerchantInteractionState interactionState,
        out MerchantRunState runState)
    {
        interactionState = new MerchantInteractionState();
        runState = new MerchantRunState();
        return new MerchantInteractionController(interactionState, runState);
    }
}
