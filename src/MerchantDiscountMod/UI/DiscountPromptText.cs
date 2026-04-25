namespace MerchantDiscountMod.UI;

public static class DiscountPromptText
{
    public const string EnglishTitle = "\"Ask\" the Merchant for a Discount";
    public const string EnglishBody = "The merchant may not appreciate your tone.";
    public const string EnglishAcceptText = "Fight";
    public const string EnglishDeclineText = "Leave";
    public const string ChineseTitle = "“请”商人降价";
    public const string ChineseBody = "要是商人不肯，这事就换个方式谈。";
    public const string ChineseAcceptText = "动手";
    public const string ChineseDeclineText = "算了";

    public static DiscountPromptRequest ForLocale(string? locale) =>
        IsChineseLocale(locale)
            ? new DiscountPromptRequest(
                ChineseTitle,
                ChineseBody,
                ChineseAcceptText,
                ChineseDeclineText)
            : English();

    public static DiscountPromptRequest English() =>
        new(
            EnglishTitle,
            EnglishBody,
            EnglishAcceptText,
            EnglishDeclineText);

    public static bool IsChineseLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return false;
        }

        var normalized = locale.Trim().Replace('-', '_').ToLowerInvariant();
        return normalized.StartsWith("zh", StringComparison.Ordinal)
            || normalized.StartsWith("zhs", StringComparison.Ordinal)
            || normalized.StartsWith("zht", StringComparison.Ordinal);
    }
}
