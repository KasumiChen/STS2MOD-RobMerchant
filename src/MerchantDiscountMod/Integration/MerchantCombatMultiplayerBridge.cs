#if NET9_0_OR_GREATER
using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Multiplayer;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace MerchantDiscountMod.Integration;

internal static class MerchantCombatMultiplayerBridge
{
    private static readonly MerchantMultiplayerVoteState VoteState = new();
    private static INetGameService? registeredNetService;
    private static bool battleStartQueued;
    private static Action? voteStateChanged;
    private static Action? votePromptClosing;

    public static bool PrepareForMultiplayerRun()
    {
        if (!RunManager.Instance.IsInProgress || !RunManager.Instance.NetService.Type.IsMultiplayer())
        {
            return false;
        }

        EnsureVoteMessageHandler(RunManager.Instance.NetService);
        return true;
    }

    public static bool RequestLaunch(MerchantBattleRequest request, object? runState)
    {
        _ = request;
        _ = runState;

        if (!PrepareForMultiplayerRun())
        {
            return false;
        }

        var currentRunState = RunManager.Instance.DebugOnlyGetState();
        if (currentRunState is null)
        {
            MerchantDiscountDiagnostics.Error(
                "Synchronized merchant combat request failed",
                new InvalidOperationException("No current run state is available."));
            return false;
        }

        var localPlayer = LocalContext.GetMe(currentRunState);
        if (localPlayer is null)
        {
            MerchantDiscountDiagnostics.Error(
                "Synchronized merchant combat request failed",
                new InvalidOperationException("Could not find the local multiplayer player."));
            return false;
        }

        var scope = GetVoteScope(currentRunState);
        RecordVote(scope, localPlayer.NetId);
        RunManager.Instance.NetService.SendMessage(new RobMerchantBattleVoteMessage(scope, localPlayer.NetId));
        TryQueueBattleStart(scope, currentRunState);
        return true;
    }

    public static void RegisterVoteUiRefresh(Action refresh)
    {
        voteStateChanged -= refresh;
        voteStateChanged += refresh;
    }

    public static void UnregisterVoteUiRefresh(Action refresh)
    {
        voteStateChanged -= refresh;
    }

    public static void RegisterVotePromptClose(Action close)
    {
        votePromptClosing -= close;
        votePromptClosing += close;
    }

    public static void UnregisterVotePromptClose(Action close)
    {
        votePromptClosing -= close;
    }

    public static bool PlayerHasPendingVote(object? player)
    {
        if (player is not Player typedPlayer)
        {
            return false;
        }

        var runState = RunManager.Instance.DebugOnlyGetState();
        return runState is not null && VoteState.HasVoted(GetVoteScope(runState), typedPlayer.NetId);
    }

    internal static Task ExecuteLaunch()
    {
        NotifyVotePromptsClosing();
        ResetVotes();
        var currentRunState = RunManager.Instance.DebugOnlyGetState();
        return MerchantDiscountLiveBootstrap.LaunchMerchantCombatFromSynchronizedAction(currentRunState);
    }

    private static void EnsureVoteMessageHandler(INetGameService netService)
    {
        if (ReferenceEquals(registeredNetService, netService))
        {
            return;
        }

        if (registeredNetService is not null)
        {
            try
            {
                registeredNetService.UnregisterMessageHandler<RobMerchantBattleVoteMessage>(HandleVoteMessage);
            }
            catch (Exception exception)
            {
                MerchantDiscountDiagnostics.Warn($"Could not unregister stale merchant vote handler: {exception.Message}");
            }
        }

        registeredNetService = netService;
        netService.RegisterMessageHandler<RobMerchantBattleVoteMessage>(HandleVoteMessage);
        MerchantDiscountDiagnostics.Info($"Merchant combat multiplayer vote handler bound to {netService.Type}.");
    }

    private static void HandleVoteMessage(RobMerchantBattleVoteMessage message, ulong senderId)
    {
        EnsureVoteMessageHandler(RunManager.Instance.NetService);

        var voterId = message.VoterId == 0 ? senderId : message.VoterId;
        var scope = message.ToScope();
        RecordVote(scope, voterId);

        var currentRunState = RunManager.Instance.DebugOnlyGetState();
        if (currentRunState is not null)
        {
            TryQueueBattleStart(scope, currentRunState);
        }
    }

    private static void RecordVote(MerchantMultiplayerVoteScope scope, ulong voterId)
    {
        if (VoteState.RecordVote(scope, voterId))
        {
            MerchantDiscountDiagnostics.Info($"Merchant combat vote recorded from player {voterId}.");
        }

        NotifyVotesChanged();
    }

