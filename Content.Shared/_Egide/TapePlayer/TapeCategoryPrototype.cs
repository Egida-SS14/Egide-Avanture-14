using Robust.Shared.Prototypes;

namespace Content.Shared._Egide.TapePlayer;

[Prototype("tapeCategory")]
public sealed partial class TapeCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string? ParentId;

    [DataField]
    public int SortOrder;
}
