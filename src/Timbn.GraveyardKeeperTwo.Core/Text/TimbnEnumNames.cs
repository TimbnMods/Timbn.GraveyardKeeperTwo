using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// A mod names an enum value with a lang line keyed by the enum's full type name and the value, such as
// Timbn.GraveyardKeeperTwo.ChestLabels.ChestLabelSize.ExtraLarge = Extra Large, and a value with no line shows its name as written.
internal static class TimbnEnumNames
{
    internal static string For(Type enumType, string value)
    {
        var key = $"{enumType.FullName}.{value}";
        return LLBase.HasL(key) ? LLBase.L(key) : value;
    }
}
