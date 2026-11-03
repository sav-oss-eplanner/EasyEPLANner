using System.Collections.Generic;
using System.Linq;

namespace IO.ViewModel
{
    public class Root : IRoot, IExpandable, IHasDescriptionIcon
    {
        private readonly List<IViewItem> items = [];

        public Root(IIOViewModel context)
        {
            Context = context;

            var stubId = 0;
            var deletedModules = context.IOManager?.DeletedModules ?? [];
            var nodes = (context.IOManager?.IONodes ?? [])
                .Where(n => n != null)
                // Заполнители дыр из AddNode (пустое имя) не показываем.
                .Where(n => !(n.Type is IONode.TYPES.T_EMPTY &&
                    string.IsNullOrEmpty(n.Name)))
                .ToList();

            foreach (var group in nodes.GroupBy(
                n => GetLocationGroupKey(n, ref stubId)))
            {
                if (IsFlatNodeGroup(group.Key))
                {
                    // Без шкафа (или заглушка) — каждый узел отдельно.
                    // Иначе при пустом Location все узлы схлопывались в First().
                    items.AddRange(group.Select(n => new Node(n, null)));
                    continue;
                }

                items.Add(new Location(
                    group.Key.Location,
                    group.Key.Description,
                    [.. group],
                    GetDeletedModulesByLocation(deletedModules,
                        group.Key.Location)));
            }

            var deletedModulesWithoutLocation = GetDeletedModulesByLocation(
                deletedModules, string.Empty);
            if (deletedModulesWithoutLocation.Any())
            {
                items.Add(new DeletedModulesGroup(
                    deletedModulesWithoutLocation));
            }
        }

        private static LocationGroupKey GetLocationGroupKey(IIONode node,
            ref int stubId)
        {
            if (node.Type is IONode.TYPES.T_EMPTY)
                return LocationGroupKey.FlatStub(stubId++);

            if (string.IsNullOrEmpty(node.Location))
                return LocationGroupKey.FlatNode(node.N, node.Name);

            return LocationGroupKey.Cabinet(node.Location,
                node.LocationDescription);
        }

        private static bool IsFlatNodeGroup(LocationGroupKey key) =>
            key.Kind is not LocationGroupKind.Cabinet;

        private static IEnumerable<IIOModule> GetDeletedModulesByLocation(
            IEnumerable<IIOModule> deletedModules, string location)
        {
            return deletedModules.Where(module => module.Location == location);
        }

        public IIOViewModel Context { get; private set; }

        public string Name => "ПЛК";

        public string Description => string.Empty;

        public IEnumerable<IViewItem> Items => items;

        public bool Expanded { get; set; } = true;

        public bool HasBindingError =>
            items.OfType<IHasBindingError>().Any(item => item.HasBindingError);

        Icon IHasDescriptionIcon.Icon =>
            HasBindingError ? Icon.Error : Icon.None;

        private enum LocationGroupKind
        {
            Cabinet,
            FlatNode,
            FlatStub,
        }

        private readonly struct LocationGroupKey
        {
            public LocationGroupKind Kind { get; }
            public string Location { get; }
            public string Description { get; }
            private readonly string unique;

            private LocationGroupKey(LocationGroupKind kind, string location,
                string description, string unique)
            {
                Kind = kind;
                Location = location;
                Description = description;
                this.unique = unique;
            }

            public static LocationGroupKey Cabinet(string location,
                string description) =>
                new(LocationGroupKind.Cabinet, location,
                    description ?? string.Empty, location);

            public static LocationGroupKey FlatNode(int n, string name) =>
                new(LocationGroupKind.FlatNode, string.Empty, string.Empty,
                    $"node:{n}:{name}");

            public static LocationGroupKey FlatStub(int stubId) =>
                new(LocationGroupKind.FlatStub, string.Empty, string.Empty,
                    $"stub:{stubId}");

            public override bool Equals(object obj) =>
                obj is LocationGroupKey other &&
                Kind == other.Kind &&
                unique == other.unique;

            public override int GetHashCode() =>
                (Kind, unique).GetHashCode();
        }
    }
}
