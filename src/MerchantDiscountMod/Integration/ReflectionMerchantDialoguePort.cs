namespace MerchantDiscountMod.Integration;

public sealed class ReflectionMerchantDialoguePort : IMerchantDialoguePort
{
    private const double DialogueDurationSeconds = 2.5;
    private readonly ReflectionMerchantShopContext context;

    public ReflectionMerchantDialoguePort(ReflectionMerchantShopContext context)
    {
        this.context = context;
    }

    public void ShowMerchantLine(string line)
    {
        context.RecordMerchantDialogueLine(line);

        var merchantRoom = context.CurrentMerchantRoom;
        if (merchantRoom is null)
        {
            return;
        }

        if (ReflectionMemberAccess.TryInvoke(merchantRoom, "ShowMerchantLine", out _, line))
        {
            return;
        }

        var merchantButton = ReflectionMemberAccess.GetPropertyValue(merchantRoom, "MerchantButton");
        if (merchantButton is null)
        {
            return;
        }

        if (ReflectionMemberAccess.TryInvoke(merchantButton, "ShowMerchantLine", out _, line))
        {
            return;
        }

        ReflectionMemberAccess.Invoke(merchantButton, "PlayDialogue", line, DialogueDurationSeconds);
    }
}
