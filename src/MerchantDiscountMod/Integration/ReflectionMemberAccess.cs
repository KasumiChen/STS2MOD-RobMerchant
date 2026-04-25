using System.Collections;
using System.Reflection;

namespace MerchantDiscountMod.Integration;

internal static class ReflectionMemberAccess
{
    public const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static object? GetPropertyValue(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, InstanceMemberFlags);
        if (property is null || property.GetIndexParameters().Length != 0)
        {
            return null;
        }

        try
        {
            return property.GetValue(instance);
        }
        catch (TargetException)
        {
            return null;
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    public static IEnumerable<object> EnumerateProperty(object instance, string propertyName)
    {
        if (GetPropertyValue(instance, propertyName) is not IEnumerable values)
        {
            yield break;
        }

        foreach (var value in values)
        {
            if (value is not null)
            {
                yield return value;
            }
        }
    }

    public static object? InvokeParameterless(object instance, string methodName) =>
        Invoke(instance, methodName);

    public static object? InvokeStaticParameterless(Type type, string methodName)
    {
        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == methodName && method.GetParameters().Length == 0);

        try
        {
            return method?.Invoke(null, []);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (MethodAccessException)
        {
            return null;
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    public static Delegate? CreateSingleParameterAction(Type parameterType, object target, string methodName)
    {
        var method = target
            .GetType()
            .GetMethod(methodName, InstanceMemberFlags);
        if (method is null)
        {
            return null;
        }

        var actionType = typeof(Action<>).MakeGenericType(parameterType);
        try
        {
            var directDelegate = Delegate.CreateDelegate(actionType, target, method, throwOnBindFailure: false);
            if (directDelegate is not null)
            {
                return directDelegate;
            }
        }
        catch (ArgumentException)
        {
        }
        catch (MemberAccessException)
        {
        }

        return CreateReflectionSingleParameterAction(parameterType, target, method);
    }

    private static Delegate? CreateReflectionSingleParameterAction(Type parameterType, object target, MethodInfo method)
    {
        var createMethod = typeof(ReflectionMemberAccess)
            .GetMethod(nameof(CreateReflectionSingleParameterActionCore), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(parameterType);

        try
        {
            return createMethod?.Invoke(null, [target, method]) as Delegate;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    private static Action<T> CreateReflectionSingleParameterActionCore<T>(object target, MethodInfo method) =>
        value =>
        {
            var parameters = method.GetParameters();
            var args = parameters.Length == 0 ? [] : new object?[] { value };
            method.Invoke(target, args);
        };

    public static object? Invoke(object instance, string methodName, params object?[] args)
    {
        return TryInvoke(instance, methodName, out var result, args)
            ? result
            : null;
    }

    public static object? InvokeRequired(object instance, string methodName, params object?[] args)
    {
        var method = FindMethod(instance.GetType(), methodName, args)
            ?? throw new MissingMethodException(instance.GetType().FullName, methodName);

        try
        {
            return method.Invoke(instance, args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"{instance.GetType().FullName}.{methodName} failed.",
                exception.InnerException);
        }
    }

    public static bool TryInvoke(object instance, string methodName, out object? result, params object?[] args)
    {
        var method = FindMethod(instance.GetType(), methodName, args);
        if (method is null)
        {
            result = null;
            return false;
        }

        try
        {
            result = method.Invoke(instance, args);
            return true;
        }
        catch (ArgumentException)
        {
            result = null;
            return false;
        }
        catch (TargetException)
        {
            result = null;
            return false;
        }
        catch (TargetInvocationException)
        {
            result = null;
            return false;
        }
    }

    public static void TrySetValue(object instance, FieldInfo field, object? value)
    {
        try
        {
            field.SetValue(instance, value);
        }
        catch (ArgumentException)
        {
        }
        catch (FieldAccessException)
        {
        }
        catch (TargetException)
        {
        }
    }

    public static void TrySetValue(object instance, PropertyInfo property, object? value)
    {
        try
        {
            property.SetValue(instance, value);
        }
        catch (ArgumentException)
        {
        }
        catch (MethodAccessException)
        {
        }
        catch (TargetException)
        {
        }
        catch (TargetInvocationException)
        {
        }
    }

    public static object? CreateDefaultValue(Type type)
    {
        var targetType = Nullable.GetUnderlyingType(type) ?? type;
        if (targetType.IsEnum)
        {
            return Enum.ToObject(targetType, 0);
        }

        return targetType.IsValueType
            ? Activator.CreateInstance(targetType)
            : null;
    }

    private static MethodInfo? FindMethod(Type instanceType, string methodName, object?[] args) =>
        instanceType
            .GetMethods(InstanceMemberFlags)
            .Where(method => method.Name == methodName)
            .FirstOrDefault(method => ParametersMatch(method.GetParameters(), args));

    private static bool ParametersMatch(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length != args.Length)
        {
            return false;
        }

        for (var index = 0; index < parameters.Length; index += 1)
        {
            var arg = args[index];
            if (arg is null)
            {
                continue;
            }

            if (!parameters[index].ParameterType.IsInstanceOfType(arg))
            {
                return false;
            }
        }

        return true;
    }
}
