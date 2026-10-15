using FlowCanvas;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Reads the script graph an NPC runs for its main menu, the one its interaction event opens.</summary>
internal static class TimbnNpcMenu
{
    internal const string InteractionEvent = "interaction";

    private static readonly AccessTools.FieldRef<WgoDataScriptsManager, Dictionary<SGuid, WgoDataScript>> _runningScripts =
        AccessTools.FieldRefAccess<WgoDataScriptsManager, Dictionary<SGuid, WgoDataScript>>("runningScripts");

    private static readonly AccessTools.FieldRef<CustomFlowScript, FlowScriptController> _controller =
        AccessTools.FieldRefAccess<CustomFlowScript, FlowScriptController>("flowScriptController");

    internal static Flow_MultiAnswer? MainMenu(WgoData npc)
    {
        if (GraphOf(npc) is not { } graph)
            return null;

        var start = graph.allNodes.OfType<CustomEvent>().FirstOrDefault(e => string.Equals(e.eventName.value, InteractionEvent, StringComparison.OrdinalIgnoreCase));
        if (start is null)
            return null;

        var seen = new HashSet<Node> { start };
        var queue = new Queue<Node>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node is Flow_MultiAnswer menu)
                return menu;

            foreach (var connection in node.outConnections)
            {
                if (connection is BinderConnection { sourcePort: FlowOutput } && connection.targetNode is { } next && seen.Add(next))
                    queue.Enqueue(next);
            }
        }

        return null;
    }

    internal static bool Offers(Flow_MultiAnswer menu, string answerId) => menu.AnswerOptions.Exists(option => option.id == answerId);

    private static Graph? GraphOf(WgoData npc)
    {
        if (WgoDataScriptsManager.Instance is not { } scripts
            || !_runningScripts(scripts).TryGetValue(npc.UniqueId, out var script)
            || script == null
            || _controller(script) is not { } controller
            || controller == null)
        {
            return null;
        }

        return controller.graph;
    }
}
