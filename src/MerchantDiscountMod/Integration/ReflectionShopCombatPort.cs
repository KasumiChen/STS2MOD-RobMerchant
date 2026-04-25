using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MerchantDiscountMod.Combat;

namespace MerchantDiscountMod.Integration;

public sealed class ReflectionShopCombatPort : IShopCombatPort
{
    private const string Sts2AssemblyName = "sts2";
    private const string FakeMerchantEncounterTypeName =
        "MegaCrit.Sts2.Core.Models.Encounters.FakeMerchantEventEncounter";
    private const string CombatRoomTypeName = "MegaCrit.Sts2.Core.Rooms.CombatRoom";
    private const string ModelDbTypeName = "MegaCrit.Sts2.Core.Models.ModelDb";
    private const string RunManagerTypeName = "MegaCrit.Sts2.Core.Runs.RunManager";

    private readonly ReflectionMerchantShopContext context;
    private readonly Func<MerchantBattleRequest, object?, object?> combatRoomFactory;
    private readonly Func<object, object?, bool> combatRoomLauncher;

    public ReflectionShopCombatPort(ReflectionMerchantShopContext context)
        : this(context, CreateSts2CombatRoom, LaunchSts2CombatRoom)
    {
    }

    public ReflectionShopCombatPort(
        ReflectionMerchantShopContext context,
        Func<MerchantBattleRequest, object?, object?> combatRoomFactory)
        : this(context, combatRoomFactory, PushCombatRoomOntoRunState)
    {
    }

    private ReflectionShopCombatPort(
        ReflectionMerchantShopContext context,
        Func<MerchantBattleRequest, object?, object?> combatRoomFactory,
        Func<object, object?, bool> combatRoomLauncher)
    {
        this.context = context;
        this.combatRoomFactory = combatRoomFactory;
        this.combatRoomLauncher = combatRoomLauncher;
    }

    public void Launch(MerchantBattleRequest request)
    {
        context.RecordCombatLaunch(request);

        var runState = context.CurrentRunState;
        if (runState is null)
        {
            ThrowLaunchFailure("No captured run state is available for merchant combat launch.");
        }

        var combatRoom = combatRoomFactory(request, runState);
        if (combatRoom is null)
        {
            ThrowLaunchFailure("Could not create a merchant combat room.");
        }

        if (!combatRoomLauncher(combatRoom, runState))
        {
            ThrowLaunchFailure($"Could not enter merchant combat room from run state type {runState.GetType().FullName}.");
        }

        context.CaptureMerchantCombatRoom(combatRoom);
        MerchantDiscountDiagnostics.Info($"Merchant combat room transition requested: {combatRoom.GetType().FullName}.");
    }

    private static bool PushCombatRoomOntoRunState(object combatRoom, object? runState) =>
        runState is not null && ReflectionMemberAccess.TryInvoke(runState, "PushRoom", out _, combatRoom);

    private static bool LaunchSts2CombatRoom(object combatRoom, object? _)
    {
        var runManagerType = FindType(RunManagerTypeName);
        var runManager = runManagerType?
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null);
        if (runManager is null)
        {
            return false;
        }

