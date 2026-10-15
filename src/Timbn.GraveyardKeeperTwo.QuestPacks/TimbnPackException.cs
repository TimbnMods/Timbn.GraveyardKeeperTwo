using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.QuestPacks;

internal sealed class TimbnPackException : Exception
{
    internal TimbnPackException(string message)
        : base(message)
    {
    }

    internal static TimbnPackException At(JToken token, string message) => new($"{LineOf(token)}{message}");

    internal static string LineOf(JToken token) =>
        token is IJsonLineInfo info && info.HasLineInfo() ? $"line {info.LineNumber}, " : "";
}
