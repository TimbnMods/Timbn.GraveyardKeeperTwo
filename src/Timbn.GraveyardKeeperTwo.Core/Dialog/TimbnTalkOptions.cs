namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Holds optional settings for a talk added with Dialog.AddTalk or AddDynamicTalk.</summary>
public sealed class TimbnTalkOptions
{
    /// <summary>Sets the color of this talk's speech bubble in place of Dialog.BubbleColor. Leave it out to use the mod's color.</summary>
    public Color? BubbleColor { get; set; }

    /// <summary>
    /// Offers the talk only once per save. After the player starts it, the NPC stops offering it in that save, even
    /// after a reload. It is marked when the conversation starts and written to disk when the game next finishes
    /// saving. A player who quits without saving sees it again. Use <see cref="TimbnPluginDialog.ResetTalk"/> to
    /// offer it again.
    /// </summary>
    public bool Once { get; set; }
}
