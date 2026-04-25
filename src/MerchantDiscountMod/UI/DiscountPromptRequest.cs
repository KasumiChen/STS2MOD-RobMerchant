namespace MerchantDiscountMod.UI;

public sealed class DiscountPromptRequest
{
    public DiscountPromptRequest(
        string title,
        string body,
        string acceptText = "Fight",
        string declineText = "Leave")
    {
        Title = title;
        Body = body;
        AcceptText = acceptText;
        DeclineText = declineText;
    }

    public string Title { get; }

    public string Body { get; }

    public string AcceptText { get; }

    public string DeclineText { get; }
}
