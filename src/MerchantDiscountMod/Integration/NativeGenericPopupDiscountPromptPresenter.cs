using System.Reflection;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Integration;

internal sealed class NativeGenericPopupDiscountPromptPresenter : IReflectionDiscountPromptPresenter
{
    private Action? onAccepted;
    private Action? onDeclined;
    private Action? multiplayerVoteRefresh;
    private Action? multiplayerVotePromptClose;
    private object? multiplayerVoteContainer;
    private object? multiplayerVoteNoButton;
    private bool multiplayerVoteAccepted;

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

            if (IsCurrentRunMultiplayer())
            {
                if (TryInitializeMultiplayerVotePrompt(popup, request, onAccepted, onDeclined))
                {
                    MerchantDiscountDiagnostics.Info("Native discount prompt popup shown through multiplayer vote prompt.");
                    return true;
                }

                TryClearModalContainer();
                MerchantDiscountDiagnostics.Error(
                    "Native discount prompt multiplayer popup failed",
                    new InvalidOperationException("Could not initialize the multiplayer vote prompt."));
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

    private bool TryInitializeMultiplayerVotePrompt(
        object popup,
        DiscountPromptRequest request,
        Action onAccepted,
        Action onDeclined)
    {
        TryPrepareMultiplayerVoteBridge();

        var verticalPopup = GetChildNode(popup, "VerticalPopup");
        if (verticalPopup is null)
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt multiplayer popup is missing child node VerticalPopup.");
            return false;
        }

        var buttonParameterType = FindType("MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton");
        var yesDelegate = buttonParameterType is null
            ? null
            : ReflectionMemberAccess.CreateSingleParameterAction(
                buttonParameterType,
                this,
                nameof(AcceptMultiplayerVoteButton));
        var noDelegate = buttonParameterType is null
            ? null
            : ReflectionMemberAccess.CreateSingleParameterAction(
                buttonParameterType,
                this,
                nameof(DeclineButton));
        var confirmText = CreateLocString("main_menu_ui", "GENERIC_POPUP.confirm");
        var cancelText = CreateLocString("main_menu_ui", "GENERIC_POPUP.cancel");

        if (yesDelegate is null
            || noDelegate is null
            || confirmText is null
            || cancelText is null
            || !ReflectionMemberAccess.TryInvoke(verticalPopup, "InitYesButton", out _, confirmText, yesDelegate)
            || !ReflectionMemberAccess.TryInvoke(verticalPopup, "InitNoButton", out _, cancelText, noDelegate))
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt multiplayer popup could not initialize vote buttons.");
            return false;
        }

        this.onAccepted = onAccepted;
        this.onDeclined = onDeclined;
        multiplayerVoteAccepted = false;
        OverridePromptText(popup, request);
        TryDisconnectPopupYesClose(verticalPopup);
        TryAttachMultiplayerVoteContainer(verticalPopup);
        return true;
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

    private static void TryDisconnectPopupYesClose(object verticalPopup)
    {
        var yesButton = verticalPopup
            .GetType()
            .GetProperty("YesButton", ReflectionMemberAccess.InstanceMemberFlags)?
            .GetValue(verticalPopup);
        var buttonParameterType = FindType("MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton");
        var closeDelegate = buttonParameterType is null
            ? null
            : ReflectionMemberAccess.CreateSingleParameterAction(buttonParameterType, verticalPopup, "Close");
        var closeCallable = buttonParameterType is null || closeDelegate is null
            ? null
            : CreateGodotCallable(buttonParameterType, closeDelegate);
        var releasedSignal = GetClickableReleasedSignal();

        if (yesButton is null || closeCallable is null || releasedSignal is null)
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt multiplayer popup could not locate built-in yes close callback.");
            return;
        }

