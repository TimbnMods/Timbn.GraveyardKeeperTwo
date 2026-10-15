namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Works out where mod quests sit in the quest tree, from each quest's original place and nothing a previous layout
/// left behind, with no game statics so it can be tested. Started mod quest families are packed into a band at the
/// top and the story moves down below it. A family under a game quest goes in the free spot nearest below that quest,
/// within two rows, and only when there is none do rows open up in the story under it. Unstarted mod quests sit off the
/// tree. A chain of started quests made with ContinuesFrom shares one cell and shows its newest step, the way the game's
/// brother quests do. A quest that only names a parent gets a cell of its own below it.
/// </summary>
internal static class TimbnQuestLayout
{
    internal static Plan Arrange(IReadOnlyList<Quest> ours, IReadOnlyList<Quest> game)
    {
        var layout = new Layout(ours, game);
        layout.FindChains();
        layout.FindFamilies();
        layout.PlaceFamiliesInBand();
        layout.MoveStoryBelowBand();
        layout.PlaceFamiliesUnderGameQuests();
        layout.StackChains();
        layout.LinkBranchesToChains();
        return layout.Plan;
    }

    internal sealed class Quest
    {
        internal Quest(string id, IReadOnlyList<string> parents, Cell cell, bool hasCell = true, bool started = false, bool continues = false)
        {
            Id = id;
            Parents = parents;
            Cell = cell;
            HasCell = hasCell;
            Started = started;
            Continues = continues;
        }

        internal string Id { get; }

        internal IReadOnlyList<string> Parents { get; }

        internal Cell Cell { get; }

        internal bool HasCell { get; }

        internal bool Started { get; }

        internal bool Continues { get; }
    }

    internal sealed class Plan
    {
        internal Dictionary<string, Cell> Cells { get; } = [];

        internal Dictionary<string, List<string>> ExtraParents { get; } = [];

        internal Dictionary<string, bool> Hidden { get; } = [];
    }

    internal readonly struct Cell : IEquatable<Cell>
    {
        internal Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        internal int X { get; }

        internal int Y { get; }

        public static Cell operator +(Cell a, Cell b) => new(a.X + b.X, a.Y + b.Y);

        public static bool operator ==(Cell a, Cell b) => a.Equals(b);

        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);

        public bool Equals(Cell other) => X == other.X && Y == other.Y;

        public override bool Equals(object? obj) => obj is Cell other && Equals(other);

        public override int GetHashCode() => (X * 397) ^ Y;

