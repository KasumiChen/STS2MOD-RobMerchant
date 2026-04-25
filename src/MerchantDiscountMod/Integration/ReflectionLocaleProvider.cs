using System.Reflection;

namespace MerchantDiscountMod.Integration;

internal static class ReflectionLocaleProvider
{
    private static readonly string[] CandidateMethods =
    [
        "GetLocale",
        "GetToolLocale"
    ];

    private static readonly string[] CandidateTypes =
    [
        "Godot.TranslationServer",
        "Godot.OS"
    ];

    public static string? GetCurrentLocale()
    {
        foreach (var typeName in CandidateTypes)
        {
            var type = FindType(typeName);
            if (type is null)
            {
                continue;
            }

            foreach (var methodName in CandidateMethods)
            {
                var locale = TryInvokeLocaleMethod(type, methodName);
                if (!string.IsNullOrWhiteSpace(locale))
                {
                    return locale;
                }
            }
        }

        return null;
    }

    private static string? TryInvokeLocaleMethod(Type type, string methodName)
    {
        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(candidate =>
                candidate.Name == methodName
                && candidate.GetParameters().Length == 0
                && candidate.ReturnType == typeof(string));

        try
        {
            return method?.Invoke(null, []) as string;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (MemberAccessException)
        {
            return null;
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    private static Type? FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName, throwOnError: false);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }
}
