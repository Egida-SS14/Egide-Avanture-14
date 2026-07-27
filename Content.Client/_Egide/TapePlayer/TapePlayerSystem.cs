using Content.Client._Egide.TapePlayer.UI;
using Content.Shared._Egide.TapePlayer;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;

namespace Content.Client._Egide.TapePlayer;

public sealed class TapePlayerSystem : SharedTapePlayerSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly TapeCategorySystem _categorySystem = default!;

    private readonly Dictionary<EntityUid, EntityUid?> _audioStreams = new();
    private readonly Dictionary<EntityUid, string> _lastSongId = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapePlayerComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<TapePlayerComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<TapePlayerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;

        // Sync audio playback
        if (comp.PlayMode == TapePlayerPlayMode.Playing)
        {
            if (!_audioStreams.ContainsKey(uid))
                PlayAudio(uid, comp);
        }
        else
        {
            StopAudio(uid);
        }

        // Only do a full UI rebuild when the song selection actually changes
        if (!_lastSongId.TryGetValue(uid, out var lastId) || lastId != comp.CurrentSongId)
        {
            _lastSongId[uid] = comp.CurrentSongId;

            if (_ui.TryGetOpenUi<TapePlayerBoundUserInterface>(uid, TapePlayerUiKey.Key, out var bui))
                bui.Reload();
        }
    }

    private void OnAppearanceChange(EntityUid uid, TapePlayerComponent comp, ref AppearanceChangeEvent args)
    {
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Update position slider from local audio, no full rebuild
        var query = EntityQueryEnumerator<TapePlayerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PlayMode != TapePlayerPlayMode.Playing)
                continue;

            if (!_ui.TryGetOpenUi<TapePlayerBoundUserInterface>(uid, TapePlayerUiKey.Key, out var bui))
                continue;

            var pos = GetAudioPosition(uid);
            bui.Window?.UpdatePlaybackPosition(pos);
        }
    }

    public void PlayAudio(EntityUid uid, TapePlayerComponent comp)
    {
        if (comp.CurrentSongSound == null)
            return;

        StopAudio(uid);

        var volMult = comp.Volume <= 0.5f
            ? comp.Volume * 2f
            : 1f + (comp.Volume - 0.5f);
        var volDb = volMult > 0.001f ? 20f * MathF.Log10(volMult) : -100f;

        var audioParams = AudioParams.Default
            .WithVolume(volDb)
            .WithLoop(false);

        var audioUid = Audio.PlayPvs(comp.CurrentSongSound, uid, audioParams: audioParams)?.Entity;
        _audioStreams[uid] = audioUid;
    }

    public void StopAudio(EntityUid uid)
    {
        if (_audioStreams.TryGetValue(uid, out var audioUid) && audioUid.HasValue)
        {
            QueueDel(audioUid.Value);
            _audioStreams.Remove(uid);
        }
    }

    public void UpdateAudioVolume(EntityUid uid, TapePlayerComponent comp)
    {
        if (!_audioStreams.TryGetValue(uid, out var audioUid) || !audioUid.HasValue)
            return;

        if (!TryComp<AudioComponent>(audioUid.Value, out var audio))
            return;

        var volMult = comp.Volume <= 0.5f
            ? comp.Volume * 2f
            : 1f + (comp.Volume - 0.5f);
        var volDb = volMult > 0.001f ? 20f * MathF.Log10(volMult) : -100f;

        Audio.SetVolume(audioUid.Value, volDb, audio);
    }

    public void SeekAudio(EntityUid uid, float position)
    {
        if (!_audioStreams.TryGetValue(uid, out var audioUid) || !audioUid.HasValue)
            return;

        Audio.SetPlaybackPosition(audioUid.Value, position);
    }

    public float GetAudioPosition(EntityUid uid)
    {
        if (!_audioStreams.TryGetValue(uid, out var audioUid) || !audioUid.HasValue)
            return 0f;

        if (TryComp<AudioComponent>(audioUid.Value, out var audio))
        {
            return audio.PlaybackPosition;
        }

        return 0f;
    }

    public List<TapeCategoryTreeEntry> BuildCategoryTree(List<string> rootIds)
    {
        var nodes = _categorySystem.BuildTree(rootIds);
        var entries = new List<TapeCategoryTreeEntry>();

        foreach (var node in nodes)
        {
            entries.Add(ConvertNode(node));
        }

        return entries;
    }

    private TapeCategoryTreeEntry ConvertNode(TapeCategoryNode node)
    {
        var children = new List<TapeCategoryTreeEntry>();
        foreach (var child in node.Children)
        {
            children.Add(ConvertNode(child));
        }

        return new TapeCategoryTreeEntry(node.Id, node.DisplayName, children);
    }

    public List<TapeSongTreeEntry> BuildSongTree(string? categoryId)
    {
        var songNodes = _categorySystem.BuildAllSongTrees(categoryId);
        var entries = new List<TapeSongTreeEntry>();

        foreach (var node in songNodes)
        {
            entries.Add(new TapeSongTreeEntry(node.Id, node.DisplayName, new()));
        }

        return entries;
    }
}