        public override string ToString() => $"({X}, {Y})";
    }

    private sealed class Layout
    {
        private readonly IReadOnlyList<Quest> _ours;
        private readonly IReadOnlyList<Quest> _game;
        private readonly Dictionary<string, Quest> _oursById = [];
        private readonly Dictionary<string, Quest> _gameById = [];
        private readonly Dictionary<Quest, Cell> _cells = [];
        private readonly Dictionary<Quest, (Quest Head, int Depth)> _chainOf = [];
        private readonly Dictionary<Quest, List<Quest>> _chains = [];
        private readonly Dictionary<Quest, List<Quest>> _kids = [];
        private readonly List<(Quest Head, Quest Branch)> _branches = [];
        private readonly List<Quest> _roots = [];
        private readonly HashSet<Cell> _band = [];
        private readonly List<Quest> _placed = [];
        private readonly int _width;

        internal Layout(IReadOnlyList<Quest> ours, IReadOnlyList<Quest> game)
        {
            _ours = ours;
            _game = game;
            foreach (var quest in ours)
                _oursById[quest.Id] = quest;

            foreach (var quest in game)
            {
                _gameById[quest.Id] = quest;
                _cells[quest] = quest.Cell;
            }

            _width = game.Where(q => q.HasCell).Select(q => q.Cell.X).DefaultIfEmpty(0).Max() + 1;
        }

        internal Plan Plan { get; } = new();

        internal void FindChains()
        {
            var nextStep = new Dictionary<Quest, Quest>();
            foreach (var quest in _ours.Where(q => q.Continues))
            {
                if (ModParent(quest) is { } parent && !nextStep.ContainsKey(parent))
                    nextStep[parent] = quest;
            }

            foreach (var quest in _ours.Where(q => q.Started))
            {
                var head = quest;
                var depth = 0;
                var seen = new HashSet<Quest>();
                while (seen.Add(head) && StartedParent(head) is { } parent && nextStep.TryGetValue(parent, out var next) && next == head)
                {
                    head = parent;
                    depth++;
                }

                _chainOf[quest] = (head, depth);
                if (!_chains.TryGetValue(head, out var steps))
                    _chains[head] = steps = [];

                steps.Add(quest);
            }
        }

        internal void FindFamilies()
        {
            var offTree = 0;
            foreach (var quest in _ours)
            {
                if (!quest.Started)
                {
                    _cells[quest] = new Cell(0, -1 - offTree++);
                    continue;
                }

                if (_chainOf[quest].Head != quest)
                    continue;

                if (StartedParent(quest) is not { } parent || HangsUnderItself(quest))
                {
                    _roots.Add(quest);
                    continue;
                }

                var head = _chainOf[parent].Head;
                if (!_kids.TryGetValue(head, out var list))
                    _kids[head] = list = [];

                list.Add(quest);
                _branches.Add((head, quest));
            }

            foreach (var quest in _ours)
            {
                if (GameParent(quest) is not { } anchor)
                    continue;

                var mates = _game.Where(q => q.HasCell && q != anchor && q.Cell == anchor.Cell && !quest.Parents.Contains(q.Id)).Select(q => q.Id);
                AddParents(quest, mates);
            }
        }

        internal void PlaceFamiliesInBand()
        {
            foreach (var root in _roots.Where(r => GameParent(r) is null))
            {
                var shape = Shape(root, out var shapeWidth);
                Place(shape, FindSpot(shape, shapeWidth, _band, 0, 0), _band);
            }
        }

        internal void MoveStoryBelowBand()
        {
            var shift = _band.Count == 0 ? 0 : _band.Max(c => c.Y) + 2;
            foreach (var quest in _game)
                _cells[quest] = quest.Cell + new Cell(0, shift);
        }

        internal void PlaceFamiliesUnderGameQuests()
        {
            var anchored = _roots.Select(root => (Root: root, Anchor: GameParent(root))).Where(a => a.Anchor is not null).ToList();
            foreach (var (root, anchor) in anchored.OrderBy(a => _cells[a.Anchor!].Y))
            {
                var parent = _cells[anchor!];
                var shape = Shape(root, out var shapeWidth);
                var height = shape.Values.Max(c => c.Y) + 1;
                if (!TryNextTo(root, shape, shapeWidth, parent, 2, out var origin))
                {
                    foreach (var quest in _game.Concat(_placed))
                    {
                        if (_cells[quest].Y > parent.Y)
                            _cells[quest] += new Cell(0, height);
                    }

                    if (!TryNextTo(root, shape, shapeWidth, parent, 1, out origin))
                    {
                        var blocked = BlockedCells();
                        blocked.UnionWith(_band);
                        blocked.UnionWith(_placed.Select(q => _cells[q]));
                        origin = FindSpot(shape, shapeWidth, blocked, parent.Y + 1, parent.X);
                    }
                }

                Place(shape, origin, []);
                _placed.AddRange(shape.Keys);
            }
        }

        internal void StackChains()
        {
            foreach (var quest in _ours.Where(q => q.Started))
            {
                var (head, _) = _chainOf[quest];
                _cells[quest] = _cells[head];
                var steps = _chains[head];
                Plan.Hidden[quest.Id] = steps.Count > 1 && quest != steps.OrderByDescending(s => _chainOf[s].Depth).First();
            }

            foreach (var pair in _cells)
                Plan.Cells[pair.Key.Id] = pair.Value;
        }

        internal void LinkBranchesToChains()
        {
            foreach (var (head, branch) in _branches)
                AddParents(branch, _chains[head].Where(step => !branch.Parents.Contains(step.Id)).Select(step => step.Id));
        }

        private Quest? ModParent(Quest quest) =>
            quest.Parents.Count > 0 && _oursById.TryGetValue(quest.Parents[0], out var parent) && parent != quest ? parent : null;

        private Quest? StartedParent(Quest quest) => ModParent(quest) is { Started: true } parent ? parent : null;

        private Quest? GameParent(Quest quest) =>
            quest.Parents.Count > 0 && _gameById.TryGetValue(quest.Parents[0], out var parent) ? parent : null;

        private bool HangsUnderItself(Quest head)
        {
            var seen = new HashSet<Quest>();
            var current = head;
            while (seen.Add(current) && StartedParent(current) is { } parent)
            {
                current = _chainOf[parent].Head;
                if (current == head)
                    return true;
            }

            return false;
        }

        private void AddParents(Quest quest, IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                if (!Plan.ExtraParents.TryGetValue(quest.Id, out var extra))
                    Plan.ExtraParents[quest.Id] = extra = [];

                if (!extra.Contains(id))
                    extra.Add(id);
            }
        }

        private Dictionary<Quest, Cell> Shape(Quest root, out int width)
        {
            var shape = new Dictionary<Quest, Cell>();
            var next = 0;
            void layout(Quest node, int depth)
            {
                shape[node] = default;
                var children = _kids.TryGetValue(node, out var list) ? list.Where(k => !shape.ContainsKey(k)).ToList() : [];
                if (children.Count == 0)
                {
                    shape[node] = new Cell(next++, depth);
                    return;
                }

                foreach (var child in children)
                    layout(child, depth + 1);

                shape[node] = new Cell(shape[children[0]].X, depth);
            }

            layout(root, 0);
            width = next;
            return shape;
        }

        private bool TryNextTo(Quest root, Dictionary<Quest, Cell> shape, int shapeWidth, Cell parent, int maxRows, out Cell origin)
        {
            var blocked = BlockedCells();
            blocked.UnionWith(_band);
            blocked.UnionWith(_placed.Select(q => _cells[q]));
            var columns = Enumerable.Range(0, _width).OrderBy(x => Math.Abs(x - parent.X)).ThenByDescending(x => x).ToList();
            for (var dy = 1; dy <= maxRows; dy++)
            {
                foreach (var column in columns)
                {
                    var start = new Cell(column - shape[root].X, parent.Y + dy);
                    if (start.X < 0 || start.X + shapeWidth > _width)
                        continue;

                    if (shape.Values.Any(cell => blocked.Contains(start + cell)))
                        continue;

                    if (Enumerable.Range(parent.Y + 1, dy - 1).Any(y => blocked.Contains(new Cell(column, y))))
                        continue;

                    origin = start;
                    return true;
                }
            }

            origin = default;
            return false;
        }

        private Cell FindSpot(Dictionary<Quest, Cell> shape, int shapeWidth, HashSet<Cell> blocked, int startY, int preferX)
        {
            var starts = Enumerable.Range(0, Math.Max(1, _width - shapeWidth + 1))
                .OrderBy(x => Math.Abs(x - preferX))
                .ThenBy(x => x)
                .ToList();
            for (var y = startY; ; y++)
            {
                foreach (var x in starts)
                {
                    var origin = new Cell(x, y);
                    if (!shape.Values.Any(cell => blocked.Contains(origin + cell)))
                        return origin;
                }
            }
        }

        private void Place(Dictionary<Quest, Cell> shape, Cell origin, HashSet<Cell> taken)
        {
            foreach (var pair in shape)
            {
                _cells[pair.Key] = origin + pair.Value;
                taken.Add(origin + pair.Value);
            }
        }

        private HashSet<Cell> BlockedCells()
        {
            var blocked = new HashSet<Cell>(_game.Select(q => _cells[q]));
            foreach (var quest in _game.Where(q => q.HasCell))
            {
                foreach (var parentId in quest.Parents)
                {
                    if (!_gameById.TryGetValue(parentId, out var parent) || !parent.HasCell)
                        continue;

                    var child = _cells[quest];
                    for (var y = _cells[parent].Y + 1; y < child.Y; y++)
                        blocked.Add(new Cell(child.X, y));
                }
            }

            return blocked;
        }
    }
}
