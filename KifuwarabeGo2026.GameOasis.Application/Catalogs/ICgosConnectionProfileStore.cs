namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>Persistence boundary for the CGOS connection collection.</summary>
public interface ICgosConnectionProfileStore
{
    string ListPath { get; }
    IReadOnlyList<CgosConnectionProfile> Load();
    void Save(IEnumerable<CgosConnectionProfile> profiles);
}
