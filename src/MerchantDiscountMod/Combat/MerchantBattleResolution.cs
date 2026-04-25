using MerchantDiscountMod.Domain.Run;
using MerchantDiscountMod.Domain.Shop;

namespace MerchantDiscountMod.Combat;

public sealed record MerchantBattleResolution(
    MerchantBattleResult Result,
    bool Applied,
    MerchantRunPhase RunPhase,
    ShopInventoryMode ShopInventoryMode,
    string Consequence);
