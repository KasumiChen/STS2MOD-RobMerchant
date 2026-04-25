using System.Reflection;

namespace MerchantDiscountMod.Integration;

public sealed class VerifiedHookTarget
{
    public VerifiedHookTarget(string declaringTypeName, string methodName, params string[] parameterTypeNames)
    {
        DeclaringTypeName = declaringTypeName;
        MethodName = methodName;
        ParameterTypeNames = parameterTypeNames;
    }

    public string DeclaringTypeName { get; }

    public string MethodName { get; }

    public IReadOnlyList<string> ParameterTypeNames { get; }

    public Type? ResolveDeclaringType() => ResolveType(DeclaringTypeName);

    public Type?[] ResolveParameterTypes() => ParameterTypeNames.Select(ResolveType).ToArray();

    public MethodInfo? ResolveMethod()
    {
        var declaringType = ResolveDeclaringType();
        if (declaringType is null)
        {
            return null;
        }

        var directParameterTypes = ResolveParameterTypes();
        if (directParameterTypes.All(parameterType => parameterType is not null))
        {
            var directMethod = declaringType.GetMethod(
                MethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null,
                directParameterTypes!,
                null);
            if (directMethod is not null)
            {
                return directMethod;
            }
        }

        return declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == MethodName && ParametersMatch(method.GetParameters()));
    }

    public override string ToString()
    {
        return $"{DeclaringTypeName}.{MethodName}({string.Join(", ", ParameterTypeNames)})";
    }

    private static Type? ResolveType(string typeName)
    {
        var genericType = ResolveGenericType(typeName);
        if (genericType is not null)
        {
            return genericType;
        }

        var type = Type.GetType(typeName, throwOnError: false);
        if (type is not null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName, throwOnError: false);
            if (type is not null)
            {
                return type;
            }
        }

        if (typeName.StartsWith("MegaCrit.", StringComparison.Ordinal))
        {
            type = TryLoadFromAssembly(typeName, "sts2");
        }
        else if (typeName.StartsWith("Godot.", StringComparison.Ordinal))
        {
            type = TryLoadFromAssembly(typeName, "GodotSharp");
        }

        return type;
    }

    private bool ParametersMatch(ParameterInfo[] parameters)
    {
        if (parameters.Length != ParameterTypeNames.Count)
        {
            return false;
        }

        for (var index = 0; index < parameters.Length; index += 1)
        {
            if (!ParameterMatches(parameters[index].ParameterType, ParameterTypeNames[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParameterMatches(Type parameterType, string expectedTypeName)
    {
        var resolvedType = ResolveType(expectedTypeName);
        if (resolvedType is not null)
        {
            return parameterType == resolvedType;
        }

        if (!parameterType.IsGenericType)
        {
            return parameterType.FullName == expectedTypeName;
        }

        var genericStart = expectedTypeName.IndexOf('<', StringComparison.Ordinal);
        if (genericStart < 0)
        {
            return false;
        }

        var genericTypeName = expectedTypeName[..genericStart];
        if (parameterType.GetGenericTypeDefinition().FullName != genericTypeName)
        {
            return false;
        }

        var expectedArguments = SplitGenericArguments(
            expectedTypeName[(genericStart + 1)..^1]);
        var actualArguments = parameterType.GetGenericArguments();
        return expectedArguments.Count == actualArguments.Length
            && actualArguments
                .Select((argument, argumentIndex) => ParameterMatches(argument, expectedArguments[argumentIndex]))
                .All(matches => matches);
    }

    private static Type? ResolveGenericType(string typeName)
    {
        var genericStart = typeName.IndexOf('<', StringComparison.Ordinal);
        if (genericStart < 0 || !typeName.EndsWith('>'))
        {
            return null;
        }

        var genericDefinition = ResolveType(typeName[..genericStart]);
        if (genericDefinition is null)
        {
            return null;
        }

        var genericArguments = SplitGenericArguments(typeName[(genericStart + 1)..^1])
            .Select(ResolveType)
            .ToArray();
        if (genericArguments.Any(argument => argument is null))
        {
            return null;
        }

        return genericDefinition.MakeGenericType(genericArguments!);
    }

    private static IReadOnlyList<string> SplitGenericArguments(string arguments)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;

        for (var index = 0; index < arguments.Length; index += 1)
        {
            var character = arguments[index];
            if (character == '<')
            {
                depth += 1;
            }
            else if (character == '>')
            {
                depth -= 1;
            }
            else if (character == ',' && depth == 0)
            {
                result.Add(arguments[start..index].Trim());
                start = index + 1;
            }
        }

        result.Add(arguments[start..].Trim());
        return result;
    }

    private static Type? TryLoadFromAssembly(string typeName, string assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName).GetType(typeName, throwOnError: false);
        }
        catch
        {
            return null;
        }
    }
}
