namespace MerchantDiscountMod.Domain.Multiplayer;

public readonly record struct MerchantMultiplayerVoteScope(
    int CurrentActIndex,
    int TotalFloor,
    int CurrentRoomCount);
