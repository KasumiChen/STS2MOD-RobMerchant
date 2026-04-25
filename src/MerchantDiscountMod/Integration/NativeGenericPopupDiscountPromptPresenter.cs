using System.Reflection;
using System.Threading.Tasks;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Integration;

internal sealed class NativeGenericPopupDiscountPromptPresenter : IReflectionDiscountPromptPresenter
{
    private Action? onAccepted;
    private Action? onDeclined;

    public bool TryShow(DiscountPromptRequest request, Action onAccepted, Action onDeclined)
    {
        ResetCallbacks();

        try
        {
            var popup = CreateGenericPopup();
            if (popup is null)
            {
                MerchantDiscountDiagnostics.Warn("Native discount prompt popup scene could not be created.");
                return false;
            }

            if (!TryAddToModalContainer(popup))
            {
                MerchantDiscountDiagnostics.Warn("Native discount prompt popup could not be added to NModalContainer.");
                return false;
            }

            var confirmationTask = WaitForConfirmation(popup);
            if (confirmationTask is null)
            {
                TryClearModalContainer();
                MerchantDiscountDiagnostics.Warn("Native discount prompt popup could not start WaitForConfirmation.");
                return false;
            }

            this.onAccepted = onAccepted;
            this.onDeclined = onDeclined;
            OverridePromptText(popup, request);
            ObserveConfirmation(confirmationTask);

            MerchantDiscountDiagnostics.Info("Native discount prompt popup shown through WaitForConfirmation.");
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or MemberAccessException
            or TargetInvocationException)
        {
            ResetCallbacks();
            MerchantDiscountDiagnostics.Error("Native discount prompt popup failed", exception);
            return false;
        }
    }

    private static object? CreateGenericPopup()
    {
        var popupType = FindType("MegaCrit.Sts2.Core.Nodes.Multiplayer.NGenericPopup");
        if (popupType is null)
        {
            return null;
        }

        return ReflectionMemberAccess.InvokeStaticParameterless(popupType, "Create");
    }

    private static object? GetChildNode(object node, string path)
    {
        var nodePath = CreateNodePath(path);
        if (nodePath is null)
        {
            return null;
        }

