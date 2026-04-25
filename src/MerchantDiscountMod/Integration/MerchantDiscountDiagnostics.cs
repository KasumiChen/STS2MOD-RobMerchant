using System.Reflection;

namespace MerchantDiscountMod.Integration;

internal static class MerchantDiscountDiagnostics
{
    private const string Prefix = "[RobMerchant]";
    private static object? logger;

    public static void Info(string message) => Write("Info", message, exception: null);

    public static void Warn(string message) => Write("Warn", message, exception: null);

    public static void Error(string message, Exception exception) => Write("Error", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        var text = exception is null
            ? $"{Prefix} {message}"
            : $"{Prefix} {message}: {exception}";

        if (level == "Error")
        {
            Console.Error.WriteLine(text);
        }
        else
        {
            Console.WriteLine(text);
        }

        TryWriteToSts2Logger(level, text);
    }

    private static void TryWriteToSts2Logger(string level, string text)
    {
        try
        {
            var loggerInstance = logger ??= CreateSts2Logger();
            var method = loggerInstance?.GetType().GetMethod(
                level,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: [typeof(string), typeof(int)],
                modifiers: null);

            method?.Invoke(loggerInstance, [text, 1]);
        }
        catch (ArgumentException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (TargetInvocationException)
        {
        }
        catch (MemberAccessException)
        {
        }
    }

    private static object? CreateSts2Logger()
    {
        var loggerType = FindType("MegaCrit.Sts2.Core.Logging.Logger");
        var logType = FindType("MegaCrit.Sts2.Core.Logging.LogType");
        if (loggerType is null || logType is null)
        {
            return null;
        }

        var genericLogType = Enum.Parse(logType, "Generic");
        return Activator.CreateInstance(loggerType, "RobMerchant", genericLogType);
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
