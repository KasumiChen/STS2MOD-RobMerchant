using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Persistence;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Integration;

public sealed class ReflectionMerchantShopContext
{
    private object? currentRunState;
    private object? currentMerchantRoom;
    private object? currentMerchantCombatRoom;
    private object? pendingMerchantCombatResumeRoom;

    public object? CurrentRunState => currentRunState;

    public object? CurrentMerchantRoom => currentMerchantRoom;

    public object? CurrentMerchantCombatRoom => currentMerchantCombatRoom;

    public bool MerchantCombatLaunchInProgress => currentMerchantCombatRoom is not null;

    public string? LastMerchantDialogueLine { get; private set; }

    public DiscountPromptRequest? PendingPrompt { get; private set; }

    public MerchantBattleRequest? LastCombatRequest { get; private set; }

    public MerchantRunStateSnapshot? SavedRunStateSnapshot { get; private set; }

    public void CaptureRunState(object? runState)
    {
        currentRunState = runState;
    }

    public void CaptureMerchantRoom(object? merchantRoom)
    {
        currentMerchantRoom = merchantRoom;
    }

    public void ClearMerchantRoom()
    {
        currentMerchantRoom = null;
    }

    public void CaptureMerchantCombatRoom(object? combatRoom)
    {
        currentMerchantCombatRoom = combatRoom;
    }

    public void ClearMerchantCombatRoom()
    {
        currentMerchantCombatRoom = null;
    }

    public void MarkMerchantCombatResumePending(object combatRoom)
    {
        pendingMerchantCombatResumeRoom = combatRoom;
    }

    public bool ConsumeMerchantCombatResume(object? previousRoom)
    {
        if (pendingMerchantCombatResumeRoom is null || !ReferenceEquals(pendingMerchantCombatResumeRoom, previousRoom))
        {
            return false;
        }

        pendingMerchantCombatResumeRoom = null;
        return true;
    }

    public void RecordMerchantDialogueLine(string line)
    {
        LastMerchantDialogueLine = line;
    }

    public void RecordPendingPrompt(DiscountPromptRequest prompt)
    {
        PendingPrompt = prompt;
    }

    public void ClearPendingPrompt()
    {
        PendingPrompt = null;
    }

    public void RecordCombatLaunch(MerchantBattleRequest request)
    {
        LastCombatRequest = request;
    }

    public void SaveRunStateSnapshot(MerchantRunStateSnapshot snapshot)
    {
        SavedRunStateSnapshot = snapshot;
    }
}
