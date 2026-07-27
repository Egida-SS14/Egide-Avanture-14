using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._Egide.TapePlayer;

public sealed class TapeCategorySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public List<TapeCategoryNode> BuildTree(List<string> rootIds)
    {
        var allCategories = new Dictionary<string, TapeCategoryNode>();

        foreach (var proto in _proto.EnumeratePrototypes<TapeCategoryPrototype>())
        {
            allCategories[proto.ID] = new TapeCategoryNode
            {
                Id = proto.ID,
                DisplayName = proto.Name,
                SortOrder = proto.SortOrder,
            };
        }

        foreach (var proto in _proto.EnumeratePrototypes<TapeCategoryPrototype>())
        {
            if (proto.ParentId != null && allCategories.TryGetValue(proto.ParentId, out var parent))
            {
                if (allCategories.TryGetValue(proto.ID, out var child))
                    parent.Children.Add(child);
            }
        }

        var roots = new List<TapeCategoryNode>();
        foreach (var rootId in rootIds)
        {
            if (allCategories.TryGetValue(rootId, out var root))
                roots.Add(root);
        }

        roots.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        return roots;
    }

    public List<TapeSongNode> BuildAllSongTrees(string? categoryId)
    {
        var allSongs = new List<TapeSongNode>();

        foreach (var proto in _proto.EnumeratePrototypes<MusicTapePrototype>())
        {
            if (!string.IsNullOrEmpty(categoryId) && proto.CategoryId != categoryId)
                continue;

            allSongs.Add(new TapeSongNode
            {
                Id = proto.ID,
                DisplayName = proto.SongName,
                SortOrder = 0,
            });
        }

        return allSongs;
    }

    public bool IsLeaf(string categoryId)
    {
        if (!_proto.TryIndex<TapeCategoryPrototype>(categoryId, out var proto))
            return true;

        return proto.ParentId != null && !_proto.EnumeratePrototypes<TapeCategoryPrototype>()
            .Any(p => p.ParentId == categoryId);
    }
}

public sealed class TapeCategoryNode
{
    public string Id = string.Empty;
    public string DisplayName = string.Empty;
    public int SortOrder;
    public List<TapeCategoryNode> Children = new();
}

public sealed class TapeSongNode
{
    public string Id = string.Empty;
    public string DisplayName = string.Empty;
    public int SortOrder;
}
