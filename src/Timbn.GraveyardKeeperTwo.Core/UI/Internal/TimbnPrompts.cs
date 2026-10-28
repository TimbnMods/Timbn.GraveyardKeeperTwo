using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The small windows Core opens for a mod or for itself, a line of text to type, a yes or no question and a message.
/// Each one is a <see cref="TimbnPage"/> in the page popup, so it is built from the same pieces as every other page.
/// </summary>
internal static class TimbnPrompts
{
    private static readonly TimbnLength _width = TimbnLength.Pixels(260);

    internal static IDisposable Prompt(string title, string current, string placeholder, Action<string> onSave, Action? onCancel, ManualLogSource logger, Action? closed = null, bool? wholeNumbers = null, int? characterLimit = null, TimbnLength? width = null, bool? pauseGame = null)
    {
        var text = current;
        var prompt = new TimbnPage(title, (page, close) =>
        {
            page.Add(new TimbnTextBox(() => text, value => text = value, wholeNumbers) { SetsWhileTyping = true, FocusWhenShown = true, Placeholder = placeholder, CharacterLimit = characterLimit });
            page.ButtonFooter(footer =>
            {
                footer.AddPrimarySpecial(LLBase.L(TimbnGameIds.Texts.BtnOk), () =>
                {
                    close();
                    TimbnSafe.Run(() => onSave(text), logger, $"{nameof(TimbnPrompts)}|The prompt's save handler");
                });
                footer.AddSecondary(LLBase.L(TimbnGameIds.Texts.BtnCancel), () => Answer(close, onCancel, logger));
            });
        })
        { Width = width ?? _width, PauseGame = pauseGame ?? TimbnGame.IsInGame };
        return TimbnPageWindow.Show(prompt, logger, closed, overOthers: true);
    }

    internal static IDisposable Confirm(string title, string message, Action onYes, Action? onNo, ManualLogSource logger, Action? closed = null, TimbnLength? width = null, bool? pauseGame = null)
    {
        var confirm = new TimbnPage(title, (page, close) =>
        {
            page.Add(new TimbnText(message) { Wrap = true, Align = TimbnAlign.Center });
            page.ButtonFooter(footer =>
            {
                footer.AddPrimary(LLBase.L(TimbnGameIds.Texts.BtnYes), () => Answer(close, onYes, logger));
                footer.AddSecondary(LLBase.L(TimbnGameIds.Texts.BtnNo), () => Answer(close, onNo, logger));
            });
        })
        { Width = width ?? _width, PauseGame = pauseGame ?? TimbnGame.IsInGame };
        return TimbnPageWindow.Show(confirm, logger, closed, overOthers: true);
    }

    internal static IDisposable Message(string title, string message, Action? onOk, ManualLogSource logger, Action? closed = null, TimbnLength? width = null, bool? pauseGame = null)
    {
        var notice = new TimbnPage(title, (page, close) =>
        {
            page.Add(new TimbnText(message) { Wrap = true, Align = TimbnAlign.Center });
            page.ButtonFooter(footer => footer.AddPrimary(LLBase.L(TimbnGameIds.Texts.BtnOk), () => Answer(close, onOk, logger)));
        }, Handler(onOk, logger))
        { Width = width ?? _width, PauseGame = pauseGame ?? TimbnGame.IsInGame };
        return TimbnPageWindow.Show(notice, logger, closed, overOthers: true);
    }

    private static void Answer(Action close, Action? handler, ManualLogSource logger)
    {
        close();
        if (handler != null)
            TimbnSafe.Run(handler, logger, $"{nameof(TimbnPrompts)}|The answer handler");
    }

    private static Action? Handler(Action? handler, ManualLogSource logger) =>
        handler == null ? null : () => TimbnSafe.Run(handler, logger, $"{nameof(TimbnPrompts)}|The dismiss handler");
}
