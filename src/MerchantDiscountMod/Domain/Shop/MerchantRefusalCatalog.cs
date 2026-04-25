namespace MerchantDiscountMod.Domain.Shop;

public static class MerchantRefusalCatalog
{
    public static readonly string[] DefaultLines =
    {
        "These prices are already a bargain.",
        "No haggling. You will not find better value.",
        "I said no. Browse or leave.",
        "You test my patience more than my prices.",
        "One more complaint and we settle this another way."
    };

    public static string GetLineForClick(int clickCount)
    {
        if (clickCount <= 0)
        {
            return DefaultLines[0];
        }

        var index = clickCount - 1;
        if (index >= DefaultLines.Length)
        {
            index = DefaultLines.Length - 1;
        }

        return DefaultLines[index];
    }
}
