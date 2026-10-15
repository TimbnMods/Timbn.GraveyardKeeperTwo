namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Draws the quest tree's lines into mod quests in their plugin's color.</summary>
internal static class TimbnQuestTree
{
    private static readonly AccessTools.FieldRef<QuestTreeConnector, QuestTreeConnectorElement> _angleDownLeft =
        AccessTools.FieldRefAccess<QuestTreeConnector, QuestTreeConnectorElement>("angleDownLeft");

    private static readonly AccessTools.FieldRef<QuestTreeConnector, QuestTreeConnectorElement> _angleDownRight =
        AccessTools.FieldRefAccess<QuestTreeConnector, QuestTreeConnectorElement>("angleDownRight");

    private static readonly AccessTools.FieldRef<QuestTreeConnector, QuestTreeConnectorElement> _verticalDown =
        AccessTools.FieldRefAccess<QuestTreeConnector, QuestTreeConnectorElement>("verticalDown");

    internal static void TintLine(QuestTreeConnector line, LazyScrollableElement? child, bool isBackground)
    {
        if (isBackground || line.ConnectorType != QuestTreeConnectorType.Active)
            return;

        if (child?.Data is not QuestTreeElementWidgetData { questData: { } quest } || TimbnQuests.LineColorOf(quest.id) is not { } colorOf)
            return;

        TimbnSafe.Run(
            () =>
            {
                var color = colorOf();
                Tint(_angleDownLeft(line), color);
                Tint(_angleDownRight(line), color);
                Tint(_verticalDown(line), color);
            },
            $"{nameof(TimbnQuestTree)}|Coloring the quest tree line into '{quest.id}'");
    }

    private static void Tint(QuestTreeConnectorElement? element, Color color)
    {
        if (element != null)
            element.image.color = color;
    }
}
