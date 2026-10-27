namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnLineKey
{
    internal TimbnLineKey(string value, int number)
    {
        Value = value;
        Number = number;
    }

    internal string Value { get; set; }

    internal int Number { get; }
}
