using Content.Shared._Egide.TapePlayer;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Egide.TapePlayer;

public sealed class TapePlayerSystem : SharedTapePlayerSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TapeCategorySystem _categorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapePlayerComponent, TapePlayerToggleMessage>(OnToggleMessage);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerStopMessage>(OnStopMessage);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerSetVolumeMessage>(OnSetVolumeMessage);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerSetPositionMessage>(OnSetPositionMessage);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerSetCategoryMessage>(OnSetCategoryMessage);
        SubscribeLocalEvent<TapePlayerComponent, TapePlayerSelectSongMessage>(OnSelectSongMessage);
        SubscribeLocalEvent<TapePlayerComponent, AfterActivatableUIOpenEvent>(OnUiOpened);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TapePlayerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PlayMode == TapePlayerPlayMode.Playing)
            {
                comp.PlaybackPosition += frameTime;

                if (comp.CurrentSongLength > 0 && comp.PlaybackPosition >= comp.CurrentSongLength)
                {
                    comp.PlaybackPosition = comp.CurrentSongLength;
                    SetMode(uid, comp, TapePlayerPlayMode.Stopped);
                }
            }
            else if (comp.PlayMode == TapePlayerPlayMode.Rewinding)
            {
                comp.PlaybackPosition -= frameTime * comp.RewindSpeed;

                if (comp.PlaybackPosition <= 0f)
                {
                    comp.PlaybackPosition = 0f;
                    SetMode(uid, comp, TapePlayerPlayMode.Stopped);
                }
            }
        }
    }

    private void OnToggleMessage(EntityUid uid, TapePlayerComponent comp, TapePlayerToggleMessage args)
    {
        var newMode = comp.PlayMode == TapePlayerPlayMode.Playing
            ? TapePlayerPlayMode.Stopped
            : TapePlayerPlayMode.Playing;

        SetMode(uid, comp, newMode);
    }

    private void OnStopMessage(EntityUid uid, TapePlayerComponent comp, TapePlayerStopMessage args)
    {
        SetMode(uid, comp, TapePlayerPlayMode.Stopped);
        comp.PlaybackPosition = 0f;
        UpdateUi(uid, comp);
    }

    private void OnSetVolumeMessage(EntityUid uid, TapePlayerComponent comp, TapePlayerSetVolumeMessage args)
    {
        comp.Volume = Math.Clamp(args.Volume, 0f, comp.MaxVolume);
        Dirty(uid, comp);
        UpdateUi(uid, comp);
    }

    private void OnSetPositionMessage(EntityUid uid, TapePlayerComponent comp, TapePlayerSetPositionMessage args)
    {
        if (comp.PlayMode != TapePlayerPlayMode.Stopped)
            return;

        comp.PlaybackPosition = Math.Max(0f, args.Position);
        UpdateUi(uid, comp);
    }

    private void OnSetCategoryMessage(EntityUid uid, TapePlayerComponent comp, TapePlayerSetCategoryMessage args)
    {
        comp.CurrentCategoryFilter = args.Category;
        UpdateUi(uid, comp);
    }

    private void OnSelectSongMessage(EntityUid uid, TapePlayerComponent comp, TapePlayerSelectSongMessage args)
    {
        if (!_proto.TryIndex<MusicTapePrototype>(args.SongId, out var song))
            return;

        SetMode(uid, comp, TapePlayerPlayMode.Stopped);
        comp.CurrentSongId = song.ID;
        comp.CurrentSongName = song.SongName;
        comp.CurrentSongSound = song.Sound;
        comp.CurrentSongLength = ResolveSongLength(song);
        comp.PlaybackPosition = 0f;

        Dirty(uid, comp);
        UpdateAppearance(uid, comp);
        UpdateUi(uid, comp);
    }

    private void OnUiOpened(EntityUid uid, TapePlayerComponent comp, AfterActivatableUIOpenEvent args)
    {
        UpdateUi(uid, comp);
    }

    private void SetMode(EntityUid uid, TapePlayerComponent comp, TapePlayerPlayMode mode)
    {
        if (mode == comp.PlayMode)
            return;

        if (mode != TapePlayerPlayMode.Stopped && string.IsNullOrEmpty(comp.CurrentSongId))
            return;

        comp.PlayMode = mode;
        Dirty(uid, comp);
        UpdateAppearance(uid, comp);
        UpdateUi(uid, comp);
    }

    private float ResolveSongLength(MusicTapePrototype song)
    {
        try
        {
            var resolved = Audio.ResolveSound(song.Sound);
            return (float)Audio.GetAudioLength(resolved).TotalSeconds;
        }
        catch
        {
            return 0f;
        }
    }

    private void UpdateUi(EntityUid uid, TapePlayerComponent comp)
    {
        if (!TryComp<UserInterfaceComponent>(uid, out _))
            return;

        if (!_ui.IsUiOpen(uid, TapePlayerUiKey.Key))
            return;

        var categoryTree = BuildCategoryTree(comp.CategoryRoots);
        var songTree = BuildSongTree(comp.CurrentCategoryFilter);

        var state = new TapePlayerState(
            comp.CurrentSongId,
            comp.CurrentSongName,
            comp.CurrentSongLength,
            comp.PlayMode,
            comp.PlaybackPosition,
            comp.Volume,
            comp.CurrentCategoryFilter,
            categoryTree,
            songTree);

        _ui.SetUiState(uid, TapePlayerUiKey.Key, state);
    }

    private List<TapeCategoryTreeEntry> BuildCategoryTree(List<string> rootIds)
    {
        var nodes = _categorySystem.BuildTree(rootIds);
        var entries = new List<TapeCategoryTreeEntry>();

        foreach (var node in nodes)
        {
            entries.Add(ConvertCategoryNode(node));
        }

        return entries;
    }

    private List<TapeSongTreeEntry> BuildSongTree(string? currentCategoryFilter)
    {
        var songNodes = _categorySystem.BuildAllSongTrees(currentCategoryFilter);
        var entries = new List<TapeSongTreeEntry>();

        foreach (var node in songNodes)
        {
            entries.Add(new TapeSongTreeEntry(node.Id, node.DisplayName, new()));
        }

        return entries;
    }

    private TapeCategoryTreeEntry ConvertCategoryNode(TapeCategoryNode node)
    {
        var children = new List<TapeCategoryTreeEntry>();
        foreach (var child in node.Children)
        {
            children.Add(ConvertCategoryNode(child));
        }

        return new TapeCategoryTreeEntry(node.Id, node.DisplayName, children);
    }
}