    private static void TryQueueBattleStart(MerchantMultiplayerVoteScope scope, RunState runState)
    {
        var expectedPlayerIds = runState.Players.Select(player => player.NetId).ToArray();
        if (battleStartQueued || !VoteState.AllPlayersVoted(scope, expectedPlayerIds))
        {
            return;
        }

        if (RunManager.Instance.NetService.Type != NetGameType.Host)
        {
            MerchantDiscountDiagnostics.Info("All local merchant combat votes recorded; waiting for host action.");
            return;
        }

        var localPlayer = LocalContext.GetMe(runState);
        if (localPlayer is null)
        {
            MerchantDiscountDiagnostics.Error(
                "Synchronized merchant combat start failed",
                new InvalidOperationException("Could not find the local multiplayer host player."));
            return;
        }

        battleStartQueued = true;
        MerchantDiscountDiagnostics.Info("All merchant combat votes received; host is enqueueing combat.");
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
            new RobMerchantStartBattleGameAction(localPlayer.NetId));
    }

    private static MerchantMultiplayerVoteScope GetVoteScope(RunState runState) =>
        new(runState.CurrentActIndex, runState.TotalFloor, runState.CurrentRoomCount);

    private static void ResetVotes()
    {
        VoteState.Reset();
        battleStartQueued = false;
        NotifyVotesChanged();
    }

    private static void NotifyVotesChanged()
    {
        try
        {
            voteStateChanged?.Invoke();
        }
        catch (Exception exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat vote UI refresh failed: {exception.Message}");
        }
    }

    private static void NotifyVotePromptsClosing()
    {
        try
        {
            votePromptClosing?.Invoke();
        }
        catch (Exception exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat vote prompt close failed: {exception.Message}");
        }
    }
}

public sealed class RobMerchantStartBattleGameAction : GameAction
{
    private readonly ulong ownerId;

    public RobMerchantStartBattleGameAction(ulong ownerId)
    {
        this.ownerId = ownerId;
    }

    public override ulong OwnerId => ownerId;

    public override GameActionType ActionType => GameActionType.NonCombat;

    protected override Task ExecuteAction() => MerchantCombatMultiplayerBridge.ExecuteLaunch();

    public override INetAction ToNetAction() => new RobMerchantStartBattleNetAction();

    public override string ToString() => $"RobMerchantStartBattleGameAction owner={ownerId}";
}

public struct RobMerchantStartBattleNetAction : INetAction
{
    public readonly GameAction ToGameAction(Player player) =>
        new RobMerchantStartBattleGameAction(player.NetId);

    public readonly void Serialize(PacketWriter writer)
    {
        _ = writer;
    }

    public void Deserialize(PacketReader reader)
    {
        _ = reader;
    }

    public override readonly string ToString() => nameof(RobMerchantStartBattleNetAction);
}

public struct RobMerchantBattleVoteMessage : INetMessage, IPacketSerializable
{
    public int CurrentActIndex;
    public int TotalFloor;
    public int CurrentRoomCount;
    public ulong VoterId;

    public RobMerchantBattleVoteMessage(MerchantMultiplayerVoteScope scope, ulong voterId)
    {
        CurrentActIndex = scope.CurrentActIndex;
        TotalFloor = scope.TotalFloor;
        CurrentRoomCount = scope.CurrentRoomCount;
        VoterId = voterId;
    }

    public readonly bool ShouldBroadcast => true;

    public readonly NetTransferMode Mode => NetTransferMode.Reliable;

    public readonly LogLevel LogLevel => LogLevel.Info;

    public readonly MerchantMultiplayerVoteScope ToScope() =>
        new(CurrentActIndex, TotalFloor, CurrentRoomCount);

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteInt(CurrentActIndex, 16);
        writer.WriteInt(TotalFloor, 16);
        writer.WriteInt(CurrentRoomCount, 16);
        writer.WriteULong(VoterId, 64);
    }

    public void Deserialize(PacketReader reader)
    {
        CurrentActIndex = reader.ReadInt(16);
        TotalFloor = reader.ReadInt(16);
        CurrentRoomCount = reader.ReadInt(16);
        VoterId = reader.ReadULong(64);
    }

    public override readonly string ToString() =>
        $"RobMerchantBattleVote player={VoterId} act={CurrentActIndex} floor={TotalFloor} room={CurrentRoomCount}";
}
#endif
