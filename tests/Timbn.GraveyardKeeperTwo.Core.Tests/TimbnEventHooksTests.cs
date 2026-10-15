namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnEventHooksTests
{
    public delegate void OwnDelegate(string id, bool silent);

    [Fact]
    public void AStaticEventIsHookedAndUnhooked()
    {
        List<string> heard = [];
        Action<string, bool> handler = (id, _) => heard.Add(id);
        (var add, var remove) = TimbnEventHooks.Find<Action<string, bool>>(typeof(Source), nameof(Source.Unlocked), null);

        add(handler);
        Source.RaiseUnlocked("a");
        remove(handler);
        Source.RaiseUnlocked("b");

        Assert.Equal(["a"], heard);
    }

    [Fact]
    public void AnEventOfItsOwnDelegateTypeTakesAMatchingAction()
    {
        List<string> heard = [];
        Action<string, bool> handler = (id, _) => heard.Add(id);
        (var add, var remove) = TimbnEventHooks.Find<Action<string, bool>>(typeof(Source), nameof(Source.Custom), null);

        add(handler);
        Source.RaiseCustom("a");
        remove(handler);
        Source.RaiseCustom("b");

        Assert.Equal(["a"], heard);
    }

    [Fact]
    public void AnInstanceEventIsHookedOnTheSourceObject()
    {
        var source = new Source();
        var heard = 0;
        Action handler = () => heard++;
        (var add, var remove) = TimbnEventHooks.Find<Action>(typeof(Source), nameof(Source.Changed), () => source);

        add(handler);
        source.RaiseChanged();
        remove(handler);
        source.RaiseChanged();

        Assert.Equal(1, heard);
    }

    [Fact]
    public void AMissingEventIsRefused() =>
        Assert.Throws<ArgumentException>(() => TimbnEventHooks.Find<Action>(typeof(Source), "Nope", null));

    [Fact]
    public void AHandlerWithTheWrongParametersIsRefused() =>
        Assert.Throws<ArgumentException>(() => TimbnEventHooks.Find<Action<int>>(typeof(Source), nameof(Source.Unlocked), null));

    private sealed class Source
    {
        public static event Action<string, bool>? Unlocked;

        public static event OwnDelegate? Custom;

        public event Action? Changed;

        public static void RaiseUnlocked(string id) => Unlocked?.Invoke(id, false);

        public static void RaiseCustom(string id) => Custom?.Invoke(id, false);

        public void RaiseChanged() => Changed?.Invoke();
    }
}
