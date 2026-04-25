using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Integration;

public sealed class ReflectionDiscountPromptPort : IDiscountPromptPort
{
    private readonly ReflectionMerchantShopContext context;
    private readonly IReflectionDiscountPromptPresenter nativePresenter;
    private Action? onAccepted;
    private Action? onDeclined;

    public ReflectionDiscountPromptPort(ReflectionMerchantShopContext context)
        : this(context, new NativeGenericPopupDiscountPromptPresenter())
    {
    }

    public ReflectionDiscountPromptPort(
        ReflectionMerchantShopContext context,
        IReflectionDiscountPromptPresenter nativePresenter)
    {
        this.context = context;
        this.nativePresenter = nativePresenter;
    }

    public void Connect(Action onAccepted, Action onDeclined)
    {
        this.onAccepted = onAccepted;
        this.onDeclined = onDeclined;
    }

    public void ShowPrompt(DiscountPromptRequest request)
    {
        context.RecordPendingPrompt(request);

        var merchantRoom = context.CurrentMerchantRoom;
        if (merchantRoom is not null
            && ReflectionMemberAccess.TryInvoke(
                merchantRoom,
                "ShowDiscountPrompt",
                out _,
                request.Title,
                request.Body,
                (Action)Accept,
                (Action)Decline))
        {
            MerchantDiscountDiagnostics.Info("Discount prompt shown through merchant room shim.");
            return;
        }

        MerchantDiscountDiagnostics.Info("Merchant room prompt shim unavailable; trying native popup presenter.");
        if (nativePresenter.TryShow(request, Accept, Decline))
        {
            return;
        }

        var exception = new InvalidOperationException(
            "Discount prompt could not be shown through either the merchant-room shim or native popup presenter.");
        context.ClearPendingPrompt();
        MerchantDiscountDiagnostics.Error("Discount prompt display failed", exception);
        throw exception;
    }

    private void Accept()
    {
        context.ClearPendingPrompt();
        onAccepted?.Invoke();
    }

    private void Decline()
    {
        context.ClearPendingPrompt();
        onDeclined?.Invoke();
    }
}
