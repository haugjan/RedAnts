using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Board;

public sealed record WebKey(string? MusicId, string? Control)
{
    public static readonly WebKey Nothing = new(null, null);

    public bool Works => MusicId is not null || Control is not null;

    public static WebKey Of(ButtonAction action)
    {
        if (action.Kind == KeyKinds.Music && action.MusicId is { Length: > 0 } music) return new WebKey(music, null);
        if (action.Kind == KeyKinds.Special && ControlOf(action.Special) is { } control) return new WebKey(null, control);
        foreach (var step in action.Sequence)
        {
            if (step.Action == KeyActions.MusicPlay && step.Target is { Length: > 0 } played) return new WebKey(played, null);
            if (step.Action == KeyActions.MusicStop) return new WebKey(null, "stop");
            if (step.Action == KeyActions.MusicFade) return new WebKey(null, "fade");
        }
        return Nothing;
    }

    private static string? ControlOf(string? special) => special switch
    {
        SpecialKey.MusicStop => "stop",
        SpecialKey.MusicFade => "fade",
        SpecialKey.MusicNext => "next",
        SpecialKey.MusicPrevious => "previous",
        SpecialKey.MusicPause => "pause",
        _ => null,
    };
}
