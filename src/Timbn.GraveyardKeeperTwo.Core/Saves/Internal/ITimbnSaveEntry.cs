using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal interface ITimbnSaveEntry
{
    string Id { get; }

    void Load(JToken? section);

    JToken Serialize();

    void Reset();
}
