namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

public sealed class WorkingDirectoryModel
{
    private WorkingDirectoryModel(string value) => Value = value;
    public static WorkingDirectoryModel Empty { get; } = new("");
    public static WorkingDirectoryModel FromString(string? value) => string.IsNullOrWhiteSpace(value) ? Empty : new(value);
    public bool IsEmpty => string.IsNullOrEmpty(Value);
    public string Value { get; }
    public string DisplayValue => IsEmpty ? "-" : Value;
}
