using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Egide.TapePlayer;

public abstract class SharedTapePlayerSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    protected void UpdateAppearance(EntityUid uid, TapePlayerComponent comp)
    {
        var state = comp.PlayMode switch
        {
            TapePlayerPlayMode.Playing => TapePlayerVisualState.On,
            TapePlayerPlayMode.Rewinding => TapePlayerVisualState.Rewinding,
            _ => TapePlayerVisualState.Off,
        };

        Appearance.SetData(uid, TapePlayerVisuals.State, state);
    }
}
