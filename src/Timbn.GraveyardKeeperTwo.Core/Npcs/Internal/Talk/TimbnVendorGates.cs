using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Keeps a vendor's trade window shut while a plugin says the vendor is away.</summary>
internal static class TimbnVendorGates
{
    private static readonly List<Gate> _gates = [];

    internal static IDisposable Add(string vendorId, Func<bool> isClosed, string? lineKey, ManualLogSource logger)
    {
        var gate = new Gate(vendorId, isClosed, lineKey, logger);
        _gates.Add(gate);
        return new TimbnUndo(() => _gates.Remove(gate));
    }

    internal static bool LetOpen(UIVendorWindowData data)
    {
        var vendorId = data?.Vendor?.Definition?.id;
        if (vendorId == null)
            return true;

        foreach (var gate in _gates.ToList())
        {
            if (gate.VendorId != vendorId)
                continue;

            var closed = false;
            TimbnSafe.Run(() => closed = gate.IsClosed(), gate.Logger, $"{nameof(TimbnVendorGates)}|{TimbnSafe.Describe(gate.IsClosed)}");
            if (!closed)
                continue;

            // The game brings its menu back from OnWindowClosed, so it waits until the vendor has finished speaking.
            if (gate.LineKey == null || !TimbnDialog.Speak(vendorId, gate.LineKey, () => data!.OnWindowClosed?.Invoke()))
            {
                if (gate.LineKey != null)
                {
                    TimbnLocale.WarnIfMissing(gate.LineKey);
                    LazySingleton<UINotificator>.Instance?.ShowSimpleTextNotification(gate.LineKey);
                }

                data!.OnWindowClosed?.Invoke();
            }

            return false;
        }

        return true;
    }

    private sealed class Gate
    {
        public Gate(string vendorId, Func<bool> isClosed, string? lineKey, ManualLogSource logger)
        {
            VendorId = vendorId;
            IsClosed = isClosed;
            LineKey = lineKey;
            Logger = logger;
        }

        public string VendorId { get; }

        public Func<bool> IsClosed { get; }

        public string? LineKey { get; }

        public ManualLogSource Logger { get; }
    }
}