        var method = node
            .GetType()
            .GetMethods(ReflectionMemberAccess.InstanceMemberFlags)
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return method.Name == "GetNode"
                    && !method.IsGenericMethod
                    && parameters.Length == 1
                    && parameters[0].ParameterType.IsInstanceOfType(nodePath);
            });

        try
        {
            return method?.Invoke(node, [nodePath]);
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

    private static object? CreateNodePath(string path)
    {
        var nodePathType = FindType("Godot.NodePath");
        if (nodePathType is null)
        {
            return null;
        }

        var constructor = nodePathType.GetConstructor([typeof(string)]);
        if (constructor is not null)
        {
            return constructor.Invoke([path]);
        }

        var implicitStringOperator = nodePathType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return method.Name == "op_Implicit"
                    && method.ReturnType == nodePathType
                    && parameters.Length == 1
                    && parameters[0].ParameterType == typeof(string);
            });

        return implicitStringOperator?.Invoke(null, [path]);
    }

    private static Task<bool>? WaitForConfirmation(object popup)
    {
        var placeholderText = CreateLocString("main_menu_ui", "GENERIC_POPUP.confirm");
        var cancelText = CreateLocString("main_menu_ui", "GENERIC_POPUP.cancel");
        var confirmText = CreateLocString("main_menu_ui", "GENERIC_POPUP.confirm");
        var waitMethod = popup
            .GetType()
            .GetMethods(ReflectionMemberAccess.InstanceMemberFlags)
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return method.Name == "WaitForConfirmation"
                    && parameters.Length == 4
                    && placeholderText is not null
                    && parameters.All(parameter => parameter.ParameterType.IsInstanceOfType(placeholderText));
            });

        try
        {
            if (placeholderText is null || cancelText is null || confirmText is null || waitMethod is null)
            {
                return null;
            }

            return waitMethod.Invoke(
                popup,
                [placeholderText, placeholderText, cancelText, confirmText]) as Task<bool>;
        }
        catch (ArgumentException exception)
        {
            MerchantDiscountDiagnostics.Error("Native discount prompt WaitForConfirmation argument failure", exception);
            return null;
        }
        catch (MemberAccessException exception)
        {
            MerchantDiscountDiagnostics.Error("Native discount prompt WaitForConfirmation access failure", exception);
            return null;
        }
        catch (TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Error("Native discount prompt WaitForConfirmation invocation failure", exception);
            return null;
        }
    }

    private static void OverridePromptText(object popup, DiscountPromptRequest request)
    {
        var verticalPopup = GetChildNode(popup, "VerticalPopup");
        if (verticalPopup is null)
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt popup is missing child node VerticalPopup.");
            return;
        }

        if (!ReflectionMemberAccess.TryInvoke(verticalPopup, "SetText", out _, request.Title, request.Body))
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt popup could not override title/body text.");
        }

        TrySetButtonText(verticalPopup, "YesButton", request.AcceptText);
        TrySetButtonText(verticalPopup, "NoButton", request.DeclineText);
    }

    private static bool TrySetButtonText(object verticalPopup, string buttonPropertyName, string text)
    {
        var button = verticalPopup
            .GetType()
            .GetProperty(buttonPropertyName, ReflectionMemberAccess.InstanceMemberFlags)?
            .GetValue(verticalPopup);

        return button is not null && ReflectionMemberAccess.TryInvoke(button, "SetText", out _, text);
    }

    private static object? CreateLocString(string locTable, string locEntryKey)
    {
        var locStringType = FindType("MegaCrit.Sts2.Core.Localization.LocString");
        var constructor = locStringType?.GetConstructor([typeof(string), typeof(string)]);
        try
        {
            return constructor?.Invoke([locTable, locEntryKey]);
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

    private static bool TryAddToModalContainer(object popup)
    {
        var modalContainerType = FindType("MegaCrit.Sts2.Core.Nodes.CommonUi.NModalContainer");
        var modalContainer = modalContainerType?
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null);
        var addMethod = modalContainerType?
            .GetMethods(ReflectionMemberAccess.InstanceMemberFlags)
            .FirstOrDefault(method => method.Name == "Add" && method.GetParameters().Length == 2);

        if (modalContainer is null || addMethod is null)
        {
            return false;
        }

        try
        {
            addMethod.Invoke(modalContainer, [popup, true]);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (TargetException)
        {
            return false;
        }
        catch (TargetInvocationException)
        {
            return false;
        }
    }

    private static void TryClearModalContainer()
    {
        try
        {
            var modalContainerType = FindType("MegaCrit.Sts2.Core.Nodes.CommonUi.NModalContainer");
            var modalContainer = modalContainerType?
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?
                .GetValue(null);
            var clearMethod = modalContainerType?.GetMethod("Clear", ReflectionMemberAccess.InstanceMemberFlags);
            clearMethod?.Invoke(modalContainer, []);
        }
        catch (ArgumentException)
        {
        }
        catch (TargetException)
        {
        }
        catch (TargetInvocationException)
        {
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

    private void AcceptButton(object? _)
    {
        var accepted = onAccepted;
        ResetCallbacks();
        TryClearModalContainer();
        MerchantDiscountDiagnostics.Info("Native discount prompt accepted.");
        accepted?.Invoke();
    }

    private void DeclineButton(object? _)
    {
        var declined = onDeclined;
        ResetCallbacks();
        TryClearModalContainer();
        MerchantDiscountDiagnostics.Info("Native discount prompt declined.");
        declined?.Invoke();
    }

    private void ResetCallbacks()
    {
        onAccepted = null;
        onDeclined = null;
    }

    private void ObserveConfirmation(Task<bool> confirmationTask)
    {
        confirmationTask.GetAwaiter().OnCompleted(() => CompleteConfirmation(confirmationTask));
    }

    private void CompleteConfirmation(Task<bool> confirmationTask)
    {
        try
        {
            if (confirmationTask.GetAwaiter().GetResult())
            {
                AcceptButton(null);
            }
            else
            {
                DeclineButton(null);
            }
        }
        catch (Exception exception)
        {
            ResetCallbacks();
            TryClearModalContainer();
            MerchantDiscountDiagnostics.Error("Native discount prompt confirmation failed", exception);
            throw;
        }
    }
}
