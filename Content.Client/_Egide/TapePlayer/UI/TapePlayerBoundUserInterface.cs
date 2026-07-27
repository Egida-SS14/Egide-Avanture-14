using Content.Shared._Egide.TapePlayer;
using Robust.Client.UserInterface;

namespace Content.Client._Egide.TapePlayer.UI;

public sealed class TapePlayerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private TapePlayerWindow? _window;

    public TapePlayerWindow? Window => _window;

    private TapePlayerSystem _tapePlayerSystem = default!;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TapePlayerWindow>();
        _window.Owner = Owner;

        _tapePlayerSystem = EntMan.System<TapePlayerSystem>();

        _window.OnPlayPressed += OnPlay;
        _window.OnStopPressed += OnStop;
        _window.OnVolumeChanged += OnVolumeChanged;
        _window.OnPositionChanged += OnPositionChanged;
        _window.OnCategorySelected += OnCategorySelected;
        _window.OnSongSelected += OnSongSelected;
    }

    private void OnPlay()
    {
        SendMessage(new TapePlayerToggleMessage());

        if (EntMan.TryGetComponent<TapePlayerComponent>(Owner, out var comp))
        {
            if (comp.PlayMode == TapePlayerPlayMode.Playing)
            {
                _tapePlayerSystem.PlayAudio(Owner, comp);
            }
            else
            {
                _tapePlayerSystem.StopAudio(Owner);
            }
        }
    }

    private void OnStop()
    {
        SendMessage(new TapePlayerStopMessage());
        _tapePlayerSystem.StopAudio(Owner);
    }

    private void OnVolumeChanged(float volume)
    {
        SendMessage(new TapePlayerSetVolumeMessage(volume));

        if (EntMan.TryGetComponent<TapePlayerComponent>(Owner, out var comp))
        {
            _tapePlayerSystem.UpdateAudioVolume(Owner, comp);
        }
    }

    private void OnPositionChanged(float position)
    {
        SendMessage(new TapePlayerSetPositionMessage(position));
        _tapePlayerSystem.SeekAudio(Owner, position);
    }

    private void OnCategorySelected(string categoryId)
    {
        SendMessage(new TapePlayerSetCategoryMessage(categoryId));
    }

    private void OnSongSelected(string songId)
    {
        SendMessage(new TapePlayerSelectSongMessage(songId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not TapePlayerState cast)
            return;

        _window?.UpdateState(cast);
    }

    public void Reload()
    {
        if (EntMan.TryGetComponent<TapePlayerComponent>(Owner, out var comp))
        {
            var categoryTree = _tapePlayerSystem.BuildCategoryTree(comp.CategoryRoots);
            var songTree = _tapePlayerSystem.BuildSongTree(comp.CurrentCategoryFilter);

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

            _window?.UpdateState(state);
        }
    }
}
