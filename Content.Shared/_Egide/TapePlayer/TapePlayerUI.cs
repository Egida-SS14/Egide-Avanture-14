using Robust.Shared.Serialization;

namespace Content.Shared._Egide.TapePlayer;

[Serializable, NetSerializable]
public enum TapePlayerPlayMode : byte
{
    Stopped,
    Playing,
    Rewinding
}

[Serializable, NetSerializable]
public enum TapePlayerVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum TapePlayerVisualState : byte
{
    Off,
    On,
    Rewinding
}

[Serializable, NetSerializable]
public enum TapePlayerVisualLayers : byte
{
    Base
}

[Serializable, NetSerializable]
public enum TapePlayerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class TapePlayerToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TapePlayerStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TapePlayerSetVolumeMessage(float volume) : BoundUserInterfaceMessage
{
    public float Volume = volume;
}

[Serializable, NetSerializable]
public sealed class TapePlayerSetPositionMessage(float position) : BoundUserInterfaceMessage
{
    public float Position = position;
}

[Serializable, NetSerializable]
public sealed class TapePlayerSetCategoryMessage(string category) : BoundUserInterfaceMessage
{
    public string Category = category;
}

[Serializable, NetSerializable]
public sealed class TapePlayerSelectSongMessage(string songId) : BoundUserInterfaceMessage
{
    public string SongId = songId;
}

[Serializable, NetSerializable]
public sealed class TapePlayerState : BoundUserInterfaceState
{
    public string CurrentSongId;
    public string CurrentSongName;
    public float SongLength;
    public TapePlayerPlayMode Mode;
    public float CurrentPosition;
    public float Volume;
    public string CurrentCategory;
    public List<TapeCategoryTreeEntry> CategoryTree;
    public List<TapeSongTreeEntry> SongTree;

    public TapePlayerState(
        string currentSongId,
        string currentSongName,
        float songLength,
        TapePlayerPlayMode mode,
        float currentPosition,
        float volume,
        string currentCategory,
        List<TapeCategoryTreeEntry> categoryTree,
        List<TapeSongTreeEntry> songTree)
    {
        CurrentSongId = currentSongId;
        CurrentSongName = currentSongName;
        SongLength = songLength;
        Mode = mode;
        CurrentPosition = currentPosition;
        Volume = volume;
        CurrentCategory = currentCategory;
        CategoryTree = categoryTree;
        SongTree = songTree;
    }
}

[Serializable, NetSerializable]
public sealed class TapeCategoryTreeEntry
{
    public string Id;
    public string DisplayName;
    public List<TapeCategoryTreeEntry> Children;

    public TapeCategoryTreeEntry(string id, string displayName, List<TapeCategoryTreeEntry> children)
    {
        Id = id;
        DisplayName = displayName;
        Children = children;
    }
}

[Serializable, NetSerializable]
public sealed class TapeSongTreeEntry
{
    public string Id;
    public string DisplayName;
    public List<TapeSongTreeEntry> Children;

    public TapeSongTreeEntry(string id, string displayName, List<TapeSongTreeEntry> children)
    {
        Id = id;
        DisplayName = displayName;
        Children = children;
    }
}