        return InvokeEnterRoomWithoutExitingCurrentRoom(runManager, combatRoom, fadeToBlack: true);
    }

    private static bool InvokeEnterRoomWithoutExitingCurrentRoom(
        object runManager,
        object combatRoom,
        bool fadeToBlack)
    {
        var enterRoom = runManager
            .GetType()
            .GetMethods(ReflectionMemberAccess.InstanceMemberFlags)
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return method.Name == "EnterRoomWithoutExitingCurrentRoom"
                    && parameters.Length == 2
                    && parameters[0].ParameterType.IsInstanceOfType(combatRoom)
                    && parameters[1].ParameterType == typeof(bool);
            });

        try
        {
            var transitionTask = enterRoom?.Invoke(runManager, [combatRoom, fadeToBlack]) as Task;
            if (transitionTask is null)
            {
                return false;
            }

            ObserveCombatTransition(transitionTask);
            return true;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("Could not invoke RunManager.EnterRoomWithoutExitingCurrentRoom.", exception);
        }
        catch (MemberAccessException exception)
        {
            throw new InvalidOperationException("Could not access RunManager.EnterRoomWithoutExitingCurrentRoom.", exception);
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException(
                "RunManager.EnterRoomWithoutExitingCurrentRoom failed.",
                exception.InnerException ?? exception);
        }
    }

    private static void ObserveCombatTransition(Task transitionTask)
    {
        transitionTask.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                transitionTask.GetAwaiter().GetResult();
                MerchantDiscountDiagnostics.Info("Merchant combat room transition completed.");
            }
            catch (Exception exception)
            {
                MerchantDiscountDiagnostics.Error("Merchant combat room transition failed", exception);
                throw;
            }
        });
    }

    private static object? CreateSts2CombatRoom(MerchantBattleRequest request, object? runState)
    {
        _ = request;
        if (runState is null)
        {
            return null;
        }

        var sts2Assembly = LoadSts2Assembly();
        if (sts2Assembly is null)
        {
            return null;
        }

        var encounterType = sts2Assembly.GetType(FakeMerchantEncounterTypeName, throwOnError: false);
        var combatRoomType = sts2Assembly.GetType(CombatRoomTypeName, throwOnError: false);
        var modelDbType = sts2Assembly.GetType(ModelDbTypeName, throwOnError: false);
        if (encounterType is null || combatRoomType is null || modelDbType is null)
        {
            return null;
        }

        var encounter = CreateMutableModel(modelDbType, "Encounter", encounterType);
        if (encounter is null)
        {
            return null;
        }

        var constructor = combatRoomType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(ctor => ConstructorMatches(ctor, encounter, runState));
        return constructor?.Invoke([encounter, runState]);
    }

    private static object? CreateMutableModel(Type modelDbType, string accessorName, Type modelType)
    {
        var canonical = CreateCanonicalModel(modelDbType, accessorName, modelType);
        if (canonical is null)
        {
            return null;
        }

        return InvokeMutableClone(canonical, modelType);
    }

    private static object? CreateCanonicalModel(Type modelDbType, string accessorName, Type modelType)
    {
        var accessor = modelDbType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == accessorName
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 0);

        try
        {
            return accessor?.MakeGenericMethod(modelType).Invoke(null, []);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException($"Could not create ModelDb accessor for {modelType.FullName}.", exception);
        }
        catch (MemberAccessException exception)
        {
            throw new InvalidOperationException($"Could not access ModelDb accessor for {modelType.FullName}.", exception);
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException(
                $"ModelDb accessor failed for {modelType.FullName}.",
                exception.InnerException ?? exception);
        }
    }

    private static object? InvokeMutableClone(object canonical, Type modelType)
    {
        var toMutable = canonical
            .GetType()
            .GetMethod("ToMutable", BindingFlags.Public | BindingFlags.Instance, []);
        var mutableClone = canonical
            .GetType()
            .GetMethod("MutableClone", BindingFlags.Public | BindingFlags.Instance, []);
        var cloneMethod = toMutable ?? mutableClone;

        if (cloneMethod is null)
        {
            throw new InvalidOperationException(
                $"Canonical model {modelType.FullName} does not expose ToMutable or MutableClone.");
        }

        try
        {
            return cloneMethod.Invoke(canonical, []);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException($"Could not invoke mutable clone for {modelType.FullName}.", exception);
        }
        catch (MemberAccessException exception)
        {
            throw new InvalidOperationException($"Could not access mutable clone for {modelType.FullName}.", exception);
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException(
                $"Mutable clone failed for {modelType.FullName}.",
                exception.InnerException ?? exception);
        }
    }

    private static bool ConstructorMatches(ConstructorInfo constructor, object encounter, object runState)
    {
        var parameters = constructor.GetParameters();
        return parameters.Length == 2
            && parameters[0].ParameterType.IsInstanceOfType(encounter)
            && parameters[1].ParameterType.IsInstanceOfType(runState);
    }

    private static Assembly? LoadSts2Assembly()
    {
        try
        {
            return Assembly.Load(Sts2AssemblyName);
        }
        catch
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

    [DoesNotReturn]
    private static void ThrowLaunchFailure(string message)
    {
        var exception = new InvalidOperationException(message);
        MerchantDiscountDiagnostics.Error("Merchant combat launch failed", exception);
        throw exception;
    }
}
