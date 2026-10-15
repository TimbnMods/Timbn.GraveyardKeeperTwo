using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Subscribes to a game event named by its type and name, so a mod passes a handler instead of writing the += and -=
/// itself. The handler's parameters are checked against the event's when the hook is made, and a handler of Action
/// type is turned into the event's own delegate type when the game declares one.
/// </summary>
internal static class TimbnEventHooks
{
    internal static (Action<TDelegate> Add, Action<TDelegate> Remove) Find<TDelegate>(Type type, string eventName, Func<object?>? source)
        where TDelegate : Delegate
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | (source is null ? BindingFlags.Static : BindingFlags.Instance);
        var info = type.GetEvent(eventName, flags)
            ?? throw new ArgumentException($"{type.Name} has no {(source is null ? "static" : "instance")} event named {eventName}.");
        var eventType = info.EventHandlerType!;
        var expected = Parameters(eventType);
        if (eventType.GetMethod("Invoke")!.ReturnType != typeof(void) || !expected.SequenceEqual(Parameters(typeof(TDelegate))))
            throw new ArgumentException($"{type.Name}.{eventName} passes ({string.Join(", ", expected.Select(p => p.Name))}), so its handler has to take exactly those.");

        var add = info.GetAddMethod(true)!;
        var remove = info.GetRemoveMethod(true)!;
        Dictionary<TDelegate, Delegate> hooked = [];
        return (
            handler =>
            {
                var hook = eventType == typeof(TDelegate) ? handler : Delegate.CreateDelegate(eventType, handler.Target, handler.Method);
                add.Invoke(source?.Invoke(), [hook]);
                hooked[handler] = hook;
            },
            handler =>
            {
                if (hooked.Remove(handler, out var hook))
                    remove.Invoke(source?.Invoke(), [hook]);
            }
        );
    }

    private static Type[] Parameters(Type delegateType) =>
        [.. delegateType.GetMethod("Invoke")!.GetParameters().Select(parameter => parameter.ParameterType)];
}
