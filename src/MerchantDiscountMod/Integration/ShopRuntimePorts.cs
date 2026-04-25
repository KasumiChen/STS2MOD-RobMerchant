using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Shop;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Integration;

public interface IShopStateRenderer
{
    void Render(ShopStateSnapshot snapshot);
}

public interface IMerchantDialoguePort
{
    void ShowMerchantLine(string line);
}

public interface IDiscountPromptPort
{
    void ShowPrompt(DiscountPromptRequest request);
}

public interface IReflectionDiscountPromptPresenter
{
    bool TryShow(DiscountPromptRequest request, Action onAccepted, Action onDeclined);
}

public interface IShopCombatPort
{
    void Launch(MerchantBattleRequest request);
}

public interface IRunStatePersistence
{
    void SaveRunState();
}
