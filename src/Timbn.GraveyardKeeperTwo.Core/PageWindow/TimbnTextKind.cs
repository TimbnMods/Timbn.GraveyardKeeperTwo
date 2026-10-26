namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The game's text styles a line made with <see cref="TimbnText"/> can take.</summary>
public enum TimbnTextKind
{
    /// <summary>The plain gray a game settings row names itself in.</summary>
    Label,

    /// <summary>The yellow of a save slot's name line, which the Mods window names every mod and setting in.</summary>
    Name,

    /// <summary>The smaller gray of a save slot's "Days played" line, used for versions and descriptions.</summary>
    Detail,

    /// <summary>The bright yellow a settings row shows its value in.</summary>
    Value,

    /// <summary>A window title's text.</summary>
    Title,
}
