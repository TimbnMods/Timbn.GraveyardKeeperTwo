namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Acts on the player character. Every change is undone when the mod unloads or the player returns to the main
/// menu. See <see cref="TimbnPlayer"/> to read where the player is. Reach it through the mod's Player property.
/// </summary>
public sealed class TimbnPluginPlayer
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginPlayer(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Takes the player's control away, the way the game does during its cutscenes and conversations. The player
    /// cannot walk, interact, attack, use the hotbar or open the game's menus with their keys. Nothing can push them.
    /// Hotkeys checked with <see cref="TimbnInput.IsDownWithControl"/> stay quiet. Each call adds its own reason to
    /// the game's list of reasons the player is held. The game gives control back only once every reason has let go.
    /// Several mods can take control at once. A game cutscene that ends in the meantime never frees the
    /// player. Read your own keys with <see cref="TimbnInput.IsDownInGame(LazyBearTechnology.GameKey)"/> or
    /// <see cref="TimbnInput.IsDownInGame(KeyboardShortcut)"/> while it is held. Use
    /// <see cref="TimbnPlayer.GameHasControl"/> to notice the game stepping in.
    /// </summary>
    /// <example>
    /// <code>
    /// _control = Player.TakeControl();
    /// // later
    /// _control?.Dispose();
    /// </code>
    /// </example>
    /// <returns>A handle that gives control back when disposed. It is also released when the mod unloads and on the way to the main menu.</returns>
    public IDisposable TakeControl() => _owner.SessionSubscriptions.Add(TimbnPlayerControl.Take());

    /// <summary>
    /// Makes the player walk faster or slower until the returned handle is disposed. It multiplies the game's own
    /// speed without changing it, so the game's slowdowns, such as aiming a bow, still apply on top and cannot
    /// cancel it. Several mods can hold a speed at once and the newest one wins. When the last is released,
    /// the player walks at the game's speed again.
    /// </summary>
    /// <example>
    /// <code>
    /// Buff = new TimbnPotionBuff
    /// {
    ///     OnStart = () => _swift = Player.MultiplyWalkSpeed(1.5f),
    ///     OnEnd = () => _swift?.Dispose(),
    /// };
    /// </code>
    /// </example>
    /// <param name="multiplier">How many times the normal walking speed. 1 is normal.</param>
    /// <returns>A handle that ends the change when disposed. It is also released when the mod unloads and on the way to the main menu.</returns>
    public IDisposable MultiplyWalkSpeed(float multiplier) => _owner.SessionSubscriptions.Add(TimbnMovement.SetSpeed(multiplier));

    /// <summary>
    /// Plays one of the game's animations on the player until the returned handle is disposed, such as working with their
    /// hands while they pet a cat. Core keeps it playing, since the game's own movement sets the player idle, for example
    /// the frame after a walk ends. Take control first so the player stays put. Several mods can hold an animation at
    /// once, and the newest one plays. When the last is released, the player goes back to idle.
    /// </summary>
    /// <example>
    /// <code>
    /// _control = Player.TakeControl();
    /// _animation = Player.HoldAnimation(AnimationState.WorkHands);
    /// </code>
    /// </example>
    /// <param name="state">The animation, such as AnimationState.WorkHands.</param>
    /// <returns>A handle that ends the animation when disposed. It is also released when the mod unloads and on the way to the main menu.</returns>
    public IDisposable HoldAnimation(AnimationState state) => _owner.SessionSubscriptions.Add(TimbnPlayerAnimation.Hold(state));

    /// <summary>
    /// Walks the player to a spot along the navmesh as if they held the movement keys and calls back when they get
    /// there. They move at their own speed. They only go where they could by hand, so walls, fences and ledges stop
    /// them. If the player doesn't fit at the spot (against a wall, for example), they walk as close as they can.
    /// Without a hold, any movement input ends the walk. That suits a walk the player asked for, like clicking where
    /// to go. Take control first for a walk a scene needs. Then the player cannot stop it. A talk or
    /// anything else that takes control ends the walk either way. A newer walk from any mod takes over once its route is found. The player
    /// keeps moving meanwhile and carries on if the newer one finds no way.
    /// </summary>
    /// <example>
    /// <code>
    /// _control = Player.TakeControl();
    /// _walk = Player.WalkTo(bench, SitDown);
    /// // later, to call it off
    /// _walk?.Dispose();
    /// </code>
    /// </example>
    /// <param name="position">Where to walk to.</param>
    /// <param name="arrived">
    /// Runs once on arrival. It never runs after the handle is disposed, when the walk is ended or taken over, or when
    /// the player has no way to within half a unit of the spot or gets blocked on the way, each of which is logged.
    /// </param>
    /// <returns>A handle that stops the walk when disposed. It is also stopped when the mod unloads and on the way to the main menu.</returns>
    public IDisposable WalkTo(Vector3 position, Action? arrived = null) => OwnWalk(TimbnPlayerWalk.Start(position, arrived, _owner.PluginLogger), null);

    /// <summary>
    /// Walks the player to a spot like <see cref="WalkTo(Vector3, Action)"/>, and calls back however the walk ends, with
    /// whether they got there. Use it when the player has to be let go if they never arrive, such as a walk under a held
    /// control that the navmesh has no way for.
    /// </summary>
    /// <example>
    /// <code>
    /// _control = Player.TakeControl();
    /// _walk = Player.WalkTo(bench, arrived =>
    /// {
    ///     if (arrived)
    ///         SitDown();
    ///     else
    ///         _control?.Dispose();
    /// });
    /// </code>
    /// </example>
    /// <param name="position">Where to walk to.</param>
    /// <param name="ended">
    /// Runs once when the walk ends, with true on arrival and false when the player has no way, gets blocked, leaves the
    /// scene or a newer walk takes over. It never runs after the handle is disposed.
    /// </param>
    /// <returns>A handle that stops the walk when disposed. It is also stopped when the mod unloads and on the way to the main menu.</returns>
    public IDisposable WalkTo(Vector3 position, Action<bool> ended) => OwnWalk(TimbnPlayerWalk.Start(position, null, _owner.PluginLogger), ended);

    /// <summary>
    /// Puts an item over the player's head the way a log sits there. A small item like a mug shows its icon there. If
    /// the player has a free overhead slot, what is overhead stays and the new item goes on top. Otherwise what was on
    /// top is dropped. Check TimbnPlayer.HasOverheadItem first when the item should be the only one overhead. Pressing
    /// the interaction key with nothing in reach puts the item down the way the game drops anything overhead. The game
    /// does the same when the player starts work or a fight. The item is kept out of every save. An item that is
    /// already overhead is not added again. The handle then comes back already ended. The rule decides how it sits
    /// overhead and leaves, as described on <see cref="TimbnOverheadRule"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// _mug = Player.AddOverheadItem(new Item("my_mod_mug"), () => TimbnItems.RemoveDrops(TimbnItems.FindDrops(drop => drop.Id == "my_mod_mug")));
    /// // once it is taken off
    /// _mug.Dispose();
    /// </code>
    /// </example>
    /// <param name="item">The item to put overhead, such as one the mod added with Balance.Add.</param>
    /// <param name="putDown">
    /// Runs once the player has put it down or the game took it, a frame later, so a dropped item is already on the
    /// ground. It does not run when the handle is disposed.
    /// </param>
    /// <param name="rule">How the item sits overhead and leaves. By default as the game treats a log.</param>
    /// <returns>A handle that takes the item off when disposed, if it is still overhead. It is also taken off when the mod unloads and on the way to the main menu.</returns>
    public IDisposable AddOverheadItem(Item item, Action? putDown = null, TimbnOverheadRule rule = TimbnOverheadRule.Vanilla)
    {
        var handle = TimbnOverhead.Add(item, putDown, rule, _owner.PluginLogger);
        if (handle.IsDone)
            return handle;

        var check = _owner.Events.Update(handle.Check);
        var owned = _owner.SessionSubscriptions.Add(() =>
        {
            check.Dispose();
            handle.Dispose();
        });
        handle.Ended = owned.Dispose;
        return owned;
    }

    /// <summary>Use <see cref="AddOverheadItem"/>.</summary>
    [Obsolete("Use AddOverheadItem.")]
    public IDisposable Carry(Item item, Action? putDown = null, TimbnOverheadRule rule = TimbnOverheadRule.Vanilla) =>
        AddOverheadItem(item, putDown, rule);

    /// <summary>
    /// Moves the player to a spot along the NPC navmesh with the game's own walker, the way a story scene walks them,
    /// and calls back when they get there. Their physics is off on the way, so they never get stuck, but they pass
    /// through gaps and over ledges they could not walk by hand. Movement keys do not stop it, so dispose the handle
    /// to call it off. A newer slide from any mod replaces this one. Then this one never calls back.
    /// </summary>
    /// <example>
    /// <code>
    /// _slide?.Dispose();
    /// _slide = Player.SlideTo(hit.point);
    /// </code>
    /// </example>
    /// <param name="position">Where to go.</param>
    /// <param name="arrived">Runs once on arrival. It never runs after the handle is disposed or when the navmesh has no way there, which is logged.</param>
    /// <returns>A handle that stops the slide when disposed. It is also stopped when the mod unloads and on the way to the main menu.</returns>
    public IDisposable SlideTo(Vector3 position, Action? arrived = null) => OwnSlide(StartSlide(position, arrived), null);

    /// <summary>
    /// Slides the player to a spot like <see cref="SlideTo(Vector3, Action)"/>, and calls back however the slide ends, with
    /// whether they got there.
    /// </summary>
    /// <param name="position">Where to go.</param>
    /// <param name="ended">
    /// Runs once when the slide ends, with true on arrival and false when the navmesh has no way there, the slide fails or
    /// a newer slide replaces it. It never runs after the handle is disposed.
    /// </param>
    /// <returns>A handle that stops the slide when disposed. It is also stopped when the mod unloads and on the way to the main menu.</returns>
    public IDisposable SlideTo(Vector3 position, Action<bool> ended) => OwnSlide(StartSlide(position, null), ended);

    private TimbnPlayerSlide StartSlide(Vector3 position, Action? arrived) =>
        TimbnPlayerSlide.Start(position, arrived, _owner.PluginLogger, overNavmesh: true, pace: TimbnPlayerSlide.WalkPace);

    private IDisposable OwnWalk(TimbnPlayerWalk walk, Action<bool>? ended)
    {
        if (walk.IsDone)
        {
            Report(ended, false);
            return walk;
        }

        var owned = _owner.SessionSubscriptions.Add(walk);
        walk.Ended = () =>
        {
            owned.Dispose();
            Report(ended, walk.Arrived);
        };
        return owned;
    }

    private IDisposable OwnSlide(TimbnPlayerSlide slide, Action<bool>? ended)
    {
        if (slide.IsDone)
        {
            Report(ended, slide.Arrived);
            return slide;
        }

        var owned = _owner.SessionSubscriptions.Add(slide);
        slide.Ended = () =>
        {
            owned.Dispose();
            Report(ended, slide.Arrived);
        };
        return owned;
    }

    private void Report(Action<bool>? ended, bool arrived)
    {
        if (ended != null)
            TimbnSafe.Run(() => ended(arrived), _owner.PluginLogger, $"{nameof(TimbnPluginPlayer)}|{TimbnSafe.Describe(ended)}");
    }
}
