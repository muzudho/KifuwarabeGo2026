namespace KifuwarabeGo2026.GameOasis.Gui.Application;

public abstract class GoAppMode
{
    protected GoAppMode(GoAppModeKind kind, string displayName)
    {
        Kind = kind;
        DisplayName = displayName;
    }

    public GoAppModeKind Kind { get; }

    public string DisplayName { get; }
}
