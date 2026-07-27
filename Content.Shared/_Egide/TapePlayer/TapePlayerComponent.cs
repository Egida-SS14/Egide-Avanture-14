using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Egide.TapePlayer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TapePlayerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public string CurrentSongId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public string CurrentSongName = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public SoundSpecifier? CurrentSongSound;

    [ViewVariables, AutoNetworkedField]
    public float CurrentSongLength;

    [ViewVariables, AutoNetworkedField]
    public TapePlayerPlayMode PlayMode = TapePlayerPlayMode.Stopped;

    [ViewVariables, AutoNetworkedField]
    public float PlaybackPosition;

    [ViewVariables, AutoNetworkedField]
    public float Volume = 0.8f;

    [ViewVariables, AutoNetworkedField]
    public string CurrentCategoryFilter = string.Empty;

    [DataField]
    public float RewindSpeed = 3f;

    [DataField]
    public float MaxVolume = 1.0f;

    [DataField]
    public SoundSpecifier? ButtonClickSound = new SoundPathSpecifier("/Audio/UserInterface/click.ogg");

    [DataField]
    public List<string> CategoryRoots = new()
    {
        "music",
    };
}
