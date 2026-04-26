namespace MerchantDiscountMod.Domain.Multiplayer;

public sealed class MerchantMultiplayerVoteState
{
    private readonly HashSet<ulong> voterIds = [];
    private MerchantMultiplayerVoteScope? currentScope;

    public IReadOnlyCollection<ulong> VoterIds => voterIds;

    public bool RecordVote(MerchantMultiplayerVoteScope scope, ulong voterId)
    {
        EnsureScope(scope);
        return voterIds.Add(voterId);
    }

    public bool HasVoted(MerchantMultiplayerVoteScope scope, ulong voterId)
    {
        return currentScope == scope && voterIds.Contains(voterId);
    }

    public bool AllPlayersVoted(MerchantMultiplayerVoteScope scope, IEnumerable<ulong> expectedPlayerIds)
    {
        EnsureScope(scope);

        var expectedIds = expectedPlayerIds.ToArray();
        return expectedIds.Length > 0 && expectedIds.All(voterIds.Contains);
    }

    public void Reset()
    {
        currentScope = null;
        voterIds.Clear();
    }

    private void EnsureScope(MerchantMultiplayerVoteScope scope)
    {
        if (currentScope == scope)
        {
            return;
        }

        currentScope = scope;
        voterIds.Clear();
    }
}
