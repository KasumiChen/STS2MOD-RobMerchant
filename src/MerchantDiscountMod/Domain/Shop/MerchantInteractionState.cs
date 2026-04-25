namespace MerchantDiscountMod.Domain.Shop;

public sealed class MerchantInteractionState
{
    public int PressureAttemptCount { get; private set; }

    public int ClickCount => PressureAttemptCount;

    public bool PromptOpened { get; private set; }

    public bool CombatStarted { get; private set; }

    public bool PromptWasDeclined { get; private set; }

    public bool InteractionLocked => PromptOpened || CombatStarted;

    public int RegisterClick()
    {
        return RegisterPressureAttempt();
    }

    public int RegisterPressureAttempt()
    {
        if (InteractionLocked)
        {
            return PressureAttemptCount;
        }

        PressureAttemptCount += 1;
        return PressureAttemptCount;
    }

    public void OpenPrompt()
    {
        PromptOpened = true;
        PromptWasDeclined = false;
    }

    public void ClosePrompt()
    {
        PromptOpened = false;
        PromptWasDeclined = true;
    }

    public void MarkCombatStarted()
    {
        PromptOpened = false;
        CombatStarted = true;
        PromptWasDeclined = false;
    }

    public void Reset()
    {
        PressureAttemptCount = 0;
        PromptOpened = false;
        CombatStarted = false;
        PromptWasDeclined = false;
    }
}
