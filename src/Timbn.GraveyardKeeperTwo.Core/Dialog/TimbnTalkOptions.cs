namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Optional settings for a talk added with the plugin's Dialog.AddTalk.</summary>
public sealed class TimbnTalkOptions
{
    /// <summary>The color of this talk's speech bubble in place of the plugin's Dialog.BubbleColor. Leave it out to use the plugin's color.</summary>
    public Color? BubbleColor { get; set; }

    /// <summary>
    /// Makes the talk a one-shot. Once the player starts it in a save, the NPC stops offering it in that save,
    /// even after a reload. It is marked when the conversation starts and kept with the game's next save, so a
    /// player who quits without saving sees it again, like the game's own progress. See
    /// <see cref="TimbnPluginDialog.ResetOnceTalk"/> to offer it again.
    /// </summary>
    public bool Once { get; set; }
}
