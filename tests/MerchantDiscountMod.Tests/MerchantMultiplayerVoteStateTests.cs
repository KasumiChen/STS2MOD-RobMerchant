using MerchantDiscountMod.Domain.Multiplayer;

namespace MerchantDiscountMod.Tests;

public sealed class MerchantMultiplayerVoteStateTests
{
    [Fact]
    public void FirstVoteDoesNotCompleteUntilEveryExpectedPlayerVoted()
    {
        var state = new MerchantMultiplayerVoteState();
        var scope = new MerchantMultiplayerVoteScope(1, 6, 3);

        Assert.True(state.RecordVote(scope, 101));

        Assert.False(state.AllPlayersVoted(scope, [101, 202]));

        Assert.True(state.RecordVote(scope, 202));
        Assert.True(state.AllPlayersVoted(scope, [101, 202]));
    }

    [Fact]
    public void DuplicateVotesAreIgnored()
    {
        var state = new MerchantMultiplayerVoteState();
        var scope = new MerchantMultiplayerVoteScope(1, 6, 3);

        Assert.True(state.RecordVote(scope, 101));
        Assert.False(state.RecordVote(scope, 101));

        Assert.Equal([101UL], state.VoterIds);
    }

    [Fact]
    public void NewScopeClearsPreviousVotes()
    {
        var state = new MerchantMultiplayerVoteState();
        var previousScope = new MerchantMultiplayerVoteScope(1, 6, 3);
        var nextScope = new MerchantMultiplayerVoteScope(1, 7, 4);

        state.RecordVote(previousScope, 101);
        state.RecordVote(nextScope, 202);

        Assert.False(state.HasVoted(previousScope, 101));
        Assert.True(state.HasVoted(nextScope, 202));
        Assert.Equal([202UL], state.VoterIds);
    }
}