        if (!ReflectionMemberAccess.TryInvoke(yesButton, "Disconnect", out _, releasedSignal, closeCallable))
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt multiplayer popup could not disconnect built-in yes close callback.");
            return;
        }

        MerchantDiscountDiagnostics.Info("Native discount prompt multiplayer popup disconnected built-in yes close callback.");
    }

    private static object? CreateGodotCallable(Type parameterType, Delegate callback)
    {
        var callableType = FindType("Godot.Callable");
        var fromMethod = callableType?
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return method.Name == "From"
                    && method.IsGenericMethodDefinition
                    && parameters.Length == 1
                    && typeof(Delegate).IsAssignableFrom(parameters[0].ParameterType);
            });
        try
        {
            return fromMethod?.MakeGenericMethod(parameterType).Invoke(null, [callback]);
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

    private static object? GetClickableReleasedSignal()
    {
        var clickableType = FindType("MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl");
        var signalNameType = clickableType?.GetNestedType("SignalName", BindingFlags.Public | BindingFlags.NonPublic);
        return signalNameType?
            .GetField("Released", BindingFlags.Public | BindingFlags.Static)?
            .GetValue(null);
    }

    private void TryAttachMultiplayerVoteContainer(object verticalPopup)
    {
        var yesButton = verticalPopup
            .GetType()
            .GetProperty("YesButton", ReflectionMemberAccess.InstanceMemberFlags)?
            .GetValue(verticalPopup);
        var voteContainerType = FindType("MegaCrit.Sts2.Core.Nodes.CommonUi.NMultiplayerVoteContainer");
        var runState = GetCurrentRunState();
        var players = runState is null ? null : ReflectionMemberAccess.GetPropertyValue(runState, "Players");
        var playerVotedDelegateType = voteContainerType?
            .GetNestedType("PlayerVotedDelegate", BindingFlags.Public | BindingFlags.NonPublic);

        if (yesButton is null || voteContainerType is null || players is null || playerVotedDelegateType is null)
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt multiplayer vote container could not find required game objects.");
            return;
        }

        var votedDelegate = CreateSingleObjectParameterDelegate(
            playerVotedDelegateType,
            this,
            nameof(HasPlayerVotedForMerchantCombat));
        var voteContainer = Activator.CreateInstance(voteContainerType);
        if (voteContainer is null
            || votedDelegate is null
            || !ReflectionMemberAccess.TryInvoke(voteContainer, "Initialize", out _, votedDelegate, players)
            || !TryAddChild(yesButton, voteContainer))
        {
            MerchantDiscountDiagnostics.Warn("Native discount prompt multiplayer vote container could not be initialized.");
            return;
        }

        multiplayerVoteContainer = voteContainer;
        TryPlaceVoteContainer(voteContainer);
        RefreshMultiplayerVotes();
        RegisterVoteUiRefresh();
        RegisterVotePromptClose();
    }

    private static Delegate? CreateSingleObjectParameterDelegate(
        Type delegateType,
        object target,
        string methodName)
    {
        var invoke = delegateType.GetMethod("Invoke");
        var method = target.GetType().GetMethod(methodName, ReflectionMemberAccess.InstanceMemberFlags);
        var parameters = invoke?.GetParameters();
        if (invoke is null || method is null || parameters is null || parameters.Length != 1)
        {
            return null;
        }

        var parameter = Expression.Parameter(parameters[0].ParameterType, "value");
        var body = Expression.Call(
            Expression.Constant(target),
            method,
            Expression.Convert(parameter, typeof(object)));
        try
        {
            return Expression.Lambda(delegateType, body, parameter).Compile();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool TryAddChild(object parent, object child)
    {
        var method = parent
            .GetType()
            .GetMethods(ReflectionMemberAccess.InstanceMemberFlags)
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return method.Name == "AddChild"
                    && parameters.Length >= 1
                    && parameters[0].ParameterType.IsInstanceOfType(child);
            });
        if (method is null)
        {
            return false;
        }

        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = child;
        for (var index = 1; index < args.Length; index += 1)
        {
            args[index] = parameters[index].HasDefaultValue
                ? parameters[index].DefaultValue
                : ReflectionMemberAccess.CreateDefaultValue(parameters[index].ParameterType);
        }

        try
        {
            method.Invoke(parent, args);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (TargetInvocationException)
        {
            return false;
        }
    }

    private static void TryPlaceVoteContainer(object voteContainer)
    {
        var vector2Type = FindType("Godot.Vector2");
        var constructor = vector2Type?.GetConstructor([typeof(float), typeof(float)]);
        var position = constructor?.Invoke([180f, 0f]);
        if (position is not null)
        {
            var positionProperty = voteContainer.GetType().GetProperty("Position", ReflectionMemberAccess.InstanceMemberFlags);
            if (positionProperty is not null)
            {
                ReflectionMemberAccess.TrySetValue(voteContainer, positionProperty, position);
            }
        }
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

    private static bool IsCurrentRunMultiplayer()
    {
        var runManager = GetRunManager();
        var netService = runManager is null ? null : ReflectionMemberAccess.GetPropertyValue(runManager, "NetService");
        var netType = netService is null ? null : ReflectionMemberAccess.GetPropertyValue(netService, "Type");
        return netType?.ToString() is "Host" or "Client";
    }

    private static object? GetCurrentRunState()
    {
        var runManager = GetRunManager();
        return runManager is null ? null : ReflectionMemberAccess.InvokeParameterless(runManager, "DebugOnlyGetState");
    }

    private static object? GetRunManager()
    {
        var runManagerType = FindType("MegaCrit.Sts2.Core.Runs.RunManager");
        return runManagerType?
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null);
    }

    private static void TryPrepareMultiplayerVoteBridge()
    {
        var bridgeType = FindType("MerchantDiscountMod.Integration.MerchantCombatMultiplayerBridge");
        var prepare = bridgeType?.GetMethod(
            "PrepareForMultiplayerRun",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            prepare?.Invoke(null, []);
        }
        catch (ArgumentException exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat multiplayer vote bridge preparation failed: {exception.Message}");
        }
        catch (TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Warn(
                $"Merchant combat multiplayer vote bridge preparation failed: {exception.InnerException?.Message ?? exception.Message}");
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

    private void AcceptMultiplayerVoteButton(object? _)
    {
        if (multiplayerVoteAccepted)
        {
            RefreshMultiplayerVotes();
            return;
        }

        multiplayerVoteAccepted = true;
        MerchantDiscountDiagnostics.Info("Native discount prompt accepted as multiplayer vote.");
        onAccepted?.Invoke();
        TryDisableButton(multiplayerVoteNoButton);
        RefreshMultiplayerVotes();
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
        UnregisterVoteUiRefresh();
        UnregisterVotePromptClose();
        onAccepted = null;
        onDeclined = null;
        multiplayerVoteContainer = null;
        multiplayerVoteNoButton = null;
        multiplayerVoteAccepted = false;
    }

    private static void TryDisableButton(object? button)
    {
        if (button is null)
        {
            return;
        }

        if (!ReflectionMemberAccess.TryInvoke(button, "Disable", out _))
        {
            _ = ReflectionMemberAccess.TryInvoke(button, "SetEnabled", out _, false);
        }
    }

    private bool HasPlayerVotedForMerchantCombat(object? player)
    {
        var bridgeType = FindType("MerchantDiscountMod.Integration.MerchantCombatMultiplayerBridge");
        var method = bridgeType?.GetMethod(
            "PlayerHasPendingVote",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            return method?.Invoke(null, [player]) is true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat vote query failed: {exception.InnerException?.Message ?? exception.Message}");
            return false;
        }
    }

    private void RegisterVoteUiRefresh()
    {
        multiplayerVoteRefresh ??= RefreshMultiplayerVotes;
        var bridgeType = FindType("MerchantDiscountMod.Integration.MerchantCombatMultiplayerBridge");
        var method = bridgeType?.GetMethod(
            "RegisterVoteUiRefresh",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            method?.Invoke(null, [multiplayerVoteRefresh]);
        }
        catch (ArgumentException)
        {
        }
        catch (TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat vote UI registration failed: {exception.InnerException?.Message ?? exception.Message}");
        }
    }

    private void UnregisterVoteUiRefresh()
    {
        if (multiplayerVoteRefresh is null)
        {
            return;
        }

        var bridgeType = FindType("MerchantDiscountMod.Integration.MerchantCombatMultiplayerBridge");
        var method = bridgeType?.GetMethod(
            "UnregisterVoteUiRefresh",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            method?.Invoke(null, [multiplayerVoteRefresh]);
        }
        catch (ArgumentException)
        {
        }
        catch (TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat vote UI unregistration failed: {exception.InnerException?.Message ?? exception.Message}");
        }
    }

    private void RegisterVotePromptClose()
    {
        multiplayerVotePromptClose ??= CloseMultiplayerVotePrompt;
        var bridgeType = FindType("MerchantDiscountMod.Integration.MerchantCombatMultiplayerBridge");
        var method = bridgeType?.GetMethod(
            "RegisterVotePromptClose",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            method?.Invoke(null, [multiplayerVotePromptClose]);
        }
        catch (ArgumentException)
        {
        }
        catch (TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat vote prompt close registration failed: {exception.InnerException?.Message ?? exception.Message}");
        }
    }

    private void UnregisterVotePromptClose()
    {
        if (multiplayerVotePromptClose is null)
        {
            return;
        }

        var bridgeType = FindType("MerchantDiscountMod.Integration.MerchantCombatMultiplayerBridge");
        var method = bridgeType?.GetMethod(
            "UnregisterVotePromptClose",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            method?.Invoke(null, [multiplayerVotePromptClose]);
        }
        catch (ArgumentException)
        {
        }
        catch (TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat vote prompt close unregistration failed: {exception.InnerException?.Message ?? exception.Message}");
        }
    }

    private void CloseMultiplayerVotePrompt()
    {
        TryClearModalContainer();
        ResetCallbacks();
    }

    private void RefreshMultiplayerVotes()
    {
        var voteContainer = multiplayerVoteContainer;
        if (voteContainer is null)
        {
            return;
        }

        if (!ReflectionMemberAccess.TryInvoke(voteContainer, "RefreshPlayerVotes", out _, true))
        {
            MerchantDiscountDiagnostics.Warn("Merchant combat vote UI refresh could not call RefreshPlayerVotes.");
        }
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
